using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.Models;
using proyectosena.Interfaces;

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

        // -------------------- POST: api/notification/CreateNotification --------------------
        [HttpPost("CreateNotification")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateNotification([FromBody] Notification notification)
        {
            try
            {
                if (notification == null)
                    return BadRequest("Notification data cannot be null.");

                // Valida que los campos obligatorios no estén vacíos
                if (string.IsNullOrWhiteSpace(notification.Title) || string.IsNullOrWhiteSpace(notification.Message))
                    return BadRequest("Title and message are required.");

                var newNotification = await _notificationRepository.CreateNotification(notification);
                return Ok(newNotification);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating the notification.");
            }
        }

        // -------------------- PUT: api/notification/UpdateNotification --------------------
        [HttpPut("UpdateNotification")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateNotification([FromBody] Notification notification)
        {
            try
            {
                if (notification == null)
                    return BadRequest("Notification data cannot be null.");

                // El IdNotification es obligatorio para identificar el registro a actualizar
                if (notification.IdNotification == Guid.Empty)
                    return BadRequest("IdNotification is required to update a record.");

                var updated = await _notificationRepository.UpdateNotification(notification);
                return Ok(updated);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating the notification.");
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

        // -------------------- DELETE: api/notification/DeleteNotification --------------------
        [HttpDelete("DeleteNotification")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteNotification(Guid idNotification)
        {
            try
            {
                var deleted = await _notificationRepository.DeleteNotification(idNotification);

                // Retorna false si la notificación no existe en la base de datos
                if (!deleted)
                    return BadRequest("Could not delete the notification. Please verify it exists.");

                return Ok("Notification deleted successfully.");
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting the notification.");
            }
        }
    }
}