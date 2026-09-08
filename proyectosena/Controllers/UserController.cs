using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.DTOs.User;
using proyectosena.Extensions;
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
        // Tu propio perfil. El administrador puede consultar el de cualquiera.
        [HttpGet("GetUserById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(Guid idUser)
        {
            // Antes cualquier autenticado veía el perfil de cualquier otro con solo
            // cambiar el parámetro. Ahora solo el propio, salvo el administrador,
            // que lo necesita para el panel.
            if (idUser != User.GetUserId() && !User.IsAdministrator())
                return StatusCode(StatusCodes.Status403Forbidden,
                    "You can only view your own profile.");

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
        public async Task<IActionResult> GetUsersByRole(string roleName)
        {
            // Una lista vacía no es un error: un rol sin usuarios es una respuesta
            // legítima, no un fallo (BE-15).
            //
            // El 400 por roleName vacío lo devuelve el framework antes de entrar aquí:
            // con [ApiController] y nulos activados, un string no anulable es obligatorio.
            // Había una comprobación propia que nunca llegaba a ejecutarse.
            return Ok(await _userService.GetByRole(roleName));
        }

        // -------------------- GET: api/user/GetUserByEmail --------------------
        [HttpGet("GetUserByEmail")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            // Sin comprobar que email venga: al ser un string no anulable en la URL,
            // el framework devuelve 400 antes de entrar aquí.
            var user = await _userService.GetByEmail(email);

            if (user == null)
                return NotFound("No user found with that email.");

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
            // documentNumber no se comprueba: el framework lo exige por ser un string
            // no anulable. idDocumentType sí, porque un Guid es tipo de valor y llega
            // como Guid.Empty cuando no viene — el framework lo da por bueno.
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
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto dto)
        {
            if (dto == null)
                return BadRequest("Update data cannot be null.");

            // El id sale del token: antes bastaba cambiarlo para editar el perfil
            // de cualquier otra persona.
            var (result, user) = await _userService.UpdateUser(User.GetUserId(), dto);

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
