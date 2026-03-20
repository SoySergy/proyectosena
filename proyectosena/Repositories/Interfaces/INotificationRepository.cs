using proyectosena.Models;

namespace proyectosena.Interfaces
{
    public interface INotificationRepository
    {
        // Obtiene todas las notificaciones
        Task<List<Notification>> GetNotifications();

        // Obtiene una notificación por su ID
        Task<Notification> GetNotification(Guid idNotification);

        // Crea una nueva notificación
        Task<Notification> CreateNotification(Notification notification);

        // Actualiza una notificación existente
        Task<Notification> UpdateNotification(Notification notification);

        // Elimina una notificación por su ID
        Task<bool> DeleteNotification(Guid idNotification);
    }
}