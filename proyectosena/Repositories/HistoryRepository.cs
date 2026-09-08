using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Extensions;
using proyectosena.Interfaces.Repositories;
using proyectosena.Models;

namespace proyectosena.Repositories
{
    public class HistoryRepository : IHistoryRepository
    {
        // Contexto de la base de datos
        private readonly RecyRouteDbContext _context;

        // Constructor que recibe el contexto por inyección de dependencias
        public HistoryRepository(RecyRouteDbContext context)
        {
            _context = context;
        }

        // Página del historial de las solicitudes que pertenecen a un ciudadano.
        // Filtra por el dueño de la solicitud, no por el autor del cambio: el
        // autor casi siempre es el gestor.
        public async Task<(List<History> Items, int Total)> GetByRequestOwner(Guid idUser, int page, int pageSize)
        {
            return await _context.Histories
                .Include(h => h.CollectionRequest)
                .Include(h => h.User)
                .Where(h => h.CollectionRequest!.IdUser == idUser)
                .OrderByDescending(h => h.ChangeDate)
                .ToPagedAsync(page, pageSize);
        }

        // Historial completo de una solicitud, para mostrar su línea de tiempo
        public async Task<IEnumerable<History>> GetByRequest(Guid idRequest)
        {
            return await _context.Histories
                .Include(h => h.User)
                .Where(h => h.IdRequest == idRequest)
                .OrderByDescending(h => h.ChangeDate)
                .ToListAsync();
        }

        // Crea un nuevo registro en el historial
        // Establece automáticamente la fecha de cambio al momento actual
        public async Task<History> Create(History history)
        {
            history.ChangeDate = DateTime.UtcNow;
            _context.Histories.Add(history);
            await _context.SaveChangesAsync();
            return history;
        }

        // Cambios de todo el sistema entre dos fechas, para reportes de administración.
        // Paginado: un rango amplio puede abarcar miles de registros.
        public async Task<(List<History> Items, int Total)> GetByDateRange(
            DateTime startDate, DateTime endDate, int page, int pageSize)
        {
            return await _context.Histories
                .Include(h => h.CollectionRequest)
                .Include(h => h.User)
                .Where(h => h.ChangeDate >= startDate && h.ChangeDate <= endDate)
                .OrderByDescending(h => h.ChangeDate)
                .ToPagedAsync(page, pageSize);
        }
    }
}
