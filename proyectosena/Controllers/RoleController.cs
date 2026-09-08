using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.DTOs.User;
using proyectosena.Interfaces.Services;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        // El controlador solo traduce HTTP: las reglas viven en el servicio
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        // -------------------- GET: api/role/GetRoles --------------------
        // Lista los roles para el panel de administración
        [HttpGet("GetRoles")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRoles()
        {
            return Ok(await _roleService.GetAll());
        }

        // -------------------- GET: api/role/GetRoleById --------------------
        [HttpGet("GetRoleById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRoleById(Guid idRole)
        {
            var role = await _roleService.GetById(idRole);

            if (role == null)
                return NotFound("The requested role was not found.");

            return Ok(role);
        }

        // -------------------- POST: api/role/CreateRole --------------------
        [HttpPost("CreateRole")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateRole([FromBody] RoleDto role)
        {
            if (role == null)
                return BadRequest("Role data cannot be null.");

            return Ok(await _roleService.Create(role));
        }

        // -------------------- PUT: api/role/UpdateRole --------------------
        [HttpPut("UpdateRole")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateRole([FromBody] RoleDto role)
        {
            if (role == null)
                return BadRequest("Role data cannot be null.");

            return Ok(await _roleService.Update(role));
        }

        // -------------------- DELETE: api/role/DeleteRole --------------------
        [HttpDelete("DeleteRole")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteRole(Guid idRole)
        {
            var deleted = await _roleService.Delete(idRole);

            // Retorna false si el rol no existe en la base de datos
            if (!deleted)
                return BadRequest("Could not delete the role. Please verify it exists.");

            return Ok("Role deleted successfully.");
        }
    }
}
