using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.Models;
using proyectosena.Interfaces;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        // Repositorio de roles inyectado por dependencias
        private readonly IRoleRepository _roleRepository;

        public RoleController(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        // -------------------- GET: api/role/GetRoles --------------------
        [HttpGet("GetRoles")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _roleRepository.GetRoles();

                // Verifica si la lista está vacía o nula
                if (roles == null || !roles.Any())
                    return NotFound("No registered roles were found.");

                return Ok(roles);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving roles.");
            }
        }

        // -------------------- GET: api/role/GetRoleById --------------------
        [HttpGet("GetRoleById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRoleById(Guid idRole)
        {
            try
            {
                var role = await _roleRepository.GetRole(idRole);

                if (role == null)
                    return NotFound("The requested role was not found.");

                return Ok(role);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the role.");
            }
        }

        // -------------------- POST: api/role/CreateRole --------------------
        [HttpPost("CreateRole")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateRole([FromBody] Role role)
        {
            try
            {
                if (role == null)
                    return BadRequest("Role data cannot be null.");

                var newRole = await _roleRepository.CreateRole(role);
                return Ok(newRole);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating the role.");
            }
        }

        // -------------------- PUT: api/role/UpdateRole --------------------
        [HttpPut("UpdateRole")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateRole([FromBody] Role role)
        {
            try
            {
                if (role == null)
                    return BadRequest("Role data cannot be null.");

                var updated = await _roleRepository.UpdateRole(role);
                return Ok(updated);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating the role.");
            }
        }

        // -------------------- DELETE: api/role/DeleteRole --------------------
        [HttpDelete("DeleteRole")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteRole(Guid idRole)
        {
            try
            {
                var deleted = await _roleRepository.DeleteRole(idRole);

                // Retorna false si el rol no existe en la base de datos
                if (!deleted)
                    return BadRequest("Could not delete the role. Please verify it exists.");

                return Ok("Role deleted successfully.");
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting the role.");
            }
        }
    }
}