using proyectosena.Models;

namespace proyectosena.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        // Obtiene una notificación por su ID, o null si no existe
        Task<Notification?> GetNotification(Guid idNotification);

        // Crea una nueva notificación
        Task<Notification> CreateNotification(Notification notification);

        // Actualiza una notificación existente
        Task<Notification> UpdateNotification(Notification notification);

        //Creates several notifications in a single database round trip 
        Task CreateNotifications(IEnumerable<Notification> notifications);

        // Gets every notification for a user, newest first
        Task<(List<Notification> Items, int Total)> GetByUser(Guid idUser, int page, int pageSize);

        // Counts the user's unread notifications
        Task<int> CountUnread(Guid idUser);

        // Marks all the user's unread notifications as read, returns how many changed
        Task<int> MarkAllAsRead(Guid idUser);

    }
}