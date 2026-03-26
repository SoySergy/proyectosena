using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.DTOs.User;
using proyectosena.Interfaces;
using proyectosena.Models;

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
        // Solo Admin puede ver todos los usuarios
        [HttpGet("GetUsers")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _userRepository.GetUsers();

                if (users == null || !users.Any())
                    return NotFound("No registered users were found.");

                // Mapea a UserInfoDto para no exponer el campo Password
                return Ok(users.Select(MapToUserInfoDto).ToList());
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving users.");
            }
        }

        // -------------------- GET: api/user/GetUserById --------------------
        // Cualquier usuario autenticado puede ver su propio perfil
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

                // Mapea a UserInfoDto para no exponer el campo Password
                return Ok(MapToUserInfoDto(user));
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the user.");
            }
        }

        // -------------------- GET: api/user/GetUsersByRole --------------------
        // Solo Admin puede consultar usuarios por rol (útil para gestionar asignaciones)
        [HttpGet("GetUsersByRole")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUsersByRole(string roleName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleName))
                    return BadRequest("Role name cannot be empty.");

                var users = await _userRepository.GetByRoleNameAsync(roleName);

                if (users == null || !users.Any())
                    return NotFound($"No users found with role '{roleName}'.");

                return Ok(users.Select(MapToUserInfoDto).ToList());
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving users by role.");
            }
        }

        // -------------------- GET: api/user/GetUserByEmail --------------------
        // Solo Admin puede buscar usuarios por correo electrónico
        [HttpGet("GetUserByEmail")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest("Email cannot be empty.");

                var user = await _userRepository.GetUserByEmail(email);

                if (user == null)
                    return NotFound("No user found with that email.");

                return Ok(MapToUserInfoDto(user));
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the user by email.");
            }
        }

        // -------------------- GET: api/user/GetUserByName --------------------
        // Solo Admin puede buscar usuarios por nombre
        [HttpGet("GetUserByName")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserByName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest("Name cannot be empty.");

                var user = await _userRepository.GetUserByName(name);

                if (user == null)
                    return NotFound("No user found with that name.");

                return Ok(MapToUserInfoDto(user));
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the user by name.");
            }
        }

        // -------------------- GET: api/user/GetUserByDocument --------------------
        // Solo Admin puede buscar un usuario por tipo y número de documento
        // La combinación de ambos campos identifica unicamente al usuario
        [HttpGet("GetUserByDocument")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserByDocument(string documentNumber, Guid idDocumentType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(documentNumber))
                    return BadRequest("Document number cannot be empty.");

                if (idDocumentType == Guid.Empty)
                    return BadRequest("Document type is required.");

                var user = await _userRepository.GetUserByDocument(documentNumber, idDocumentType);

                if (user == null)
                    return NotFound("No user found with that document number and type.");

                return Ok(MapToUserInfoDto(user));
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the user by document.");
            }
        }

        // -------------------- PUT: api/user/UpdateUser --------------------
        // Permite actualizar datos del perfil y opcionalmente la contraseña
        [HttpPut("UpdateUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUser(Guid idUser, [FromBody] UpdateUserDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("Update data cannot be null.");

                // Busca el usuario existente para modificarlo
                var user = await _userRepository.GetUser(idUser);
                if (user == null)
                    return NotFound("The requested user was not found.");

                // Actualiza solo los campos que vienen en el DTO
                if (dto.Name != null) user.Name = dto.Name;
                if (dto.LastName != null) user.LastName = dto.LastName;
                if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
                if (dto.Address != null) user.Address = dto.Address;

                // Si quiere cambiar contraseña verifica la actual con BCrypt
                if (!string.IsNullOrEmpty(dto.NewPassword))
                {
                    if (string.IsNullOrEmpty(dto.CurrentPassword))
                        return BadRequest("Current password is required to set a new password.");

                    // Verifica que la contraseña actual sea correcta antes de cambiarla
                    if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.Password))
                        return BadRequest("Current password is incorrect.");

                    // Hashea la nueva contraseña antes de guardarla
                    user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                }

                var updated = await _userRepository.UpdateUser(user);

                // Retorna el perfil actualizado sin exponer Password
                return Ok(MapToUserInfoDto(updated));
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating the user.");
            }
        }

        // -------------------- DELETE: api/user/DeleteUser --------------------
        // Solo Admin puede eliminar usuarios
        [HttpDelete("DeleteUser")]
        [Authorize(Policy = "AdminOnly")]
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

        // ── Métodos privados ────────────────────────────────────────────

        // Mapea el modelo User al DTO de respuesta sin exponer datos sensibles
        private static UserInfoDto MapToUserInfoDto(User user) => new()
        {
            IdUser = user.IdUser,
            IdRole = user.IdRole,
            RoleName = user.Role?.RoleName ?? string.Empty,
            IdDocumentType = user.IdDocumentType,
            DocumentTypeName = user.DocumentType?.DocumentName ?? string.Empty,
            DocumentNumber = user.DocumentNumber,
            Name = user.Name,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            RegistrationDate = user.RegistrationDate
        };
    }
}