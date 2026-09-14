using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.DTOs.User;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

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
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRole([FromBody] RoleDto role)
        {
            if (role == null)
                return BadRequest("Role data cannot be null.");

            var (result, updated) = await _roleService.Update(role);

            if (result == CatalogMutationResult.NotFound)
                return NotFound("The requested role was not found.");

            // Renombrar Administrator, Manager o Citizen dejaría fuera en
            // silencio a quien inicie sesión después: las políticas de
            // autorización comparan por ese nombre, escrito en el código.
            if (result == CatalogMutationResult.NombreDelSistema)
                return BadRequest("This role's name can't be changed: the authorization policies depend on it.");

            return Ok(updated);
        }

        // -------------------- DELETE: api/role/DeleteRole --------------------
        [HttpDelete("DeleteRole")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteRole(Guid idRole)
        {
            var result = await _roleService.Delete(idRole);

            if (result == CatalogMutationResult.NotFound)
                return NotFound("The requested role was not found.");

            if (result == CatalogMutationResult.InUse)
                return Conflict("This role is still assigned to at least one user.");

            return Ok("Role deleted successfully.");
        }
    }
}
