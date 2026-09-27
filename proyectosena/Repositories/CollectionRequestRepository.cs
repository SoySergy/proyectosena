using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Extensions;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Repositories
{
    public class CollectionRequestRepository : ICollectionRequestRepository
    {
        // Contexto de la base de datos
        private readonly RecyRouteDbContext _context;

        // Constructor que recibe el contexto por inyección de dependencias
        public CollectionRequestRepository(RecyRouteDbContext context)
        {
            _context = context;
        }

        // Página de solicitudes con los filtros del panel. Cada filtro se suma a
        // la consulta solo si viene; sin ninguno, es la lista de siempre.
        public async Task<(List<CollectionRequest> Items, int Total)> GetCollectionRequests(
            CollectionRequestQuery filter, int page, int pageSize)
        {
            var query = WithCitizenAndManager();

            if (filter.Statuses.Length > 0)
                query = query.Where(r => filter.Statuses.Contains(r.CurrentStatus));

            if (filter.CreatedFromUtc.HasValue)
                query = query.Where(r => r.RequestDate >= filter.CreatedFromUtc.Value);

            if (filter.CreatedBeforeUtc.HasValue)
                query = query.Where(r => r.RequestDate < filter.CreatedBeforeUtc.Value);

            if (filter.CollectionFrom.HasValue)
                query = query.Where(r => r.CollectionDate >= filter.CollectionFrom.Value);

            if (filter.CollectionBefore.HasValue)
                query = query.Where(r => r.CollectionDate < filter.CollectionBefore.Value);

            if (filter.IdCitizen.HasValue)
                query = query.Where(r => r.IdUser == filter.IdCitizen.Value);

            if (filter.IdManager.HasValue)
                query = query.Where(r => r.CollectionManagement!.Any(m => m.IdManager == filter.IdManager.Value));

            if (filter.Search != null)
            {
                var pattern = "%" + filter.Search.EscapeForLike() + "%";
                query = query.Where(r =>
                    EF.Functions.ILike(r.CollectionAddress, pattern, SearchTextExtensions.LikeEscape) ||
                    EF.Functions.ILike(r.ContactPhone, pattern, SearchTextExtensions.LikeEscape) ||
                    EF.Functions.ILike(r.User!.Name + " " + r.User.LastName, pattern, SearchTextExtensions.LikeEscape));
            }

            var ordered = (filter.OrderByCollectionDate, filter.Ascending) switch
            {
                (true, true) => query.OrderBy(r => r.CollectionDate),
                (true, false) => query.OrderByDescending(r => r.CollectionDate),
                (false, true) => query.OrderBy(r => r.RequestDate),
                (false, false) => query.OrderByDescending(r => r.RequestDate)
            };

            // El id desempata: con dos solicitudes de la misma fecha, sin él una
            // podría salir en dos páginas seguidas y la otra en ninguna.
            return await ordered.ThenBy(r => r.IdRequest).ToPagedAsync(page, pageSize);
        }

        // Una solicitud por su id, con seguimiento, para quien la va a modificar.
        // Solo trae al ciudadano: UpdateCollectionRequest usa Update(), que marca
        // como modificado todo lo cargado con ella, y traer también al gestor
        // haría que cada cambio de estado reescribiera su fila de Users.
        public async Task<CollectionRequest> GetCollectionRequest(Guid idRequest)
        {
            return await _context.CollectionRequests
                                 .Include(s => s.User)
                                 .FirstOrDefaultAsync(s => s.IdRequest == idRequest);
        }

        // Una solicitud por su id para mostrarla, con su ciudadano y su gestor
        public async Task<CollectionRequest?> GetCollectionRequestDetail(Guid idRequest)
        {
            return await WithCitizenAndManager()
                                 .FirstOrDefaultAsync(s => s.IdRequest == idRequest);
        }

        // Number of requests per current status. One GROUP BY in SQL,
        // not one query per status.
        public async Task<Dictionary<string, int>> GetStatusCounts()
        {
            return await _context.CollectionRequests
                .GroupBy(r => r.CurrentStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }

        // Number of requests created on or after a given date
        public async Task<int> CountSince(DateTime since)
        {
            return await _context.CollectionRequests
                .CountAsync(r => r.RequestDate >= since);
        }

        // Checks whether a user takes part in a request: either its owner or an assigned manager.
        // Runs as a single EXISTS query — no rows are loaded.
        public async Task<bool> IsParticipant(Guid idRequest, Guid idUser)
        {
            return await _context.CollectionRequests
                .AnyAsync(r => r.IdRequest == idRequest &&
                               (r.IdUser == idUser ||
                                r.CollectionManagement!.Any(m => m.IdManager == idUser)));
        }

        public async Task<(List<CollectionRequest> Items, int Total)> GetRequestsByManager(Guid idManager, int page, int pageSize)
        {
            return await WithCitizenAndManager()
              .Where(r => r.CollectionManagement!.Any(m => m.IdManager == idManager))
              .OrderByDescending(r => r.RequestDate)
              .ToPagedAsync(page, pageSize);
        }

        public async Task<(List<CollectionRequest> Items, int Total)> GetRequestsByUser(Guid idUser, int page, int pageSize)
        {
            return await WithCitizenAndManager()
                .Where(r => r.IdUser == idUser)
                .OrderByDescending(r => r.RequestDate)
                .ToPagedAsync(page, pageSize);
        }

        // Obtiene todas las solicitudes en estado Pending ordenadas por fecha de solicitud
        // Las más antiguas aparecen primero para priorizar las que llevan más tiempo esperando
        public async Task<(List<CollectionRequest> Items, int Total)> GetPendingRequests(int page, int pageSize)
        {
            return await WithCitizenAndManager()
                .Where(r => r.CurrentStatus == CollectionRequestStatus.Pending)
                .OrderBy(r => r.RequestDate)
                .ToPagedAsync(page, pageSize);
        }

        // Crea una nueva solicitud de recolección y guarda los cambios en la base de datos
        public async Task<CollectionRequest> CreateCollectionRequest(CollectionRequest collectionRequest)
        {
            _context.CollectionRequests.Add(collectionRequest);
            await _context.SaveChangesAsync();

            // ✅ Carga explícita de la referencia User después de guardar
            await _context.Entry(collectionRequest).Reference(c => c.User).LoadAsync();

            return collectionRequest;
        }

        // Actualiza una solicitud de recolección existente y guarda los cambios en la base de datos
        public async Task<CollectionRequest> UpdateCollectionRequest(CollectionRequest collectionRequest)
        {
            _context.CollectionRequests.Update(collectionRequest);
            await _context.SaveChangesAsync();
            return collectionRequest;
        }

        // ── Privados ────────────────────────────────────────────────────

        // Toda solicitud que sale hacia el cliente lleva su ciudadano y el gestor
        // que la tiene ahora. La gestión vigente es la más reciente: AcceptRequest
        // la crea y ReassignRequest la mueve de gestor.
        //
        // Solo para leer, y por eso sin seguimiento: nada de lo que devuelve
        // puede terminar guardado por accidente junto con otra entidad.
        private IQueryable<CollectionRequest> WithCitizenAndManager() =>
            _context.CollectionRequests
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.CollectionManagement!
                    .OrderByDescending(m => m.StatusChangeDate)
                    .Take(1))
                    .ThenInclude(m => m.Manager);
    }
}