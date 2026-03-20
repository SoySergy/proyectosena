using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.Models;
using proyectosena.Interfaces;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        // Repositorio de usuarios inyectado por dependencias
        private readonly IUserRepository _userRepository;

        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // -------------------- GET: api/user/GetUsers --------------------
        [HttpGet("GetUsers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _userRepository.GetUsers();

                // Verifica si la lista está vacía o nula
                if (users == null || !users.Any())
                    return NotFound("No registered users were found.");

                return Ok(users);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving users.");
            }
        }

        // -------------------- GET: api/user/GetUserById --------------------
        [HttpGet("GetUserById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserById(Guid idUser)
        {
            try
            {
                var user = await _userRepository.GetUser(idUser);

                if (user == null)
                    return NotFound("The requested user was not found.");

                // Incluye sus datos de Role y DocumentType gracias al Include() del repositorio
                return Ok(user);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the user.");
            }
        }

        // -------------------- POST: api/user/CreateUser --------------------
        [HttpPost("CreateUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            try
            {
                if (user == null)
                    return BadRequest("User data cannot be null.");

                // Validación simple de claves foráneas
                if (user.IdRole == Guid.Empty || user.IdDocumentType == Guid.Empty)
                    return BadRequest("The user must have a valid IdRole and IdDocumentType.");

                var newUser = await _userRepository.CreateUser(user);
                return Ok(newUser);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating the user.");
            }
        }

        // -------------------- PUT: api/user/UpdateUser --------------------
        [HttpPut("UpdateUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUser([FromBody] User user)
        {
            try
            {
                if (user == null)
                    return BadRequest("User data cannot be null.");

                // El IdUser es obligatorio para identificar el registro a actualizar
                if (user.IdUser == Guid.Empty)
                    return BadRequest("IdUser is required to update a record.");

                var updated = await _userRepository.UpdateUser(user);
                return Ok(updated);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating the user.");
            }
        }

        // -------------------- DELETE: api/user/DeleteUser --------------------
        [HttpDelete("DeleteUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUser(Guid idUser)
        {
            try
            {
                var deleted = await _userRepository.DeleteUser(idUser);

                // Retorna false si el usuario no existe en la base de datos
                if (!deleted)
                    return BadRequest("Could not delete the user. Please verify it exists.");

                return Ok("User deleted successfully.");
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting the user.");
            }
        }
    }
}