namespace proyectosena.Interfaces
{
    public interface IAssignmentService
    {
        // Intenta que un gestor tome una solicitud pendiente
        // Retorna true si la tomó exitosamente, false si ya fue tomada por otro gestor
        Task<(bool Success, string Message)> AcceptRequestAsync(Guid idRequest, Guid idManager);

        // Notifica a todos los gestores que hay una nueva solicitud disponible
        Task NotifyAllManagersAsync(Guid idRequest, string collectionAddress);
    }
}