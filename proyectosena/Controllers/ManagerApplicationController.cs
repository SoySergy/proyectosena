using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.DTOs.ManagerApplication;
using proyectosena.Extensions;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ManagerApplicationController : ControllerBase
    {
        // El controlador solo traduce HTTP: las reglas viven en el servicio
        private readonly IManagerApplicationService _applicationService;

        public ManagerApplicationController(IManagerApplicationService applicationService)
        {
            _applicationService = applicationService;
        }

        // -------------------- POST: api/managerapplication/Apply --------------------
        // El ciudadano pide que se le conceda el rol de gestor
        [HttpPost("Apply")]
        [Authorize(Policy = "CitizenOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Apply([FromBody] CreateManagerApplicationDto dto)
        {
            if (dto == null)
                return BadRequest("Application data cannot be null.");

            // Quién solicita sale del token
            var (result, application) = await _applicationService.Apply(dto, User.GetUserId());

            if (result == ManagerApplicationResult.AlreadyPending)
                return Conflict("You already have an application waiting for review.");

            if (result == ManagerApplicationResult.AlreadyApproved)
                return Conflict("Your application was already approved.");

            return Ok(application);
        }

        // -------------------- GET: api/managerapplication/GetMyApplication --------------------
        // El ciudadano consulta en qué va su solicitud
        [HttpGet("GetMyApplication")]
        [Authorize(Policy = "CitizenOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyApplication()
        {
            var application = await _applicationService.GetMine(User.GetUserId());

            if (application == null)
                return NotFound("You have not applied to become a manager yet.");

            return Ok(application);
        }

        // -------------------- GET: api/managerapplication/GetPending --------------------
        // Bandeja del administrador: lo que falta por revisar
        [HttpGet("GetPending")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPending(int page = 1, int pageSize = 20)
        {
            return Ok(await _applicationService.GetPending(page, pageSize));
        }

        // -------------------- PATCH: api/managerapplication/Approve --------------------
        // Concede el rol de gestor al solicitante
        [HttpPatch("Approve")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Approve(Guid idApplication)
        {
            // Qué administrador decide sale de su token, y queda firmado así
            var (result, application) = await _applicationService
                .Approve(idApplication, User.GetUserId());

            if (result == ManagerApplicationDecisionResult.ApplicationNotFound)
                return NotFound("The requested application was not found.");

            if (result == ManagerApplicationDecisionResult.AlreadyDecided)
                return Conflict("This application was already reviewed.");

            return Ok(application);
        }

        // -------------------- PATCH: api/managerapplication/Reject --------------------
        // Rechaza con un motivo. El ciudadano puede volver a solicitar.
        [HttpPatch("Reject")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Reject(Guid idApplication, [FromBody] RejectManagerApplicationDto dto)
        {
            if (dto == null)
                return BadRequest("A reason is required to reject an application.");

            var (result, application) = await _applicationService
                .Reject(idApplication, User.GetUserId(), dto.Reason);

            if (result == ManagerApplicationDecisionResult.ApplicationNotFound)
                return NotFound("The requested application was not found.");

            if (result == ManagerApplicationDecisionResult.AlreadyDecided)
                return Conflict("This application was already reviewed.");

            return Ok(application);
        }
    }
}
