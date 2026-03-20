using proyectosena.Models;

namespace proyectosena.Interfaces
{
    public interface IHistoryRepository
    {
        // Obtiene todos los registros del historial
        Task<IEnumerable<History>> GetAll();

        // Obtiene un registro del historial por su ID
        Task<History?> GetById(Guid idHistory);

        // Obtiene el historial de cambios de una solicitud específica
        Task<IEnumerable<History>> GetByRequest(Guid idRequest);

        // Obtiene el historial de cambios realizados por un usuario específico
        Task<IEnumerable<History>> GetByUser(Guid idUser);

        // Crea un nuevo registro en el historial
        Task<History> Create(History history);

        // Actualiza un registro existente del historial
        Task<History?> Update(History history);

        // Elimina un registro del historial por su ID
        Task<bool> Delete(Guid idHistory);

        // Verifica si existe un registro del historial con el ID proporcionado
        Task<bool> Exists(Guid idHistory);

        // Obtiene registros del historial dentro de un rango de fechas
        Task<IEnumerable<History>> GetByDateRange(DateTime startDate, DateTime endDate);

        // Filtra el historial por el nuevo estado al que cambiaron las solicitudes
        Task<IEnumerable<History>> GetByNewStatus(string newStatus);
    }
}