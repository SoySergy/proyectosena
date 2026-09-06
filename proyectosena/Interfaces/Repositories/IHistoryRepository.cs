using proyectosena.Models;

namespace proyectosena.Interfaces.Repositories
{
    public interface IHistoryRepository
    {
        // Obtiene una página del historial completo
        Task<(List<History> Items, int Total)> GetAll(int page, int pageSize);

        // Obtiene un registro del historial por su ID
        Task<History?> GetById(Guid idHistory);

        // todo el historial de las solicitudes que pertenecen a este ciudadano
        Task<(List<History> Items, int Total)> GetByRequestOwner(Guid idUser, int page, int pageSize);

        // Obtiene el historial de cambios de una solicitud específica
        Task<IEnumerable<History>> GetByRequest(Guid idRequest);

        // Obtiene el historial de cambios realizados por un usuario específico
        Task<IEnumerable<History>> GetByUser(Guid idUser);

        // Crea un nuevo registro en el historial
        Task<History> Create(History history);

        // Verifica si existe un registro del historial con el ID proporcionado
        Task<bool> Exists(Guid idHistory);

        // Obtiene registros del historial dentro de un rango de fechas
        Task<IEnumerable<History>> GetByDateRange(DateTime startDate, DateTime endDate);

        // Filtra el historial por el nuevo estado al que cambiaron las solicitudes
        Task<IEnumerable<History>> GetByNewStatus(string newStatus);
    }
}