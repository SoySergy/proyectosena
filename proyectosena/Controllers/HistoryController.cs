using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.Extensions;
using proyectosena.Interfaces.Services;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class HistoryController : ControllerBase
    {
        // El controlador solo traduce HTTP: las reglas viven en el servicio
        private readonly IHistoryService _historyService;

        public HistoryController(IHistoryService historyService)
        {
            _historyService = historyService;
        }

        // -------------------- GET: api/history/GetMyHistory --------------------
        // Historial de las solicitudes que pertenecen a este ciudadano
        [HttpGet("GetMyHistory")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyHistory(int page = 1, int pageSize = 20)
        {
            // El id sale del token, no de la URL
            return Ok(await _historyService.GetMyHistory(User.GetUserId(), page, pageSize));
        }

        // -------------------- GET: api/history/GetByRequest --------------------
        // Línea de tiempo de una solicitud: todos sus cambios de estado
        [HttpGet("GetByRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByRequest(Guid idRequest)
        {
            // Una solicitud sin cambios registrados no es un error
            return Ok(await _historyService.GetByRequest(idRequest));
        }

        // -------------------- GET: api/history/GetByDateRange --------------------
        // Reporte administrativo: qué pasó en el sistema entre dos fechas.
        // Solo Admin: expone la actividad de todos los usuarios.
        [HttpGet("GetByDateRange")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetByDateRange(
            DateTime startDate, DateTime endDate, int page = 1, int pageSize = 20)
        {
            if (startDate > endDate)
                return BadRequest("startDate must be earlier than or equal to endDate.");

            return Ok(await _historyService.GetByDateRange(startDate, endDate, page, pageSize));
        }
    }
}
