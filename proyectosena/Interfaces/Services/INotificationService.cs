using proyectosena.DTOs.Common;
using proyectosena.DTOs.Communication;

namespace proyectosena.Interfaces.Services
{
    /// <summary>
    /// Reglas de negocio de las notificaciones.
    /// Devuelve siempre DTOs: la entidad Notification no sale de esta capa.
    /// </summary>
    public interface INotificationService
    {
        // Página de notificaciones de un usuario, más recientes primero
        Task<PagedResult<NotificationResponseDto>> GetMyNotifications(Guid idUser, int page, int pageSize);

        // Cuántas tiene sin leer, para el indicador del encabezado
        Task<int> GetUnreadCount(Guid idUser);

        // Marca todas las no leídas de un usuario. Devuelve cuántas cambiaron.
        Task<int> MarkAllAsRead(Guid idUser);

        // Marca una sola. Devuelve null si esa notificación no existe.
        Task<NotificationResponseDto?> MarkAsRead(Guid idNotification);
    }
}
