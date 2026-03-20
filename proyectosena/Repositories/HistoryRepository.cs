using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Models;
using proyectosena.Interfaces;

namespace proyectosena.Repositorios
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

        // Obtiene todos los registros del historial con sus relaciones de solicitud y usuario
        // Ordena por fecha de cambio descendente (más recientes primero)
        public async Task<IEnumerable<History>> GetAll()
        {
            return await _context.Histories
                .Include(h => h.CollectionRequest)
                .Include(h => h.User)
                .OrderByDescending(h => h.ChangeDate)
                .ToListAsync();
        }

        // Busca un registro específico del historial por su ID
        // Incluye las relaciones de solicitud y usuario
        public async Task<History?> GetById(Guid idHistory)
        {
            return await _context.Histories
                .Include(h => h.CollectionRequest)
                .Include(h => h.User)
                .FirstOrDefaultAsync(h => h.IdHistory == idHistory);
        }

        // Obtiene todo el historial de cambios de una solicitud específica
        // Útil para rastrear todos los estados por los que ha pasado una solicitud
        public async Task<IEnumerable<History>> GetByRequest(Guid idRequest)
        {
            return await _context.Histories
                .Include(h => h.User)
                .Where(h => h.IdRequest == idRequest)
                .OrderByDescending(h => h.ChangeDate)
                .ToListAsync();
        }

        // Obtiene todos los cambios realizados por un usuario específico
        // Útil para auditoría y seguimiento de acciones de usuarios
        public async Task<IEnumerable<History>> GetByUser(Guid idUser)
        {
            return await _context.Histories
                .Include(h => h.CollectionRequest)
                .Where(h => h.IdUser == idUser)
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

        // Actualiza un registro existente del historial
        public async Task<History?> Update(History history)
        {
            _context.Histories.Update(history);
            await _context.SaveChangesAsync();
            return history;
        }

        // Elimina un registro del historial por su ID
        // Retorna true si se eliminó correctamente, false si no existe
        public async Task<bool> Delete(Guid idHistory)
        {
            var history = await _context.Histories
                .FirstOrDefaultAsync(h => h.IdHistory == idHistory);
            if (history == null)
                return false;

            _context.Histories.Remove(history);
            await _context.SaveChangesAsync();
            return true;
        }

        // Verifica si existe un registro del historial con el ID proporcionado
        public async Task<bool> Exists(Guid idHistory)
        {
            return await _context.Histories
                .AnyAsync(h => h.IdHistory == idHistory);
        }

        // Obtiene registros del historial dentro de un rango de fechas
        // Útil para reportes y análisis de cambios en períodos específicos
        public async Task<IEnumerable<History>> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            return await _context.Histories
                .Include(h => h.CollectionRequest)
                .Include(h => h.User)
                .Where(h => h.ChangeDate >= startDate && h.ChangeDate <= endDate)
                .OrderByDescending(h => h.ChangeDate)
                .ToListAsync();
        }

        // Filtra el historial por el estado nuevo al que cambiaron las solicitudes
        // Útil para encontrar todas las veces que las solicitudes pasaron a un estado específico
        public async Task<IEnumerable<History>> GetByNewStatus(string newStatus)
        {
            return await _context.Histories
                .Include(h => h.CollectionRequest)
                .Include(h => h.User)
                .Where(h => h.NewStatus == newStatus)
                .OrderByDescending(h => h.ChangeDate)
                .ToListAsync();
        }
    }
}