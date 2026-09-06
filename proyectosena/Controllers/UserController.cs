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
    public class UserController : ControllerBase
    {
        // El controlador solo traduce HTTP: las reglas viven en el servicio
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // -------------------- GET: api/user/GetUsers --------------------
        // Solo Admin puede ver todos los usuarios
        [HttpGet("GetUsers")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 20)
        {
            return Ok(await _userService.GetUsers(page, pageSize));
        }

        // -------------------- GET: api/user/GetUserById --------------------
        // Cualquier usuario autenticado puede ver su propio perfil
        [HttpGet("GetUserById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(Guid idUser)
        {
            var user = await _userService.GetById(idUser);

            if (user == null)
                return NotFound("The requested user was not found.");

            return Ok(user);
        }

        // -------------------- GET: api/user/GetUsersByRole --------------------
        [HttpGet("GetUsersByRole")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUsersByRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return BadRequest("Role name cannot be empty.");

            var users = await _userService.GetByRole(roleName);

            if (!users.Any())
                return NotFound($"No users found with role '{roleName}'.");

            return Ok(users);
        }

        // -------------------- GET: api/user/GetUserByEmail --------------------
        [HttpGet("GetUserByEmail")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email cannot be empty.");

            var user = await _userService.GetByEmail(email);

            if (user == null)
                return NotFound("No user found with that email.");

            return Ok(user);
        }

        // -------------------- GET: api/user/GetUserByName --------------------
        [HttpGet("GetUserByName")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Name cannot be empty.");

            var user = await _userService.GetByName(name);

            if (user == null)
                return NotFound("No user found with that name.");

            return Ok(user);
        }

        // -------------------- GET: api/user/GetUserByDocument --------------------
        // La combinación de tipo y número identifica únicamente al usuario
        [HttpGet("GetUserByDocument")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserByDocument(string documentNumber, Guid idDocumentType)
        {
            if (string.IsNullOrWhiteSpace(documentNumber))
                return BadRequest("Document number cannot be empty.");

            if (idDocumentType == Guid.Empty)
                return BadRequest("Document type is required.");

            var user = await _userService.GetByDocument(documentNumber, idDocumentType);

            if (user == null)
                return NotFound("No user found with that document number and type.");

            return Ok(user);
        }

        // -------------------- PUT: api/user/UpdateUser --------------------
        // Actualiza el perfil y, si se envía, la contraseña
        [HttpPut("UpdateUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(Guid idUser, [FromBody] UpdateUserDto dto)
        {
            if (dto == null)
                return BadRequest("Update data cannot be null.");

            var (result, user) = await _userService.UpdateUser(idUser, dto);

            if (result == UserUpdateResult.UserNotFound)
                return NotFound("The requested user was not found.");

            if (result == UserUpdateResult.CurrentPasswordRequired)
                return BadRequest("Current password is required to set a new password.");

            if (result == UserUpdateResult.CurrentPasswordIncorrect)
                return BadRequest("Current password is incorrect.");

            return Ok(user);
        }

        // -------------------- DELETE: api/user/DeleteUser --------------------
        // Baja lógica: el usuario deja de estar activo pero conserva su historial
        [HttpDelete("DeleteUser")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteUser(Guid idUser)
        {
            var deactivated = await _userService.Deactivate(idUser);

            if (!deactivated)
                return BadRequest("Could not delete the user. Please verify it exists.");

            return Ok("User deleted successfully.");
        }
    }
}
