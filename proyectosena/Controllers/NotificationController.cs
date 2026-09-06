using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.Interfaces.Services;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        // El controlador solo traduce HTTP: las reglas viven en el servicio
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // -------------------- PATCH: api/notification/MarkAsRead --------------------
        [HttpPatch("MarkAsRead")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead(Guid idNotification)
        {
            var updated = await _notificationService.MarkAsRead(idNotification);

            if (updated == null)
                return NotFound("The requested notification was not found.");

            return Ok(updated);
        }

        // -------------------- GET: api/notification/GetMyNotifications --------------------
        [HttpGet("GetMyNotifications")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyNotifications(Guid idUser, int page = 1, int pageSize = 20)
        {
            return Ok(await _notificationService.GetMyNotifications(idUser, page, pageSize));
        }

        // -------------------- GET: api/notification/GetUnreadCount --------------------
        [HttpGet("GetUnreadCount")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnreadCount(Guid idUser)
        {
            var count = await _notificationService.GetUnreadCount(idUser);
            return Ok(new { UnreadCount = count });
        }

        // -------------------- PATCH: api/notification/MarkAllAsRead --------------------
        [HttpPatch("MarkAllAsRead")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAllAsRead(Guid idUser)
        {
            var updated = await _notificationService.MarkAllAsRead(idUser);
            return Ok(new { MarkedAsRead = updated });
        }
    }
}
