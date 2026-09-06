using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.Models;
using proyectosena.Interfaces;
using proyectosena.DTOs.Common;
using proyectosena.DTOs.Communication;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        // Repositorio de notificaciones inyectado por dependencias
        private readonly INotificationRepository _notificationRepository;

        public NotificationController(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        // -------------------- GET: api/notification/GetNotifications --------------------
        [HttpGet("GetNotifications")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                var notifications = await _notificationRepository.GetNotifications();

                // Verifica si la lista está vacía o nula
                if (notifications == null || !notifications.Any())
                    return NotFound("No registered notifications were found.");

                return Ok(notifications);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving notifications.");
            }
        }

        // -------------------- GET: api/notification/GetNotificationById --------------------
        [HttpGet("GetNotificationById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetNotificationById(Guid idNotification)
        {
            try
            {
                var notification = await _notificationRepository.GetNotification(idNotification);

                if (notification == null)
                    return NotFound("The requested notification was not found.");

                return Ok(notification);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the notification.");
            }
        }
        // -------------------- PATCH: api/notification/MarkAsRead --------------------
        // Operación parcial: solo cambia el campo IsRead a true
        [HttpPatch("MarkAsRead")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAsRead(Guid idNotification)
        {
            try
            {
                var notification = await _notificationRepository.GetNotification(idNotification);

                if (notification == null)
                    return NotFound("The requested notification was not found.");

                // Actualiza solo el campo IsRead sin tocar los demás campos
                notification.IsRead = true;
                var updated = await _notificationRepository.UpdateNotification(notification);
                return Ok(updated);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error marking the notification as read.");
            }
        }

        // -------------------- GET: api/notification/GetMyNotifications --------------------
        [HttpGet("GetMyNotifications")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyNotifications(Guid idUser, int page = 1, int pageSize = 20)
        {
            try
            {
                (page, pageSize) = PagedResult<NotificationResponseDto>.Normalize(page, pageSize);

                var (items, total) = await _notificationRepository.GetByUser(idUser, page, pageSize);

                return Ok(PagedResult<NotificationResponseDto>.Create(
                    items.Select(MapToResponseDto).ToList(), page, pageSize, total));
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving notifications.");
            }
        }

        // -------------------- GET: api/notification/GetUnreadCount --------------------
        [HttpGet("GetUnreadCount")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUnreadCount(Guid idUser)
        {
            try
            {
                var count = await _notificationRepository.CountUnread(idUser);
                return Ok(new { UnreadCount = count });
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error counting unread notifications.");
            }
        }

        // -------------------- PATCH: api/notification/MarkAllAsRead --------------------
        [HttpPatch("MarkAllAsRead")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAllAsRead(Guid idUser)
        {
            try
            {
                var updated = await _notificationRepository.MarkAllAsRead(idUser);
                return Ok(new { MarkedAsRead = updated });
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error marking notifications as read.");
            }
        }

        // ── Private mapping ─────────────────────────────────────────────
        private static NotificationResponseDto MapToResponseDto(Notification n) => new()
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