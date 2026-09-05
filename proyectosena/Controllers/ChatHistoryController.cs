using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.Models;
using proyectosena.Interfaces;
using proyectosena.DTOs.Communication;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatHistoryController : ControllerBase
    {
        // Repositorio de historial de chat inyectado por dependencias
        private readonly IChatHistoryRepository _chatHistoryRepository;

        // Needed to check whether the user takes part in the conversation
        private readonly ICollectionRequestRepository _collectionRequestRepository;

        public ChatHistoryController(
            IChatHistoryRepository chatHistoryRepository,
            ICollectionRequestRepository collectionRequestRepository)
        {
            _chatHistoryRepository = chatHistoryRepository;
            _collectionRequestRepository = collectionRequestRepository;
        }

        // -------------------- GET: api/chathistory/GetMessagesByRequest --------------------
        [HttpGet("GetMessagesByRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMessagesByRequest(Guid idRequest, Guid idUser)
        {
            try
            {
                // Only the request owner or an assigned manager may read this conversation
                var allowed = await _collectionRequestRepository.IsParticipant(idRequest, idUser);
                if (!allowed)
                    return StatusCode(StatusCodes.Status403Forbidden,
                        "You are not a participant of this collection request.");

                var messages = await _chatHistoryRepository.GetMessagesByRequest(idRequest);

                // An empty conversation is not an error
                return Ok(messages.Select(MapToResponseDto).ToList());
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving messages.");
            }
        }

        // -------------------- GET: api/chathistory/GetMessage --------------------
        [HttpGet("GetMessage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMessage(Guid idChatHistory)
        {
            try
            {
                var message = await _chatHistoryRepository.GetMessage(idChatHistory);

                if (message == null)
                    return NotFound("The requested message was not found.");

                return Ok(message);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the message.");
            }
        }

        // -------------------- POST: api/chathistory/SendMessage --------------------
        [HttpPost("SendMessage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("Message data cannot be null.");

                if (dto.IdRequest == Guid.Empty || dto.IdSender == Guid.Empty)
                    return BadRequest("The message must have a valid IdRequest and IdSender.");

                if (string.IsNullOrWhiteSpace(dto.Message))
                    return BadRequest("Message content cannot be empty.");

                // Only the request owner or an assigned manager may write here
                var allowed = await _collectionRequestRepository.IsParticipant(dto.IdRequest, dto.IdSender);
                if (!allowed)
                    return StatusCode(StatusCodes.Status403Forbidden,
                        "You are not a participant of this collection request.");

                var message = new ChatHistory
                {
                    IdRequest = dto.IdRequest,
                    IdSender = dto.IdSender,
                    Message = dto.Message,
                    SendDate = DateTime.UtcNow,
                    IsRead = false
                };

                var created = await _chatHistoryRepository.CreateMessage(message);

                // Reload with sender and role so the response carries the display name
                var full = await _chatHistoryRepository.GetMessage(created.IdChatHistory);
                return Ok(MapToResponseDto(full));
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error sending the message.");
            }
        }

        // -------------------- PUT: api/chathistory/MarkAsRead --------------------
        // Operación para marcar un mensaje como leído
        [HttpPut("MarkAsRead")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAsRead(Guid idChatHistory)
        {
            try
            {
                var result = await _chatHistoryRepository.MarkAsRead(idChatHistory);

                // Retorna false si el mensaje no existe en la base de datos
                if (!result)
                    return BadRequest("Could not mark the message as read. Please verify it exists.");

                return Ok("Message marked as read successfully.");
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error marking the message as read.");
            }
        }

        // -------------------- GET: api/chathistory/GetUnreadMessages --------------------
        // Obtiene mensajes no leídos de otros usuarios en una solicitud específica
        [HttpGet("GetUnreadMessages")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUnreadMessages(Guid idUser, Guid idRequest)
        {
            try
            {
                var messages = await _chatHistoryRepository.GetUnreadMessages(idUser, idRequest);

                // Retorna lista vacía si no hay mensajes pendientes, no un 404
                if (messages == null || !messages.Any())
                    return Ok(new List<ChatHistory>());

                return Ok(messages);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving unread messages.");
            }
        }

        // -------------------- DELETE: api/chathistory/DeleteMessage --------------------
        [HttpDelete("DeleteMessage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteMessage(Guid idChatHistory)
        {
            try
            {
                var deleted = await _chatHistoryRepository.DeleteMessage(idChatHistory);

                // Retorna false si el mensaje no existe en la base de datos
                if (!deleted)
                    return BadRequest("Could not delete the message. Please verify it exists.");

                return Ok("Message deleted successfully.");
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting the message.");
            }
        }

        // ── Private mapping ─────────────────────────────────────────────

        // Flattens the sender relationship so the client never sees the User entity
        private static ChatMessageResponseDto MapToResponseDto(ChatHistory c) => new()
        {
            IdChatHistory = c.IdChatHistory,
            IdRequest = c.IdRequest,
            IdSender = c.IdSender,
            SenderName = c.Sender?.Name ?? string.Empty,
            SenderLastName = c.Sender?.LastName ?? string.Empty,
            SenderRole = c.Sender?.Role?.RoleName ?? string.Empty,
            Message = c.Message,
            SendDate = c.SendDate,
            IsRead = c.IsRead
        };
    }
}