using proyectosena.DTOs.Common;
using proyectosena.DTOs.Communication;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<PagedResult<NotificationResponseDto>> GetMyNotifications(
            Guid idUser, int page, int pageSize)
        {
            (page, pageSize) = PagedResult<NotificationResponseDto>.Normalize(page, pageSize);

            var (items, total) = await _notificationRepository.GetByUser(idUser, page, pageSize);

            return PagedResult<NotificationResponseDto>.Create(
                items.Select(MapToDto).ToList(), page, pageSize, total);
        }

        public Task<int> GetUnreadCount(Guid idUser)
            => _notificationRepository.CountUnread(idUser);

        public Task<int> MarkAllAsRead(Guid idUser)
            => _notificationRepository.MarkAllAsRead(idUser);

        public async Task<NotificationResponseDto?> MarkAsRead(Guid idNotification)
        {
            var notification = await _notificationRepository.GetNotification(idNotification);
            if (notification == null)
                return null;

            // Solo cambia IsRead; los demás campos quedan como estaban
            notification.IsRead = true;
            var updated = await _notificationRepository.UpdateNotification(notification);

            return MapToDto(updated);
        }

        public async Task<NotificationResponseDto?> GetById(Guid idNotification)
        {
            var notification = await _notificationRepository.GetNotification(idNotification);
            return notification == null ? null : MapToDto(notification);
        }

        public async Task<List<NotificationResponseDto>> GetAll()
        {
            var notifications = await _notificationRepository.GetNotifications();
            return notifications.Select(MapToDto).ToList();
        }

        // ── Mapeo privado ───────────────────────────────────────────────
        private static NotificationResponseDto MapToDto(Notification n) => new()
        {
            IdNotification = n.IdNotification,
            IdUser = n.IdUser,
            IdRequest = n.IdRequest,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            CreationDate = n.CreationDate,
            IsRead = n.IsRead
        };
    }
}
