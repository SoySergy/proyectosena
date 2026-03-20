namespace proyectosena.Interfaces
{
    public interface ICollectionStatusService
    {
        // Cambia el estado de una solicitud, registra el historial y notifica al ciudadano
        Task<bool> UpdateStatusAsync(Guid idRequest, string newStatus, Guid idManager, string? comment = null);
    }
}