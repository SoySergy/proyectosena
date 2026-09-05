using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Interfaces;
using proyectosena.Models;

namespace proyectosena.Repositorios
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

        // Obtiene todas las solicitudes de recolección incluyendo el usuario asociado
        public async Task<List<CollectionRequest>> GetCollectionRequests()
        {
            return await _context.CollectionRequests
                                 .Include(s => s.User)
                                 .ToListAsync();
        }

        // El ciudadano consulta sus propias solicitudes directamente Obtiene una solicitud específica por ID incluyendo el usuario asociado
        public async Task<CollectionRequest> GetCollectionRequest(Guid idRequest)
        {
            return await _context.CollectionRequests
                                 .Include(s => s.User)
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

        public async Task<IEnumerable<CollectionRequest>> GetRequestsByManager(Guid idManager)
        {
            return await _context.CollectionRequests
              .Include(r => r.User)
              .Where(r => r.CollectionManagement!.Any(m => m.IdManager == idManager))
              .OrderByDescending(r => r.RequestDate)
              .ToListAsync();
               
        }

        public async Task<IEnumerable<CollectionRequest>> GetRequestsByUser(Guid idUser)
        {
            return await _context.CollectionRequests
                .Include(r => r.User)
                .Where(r => r.IdUser == idUser)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }

        // Obtiene todas las solicitudes en estado Pending ordenadas por fecha de solicitud
        // Las más antiguas aparecen primero para priorizar las que llevan más tiempo esperando
        public async Task<List<CollectionRequest>> GetPendingRequests()
        {
            return await _context.CollectionRequests
                .Include(r => r.User)
                .Where(r => r.CurrentStatus == CollectionRequestStatus.Pending)
                .OrderBy(r => r.RequestDate)
                .ToListAsync();
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

        // Elimina una solicitud de recolección por su ID, retorna false si no existe
        public async Task<bool> DeleteCollectionRequest(Guid idRequest)
        {
            var collectionRequest = await _context.CollectionRequests
                                                  .FirstOrDefaultAsync(s => s.IdRequest == idRequest);
            if (collectionRequest == null)
                return false;

            _context.CollectionRequests.Remove(collectionRequest);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}