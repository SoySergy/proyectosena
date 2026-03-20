using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.Models;
using proyectosena.Interfaces;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatHistoryController : ControllerBase
    {
        // Repositorio de historial de chat inyectado por dependencias
        private readonly IChatHistoryRepository _chatHistoryRepository;

        public ChatHistoryController(IChatHistoryRepository chatHistoryRepository)
        {
            _chatHistoryRepository = chatHistoryRepository;
        }

        // -------------------- GET: api/chathistory/GetMessagesByRequest --------------------
        [HttpGet("GetMessagesByRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMessagesByRequest(Guid idRequest)
        {
            try
            {
                var messages = await _chatHistoryRepository.GetMessagesByRequest(idRequest);

                // Verifica si hay mensajes para esta solicitud
                if (messages == null || !messages.Any())
                    return NotFound("No messages found for this request.");

                return Ok(messages);
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
        public async Task<IActionResult> SendMessage([FromBody] ChatHistory chatHistory)
        {
            try
            {
                if (chatHistory == null)
                    return BadRequest("Message data cannot be null.");

                // Valida que las claves foráneas sean válidas
                if (chatHistory.IdRequest == Guid.Empty || chatHistory.IdSender == Guid.Empty)
                    return BadRequest("The message must have a valid IdRequest and IdSender.");

                if (string.IsNullOrWhiteSpace(chatHistory.Message))
                    return BadRequest("Message content cannot be empty.");

                var newMessage = await _chatHistoryRepository.CreateMessage(chatHistory);
                return Ok(newMessage);
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
    }
}