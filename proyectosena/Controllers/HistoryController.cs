using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.Models;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.DTOs.Common;
using proyectosena.DTOs.Requests;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class HistoryController : ControllerBase
    {
        // Repositorio de historial inyectado por dependencias
        private readonly IHistoryRepository _historyRepository;

        public HistoryController(IHistoryRepository historyRepository)
        {
            _historyRepository = historyRepository;
        }

        // -------------------- GET: api/history/GetAll --------------------
        [HttpGet("GetAll")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll(int page = 1, int pageSize = 20)
        {
            try
            {
                (page, pageSize) = PagedResult<HistoryResponseDto>.Normalize(page, pageSize);

                var (items, total) = await _historyRepository.GetAll(page, pageSize);

                return Ok(PagedResult<HistoryResponseDto>.Create(
                    items.Select(MapToResponseDto).ToList(), page, pageSize, total));
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving history records.");
            }
        }

        // -------------------- GET: api/history/GetById --------------------
        [HttpGet("GetById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(Guid idHistory)
        {
            try
            {
                var history = await _historyRepository.GetById(idHistory);

                if (history == null)
                    return NotFound("The requested history record was not found.");

                return Ok(history);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the history record.");
            }
        }

        // -------------------- GET: api/history/GetByRequest --------------------
        [HttpGet("GetByRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByRequest(Guid idRequest)
        {
            try
            {
                var histories = await _historyRepository.GetByRequest(idRequest);

                // Útil para ver todos los cambios de estado de una solicitud específica
                if (histories == null || !histories.Any())
                    return NotFound("No history found for this request.");

                return Ok(histories);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the request history.");
            }
        }

        // -------------------- GET: api/history/GetByUser --------------------
        [HttpGet("GetByUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByUser(Guid idUser)
        {
            try
            {
                var histories = await _historyRepository.GetByUser(idUser);

                // Útil para auditoría y seguimiento de acciones de un usuario
                if (histories == null || !histories.Any())
                    return NotFound("No changes found made by this user.");

                return Ok(histories);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the user history.");
            }
        }

        [HttpGet("GetMyHistory")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyHistory(Guid idUser, int page = 1, int pageSize = 20)
        {
            try
            {
                (page, pageSize) = PagedResult<HistoryResponseDto>.Normalize(page, pageSize);

                var (items, total) = await _historyRepository.GetByRequestOwner(idUser, page, pageSize);

                return Ok(PagedResult<HistoryResponseDto>.Create(
                    items.Select(MapToResponseDto).ToList(), page, pageSize, total));
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the history.");
            }
        }




        // -------------------- GET: api/history/GetByNewStatus --------------------
        [HttpGet("GetByNewStatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByNewStatus(string newStatus)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newStatus))
                    return BadRequest("Status cannot be empty.");

                var histories = await _historyRepository.GetByNewStatus(newStatus);

                if (histories == null || !histories.Any())
                    return NotFound($"No records found with status '{newStatus}'.");

                return Ok(histories);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving history by status.");
            }
        }

        // -------------------- GET: api/history/GetByDateRange --------------------
        [HttpGet("GetByDateRange")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                // Valida que el rango de fechas sea coherente
                if (startDate > endDate)
                    return BadRequest("Start date cannot be greater than end date.");

                var histories = await _historyRepository.GetByDateRange(startDate, endDate);

                if (histories == null || !histories.Any())
                    return NotFound("No records found in the specified date range.");

                return Ok(histories);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving history by date range.");
            }
        }

        // -------------------- GET: api/history/HistoryExists --------------------
        [HttpGet("HistoryExists")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> HistoryExists(Guid idHistory)
        {
            try
            {
                // Retorna un objeto con la propiedad exists para el frontend
                var exists = await _historyRepository.Exists(idHistory);
                return Ok(new { exists });
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error checking history existence.");
            }
        }

        private static HistoryResponseDto MapToResponseDto(History h) => new()
        {
            IdHistory = h.IdHistory,
            IdRequest = h.IdRequest,
            IdUser = h.IdUser,
            UserName = h.User != null ? $"{h.User.Name} {h.User.LastName}" : string.Empty,
            PreviousStatus = h.PreviousStatus,
            NewStatus = h.NewStatus,
            ChangeDate = h.ChangeDate,
            Comment = h.Comment
        };

    }
}