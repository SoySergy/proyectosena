namespace proyectosena.Interfaces.Services
{
    using proyectosena.Models;
    public interface ICollectionStatusService
    {
        // Cambia el estado de una solicitud, registra el historial y notifica al ciudadano
        Task<StatusUpdateResult> UpdateStatusAsync(Guid idRequest, string newStatus, Guid idManager, string? comment = null);
    }
}