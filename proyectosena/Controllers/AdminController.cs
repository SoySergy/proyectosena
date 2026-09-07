using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.DTOs.User;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Controllers
{
    // Administration operations. Every endpoint here is Administrator-only.
    [Authorize(Policy = "AdminOnly")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        // El controlador solo traduce HTTP: las reglas viven en el servicio
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // -------------------- POST: api/admin/CreateManager --------------------
        // Registers a manager. The password is never chosen here: the account is
        // created with an unusable random one and the manager sets their own
        // through the code emailed to them.
        [HttpPost("CreateManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateManager([FromBody] CreateManagerDto dto)
        {
            if (dto == null)
                return BadRequest("Manager data cannot be null.");

            var (result, idUser, email, expiresInMinutes) = await _adminService.CreateManager(dto);

            if (result == CreateManagerResult.EmailAlreadyUsed)
                return BadRequest("Ya existe un usuario con este correo.");

            if (result == CreateManagerResult.DocumentAlreadyUsed)
                return BadRequest("El número de documento ya se encuentra registrado con este tipo de documento.");

            if (result == CreateManagerResult.DuplicateOnSave)
                return BadRequest("El correo o el documento ya se encuentran registrados.");

            return Ok(new
            {
                Message = "Gestor creado. Se envió un código de activación a su correo.",
                IdUser = idUser,
                Email = email,
                ExpiresInMinutes = expiresInMinutes
            });
        }

        // -------------------- GET: api/admin/GetDashboardStats --------------------
        // Aggregated numbers for the administrator dashboard.
        [HttpGet("GetDashboardStats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardStats()
        {
            return Ok(await _adminService.GetDashboardStats());
        }

        // -------------------- PATCH: api/admin/ReassignRequest --------------------
        // Moves an active request to a different manager, for example when the
        // current one is unavailable.
        [HttpPatch("ReassignRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReassignRequest(Guid idRequest, Guid idNewManager, Guid idAdmin)
        {
            var (success, message) = await _adminService
                .ReassignRequest(idRequest, idNewManager, idAdmin);

            if (!success)
                return BadRequest(message);

            return Ok(new
            {
                Message = message,
                IdRequest = idRequest,
                IdNewManager = idNewManager,
                ReassignedAt = DateTime.UtcNow
            });
        }
    }
}
