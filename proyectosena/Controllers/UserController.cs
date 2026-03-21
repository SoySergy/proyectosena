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