using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.DTOs.Common;
using proyectosena.DTOs.Requests;
using proyectosena.Interfaces.Repositories;
using proyectosena.Models;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class HistoryController : ControllerBase
    {
        private readonly IHistoryRepository _historyRepository;

        public HistoryController(IHistoryRepository historyRepository)
        {
            _historyRepository = historyRepository;
        }

        // -------------------- GET: api/history/GetMyHistory --------------------
        // Historial de las solicitudes que pertenecen a este ciudadano
        [HttpGet("GetMyHistory")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyHistory(Guid idUser, int page = 1, int pageSize = 20)
        {
            (page, pageSize) = PagedResult<HistoryResponseDto>.Normalize(page, pageSize);

            var (items, total) = await _historyRepository.GetByRequestOwner(idUser, page, pageSize);

            return Ok(PagedResult<HistoryResponseDto>.Create(
                items.Select(MapToResponseDto).ToList(), page, pageSize, total));
        }

        // -------------------- GET: api/history/GetByRequest --------------------
        // Línea de tiempo de una solicitud: todos sus cambios de estado
        [HttpGet("GetByRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByRequest(Guid idRequest)
        {
            var histories = await _historyRepository.GetByRequest(idRequest);

            // Una solicitud sin cambios registrados no es un error
            return Ok(histories.Select(MapToResponseDto).ToList());
        }

        // -------------------- GET: api/history/GetByDateRange --------------------
        // Reporte administrativo: qué pasó en el sistema entre dos fechas.
        // Solo Admin: expone la actividad de todos los usuarios.
        [HttpGet("GetByDateRange")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByDateRange(
            DateTime startDate, DateTime endDate, int page = 1, int pageSize = 20)
        {
            if (startDate > endDate)
                return BadRequest("startDate must be earlier than or equal to endDate.");

            (page, pageSize) = PagedResult<HistoryResponseDto>.Normalize(page, pageSize);

            var (items, total) = await _historyRepository.GetByDateRange(startDate, endDate, page, pageSize);

            return Ok(PagedResult<HistoryResponseDto>.Create(
                items.Select(MapToResponseDto).ToList(), page, pageSize, total));
        }

        // ── Mapeo privado ───────────────────────────────────────────────
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
