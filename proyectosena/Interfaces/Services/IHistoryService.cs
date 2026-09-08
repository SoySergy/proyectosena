using proyectosena.DTOs.Common;
using proyectosena.DTOs.Requests;
using proyectosena.Models;

namespace proyectosena.Interfaces.Services
{
    /// <summary>
    /// Consulta del historial de cambios de estado de las solicitudes.
    /// </summary>
    /// <remarks>
    /// Solo lee. Los registros los escribe quien cambia el estado
    /// (<c>CollectionStatusService</c> y <c>AssignmentService</c>), a través del
    /// repositorio: el historial es una bitácora, no algo que se edite.
    /// </remarks>
    public interface IHistoryService
    {
        // Historial de las solicitudes que pertenecen a este ciudadano.
        // Filtra por el dueño de la solicitud, no por el autor del cambio.
        Task<PagedResult<HistoryResponseDto>> GetMyHistory(Guid idUser, int page, int pageSize);

        // Línea de tiempo de una solicitud. Sin cambios registrados devuelve
        // una lista vacía, que no es un error.
        // Solo su dueño, o el personal. idUser y esPersonal salen del token.
        Task<(RequestAccessResult Result, List<HistoryResponseDto> Items)> GetByRequest(
            Guid idRequest, Guid idUser, bool esPersonal);

        // Reporte administrativo: qué pasó en el sistema entre dos fechas.
        Task<PagedResult<HistoryResponseDto>> GetByDateRange(
            DateTime startDate, DateTime endDate, int page, int pageSize);
    }
}
