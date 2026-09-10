using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.Extensions;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

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
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetByRequest(Guid idRequest)
        {
            var (result, items) = await _historyService
                .GetByRequest(idRequest, User.GetUserId(), User.IsStaff());

            if (result == RequestAccessResult.NotParticipant)
                return StatusCode(StatusCodes.Status403Forbidden,
                    "You can only view the history of your own requests.");

            // Una solicitud sin cambios registrados no es un error
            return Ok(items);
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

            // Aquí sí se comparan instantes: las fechas del historial se guardan en
            // UTC. Las que llegan por la URL vienen sin zona, y PostgreSQL rechaza
            // compararlas contra una columna con zona. Se interpretan como UTC, que
            // es lo que el administrador espera de un informe del sistema.
            return Ok(await _historyService.GetByDateRange(
                AsUtc(startDate), AsUtc(endDate), page, pageSize));
        }

        /// <summary>
        /// Marca como UTC una fecha que llegó sin zona horaria.
        /// </summary>
        /// <remarks>
        /// El enlazador de ASP.NET devuelve <c>Kind=Unspecified</c> para una fecha
        /// escrita en la URL, y Npgsql se niega a compararla contra una columna
        /// con zona. Si ya viene con zona se respeta: solo se rellena lo que falta.
        /// </remarks>
        private static DateTime AsUtc(DateTime fecha) => fecha.Kind switch
        {
            DateTimeKind.Utc => fecha,
            DateTimeKind.Local => fecha.ToUniversalTime(),
            _ => DateTime.SpecifyKind(fecha, DateTimeKind.Utc)
        };
    }
}
