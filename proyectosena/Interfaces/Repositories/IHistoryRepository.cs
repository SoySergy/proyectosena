using proyectosena.Models;

namespace proyectosena.Interfaces.Repositories
{
    public interface IHistoryRepository
    {
        // Todo el historial de las solicitudes que pertenecen a este ciudadano
        Task<(List<History> Items, int Total)> GetByRequestOwner(Guid idUser, int page, int pageSize);

        // Historial de cambios de una solicitud específica, para su línea de tiempo
        Task<IEnumerable<History>> GetByRequest(Guid idRequest);

        // Crea un nuevo registro en el historial
        Task<History> Create(History history);

        // Página de cambios del sistema entre dos fechas. Reporte administrativo.
        Task<(List<History> Items, int Total)> GetByDateRange(DateTime startDate, DateTime endDate, int page, int pageSize);
    }
}
