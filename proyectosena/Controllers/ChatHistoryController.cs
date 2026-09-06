using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.DTOs.Communication;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatHistoryController : ControllerBase
    {
        // El controlador solo traduce HTTP: las reglas viven en el servicio
        private readonly IChatHistoryService _chatHistoryService;

        public ChatHistoryController(IChatHistoryService chatHistoryService)
        {
            _chatHistoryService = chatHistoryService;
        }

        // -------------------- GET: api/chathistory/GetMessagesByRequest --------------------
        [HttpGet("GetMessagesByRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMessagesByRequest(Guid idRequest, Guid idUser)
        {
            var (result, messages) = await _chatHistoryService.GetMessagesByRequest(idRequest, idUser);

            if (result == ChatAccessResult.NotParticipant)
                return StatusCode(StatusCodes.Status403Forbidden,
                    "You are not a participant of this collection request.");

            return Ok(messages);
        }

        // -------------------- GET: api/chathistory/GetMessage --------------------
        [HttpGet("GetMessage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMessage(Guid idChatHistory)
        {
            var message = await _chatHistoryService.GetMessage(idChatHistory);

            if (message == null)
                return NotFound("The requested message was not found.");

            return Ok(message);
        }

        // -------------------- POST: api/chathistory/SendMessage --------------------
        [HttpPost("SendMessage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            if (dto == null)
                return BadRequest("Message data cannot be null.");

            if (dto.IdRequest == Guid.Empty || dto.IdSender == Guid.Empty)
                return BadRequest("The message must have a valid IdRequest and IdSender.");

            if (string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest("Message content cannot be empty.");

            var (result, message) = await _chatHistoryService.SendMessage(dto);

            if (result == ChatAccessResult.NotParticipant)
                return StatusCode(StatusCodes.Status403Forbidden,
                    "You are not a participant of this collection request.");

            return Ok(message);
        }

        // -------------------- PUT: api/chathistory/MarkAsRead --------------------
        [HttpPut("MarkAsRead")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead(Guid idChatHistory)
        {
            var done = await _chatHistoryService.MarkAsRead(idChatHistory);

            if (!done)
                return NotFound("The requested message was not found.");

            return Ok("Message marked as read successfully.");
        }

        // -------------------- GET: api/chathistory/GetUnreadMessages --------------------
        [HttpGet("GetUnreadMessages")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnreadMessages(Guid idUser, Guid idRequest)
        {
            return Ok(await _chatHistoryService.GetUnreadMessages(idUser, idRequest));
        }

        // -------------------- DELETE: api/chathistory/DeleteMessage --------------------
        [HttpDelete("DeleteMessage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMessage(Guid idChatHistory)
        {
            var deleted = await _chatHistoryService.DeleteMessage(idChatHistory);

            if (!deleted)
                return NotFound("The requested message was not found.");

            return Ok("Message deleted successfully.");
        }
    }
}
