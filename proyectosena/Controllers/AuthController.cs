using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using proyectosena.DTOs.Auth;
using proyectosena.DTOs.User;
using proyectosena.Interfaces;
using proyectosena.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        // Repositorio de usuarios para buscar credenciales
        private readonly IUserRepository _userRepository;

        // Configuración para leer las claves JWT del appsettings.json
        private readonly IConfiguration _configuration;

        public AuthController(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        // -------------------- POST: api/auth/Register --------------------
        // AllowAnonymous permite registrarse sin token
        [AllowAnonymous]
        [HttpPost("Register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                // Verifica que el correo no esté ya registrado en la BD
                var existing = await _userRepository.GetUserByEmail(dto.Email);
                if (existing != null)
                    return BadRequest("A user with this email already exists.");

                // Construye el modelo User desde el DTO de registro
                var user = new User
                {
                    IdUser = Guid.NewGuid(),
                    IdRole = dto.IdRole,
                    IdDocumentType = dto.IdDocumentType,
                    DocumentNumber = dto.DocumentNumber,
                    Name = dto.Name,
                    LastName = dto.LastName,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    Email = dto.Email,
                    // Hashea la contraseña antes de guardarla — nunca se guarda en texto plano
                    Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    RegistrationDate = DateTime.UtcNow
                };

                var newUser = await _userRepository.CreateUser(user);

                // Genera el token JWT para que el usuario quede autenticado al registrarse
                var token = GenerateToken(newUser);

                // Retorna el token y la información básica del usuario sin exponer Password
                return Ok(new AuthResponseDto
                {
                    Token = token,
                    TokenType = "Bearer",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                    User = MapToUserInfoDto(newUser)
                });
            }
            // DESPUÉS ✅
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
                      && (sqlEx.Number == 2627 || sqlEx.Number == 2601))
            {
                // Viola la constraint UQ_User_Document — documento duplicado
                return BadRequest("El número de identificación ya se encuentra registrado.");
            }
            catch (Exception ex)
            {
                // Cualquier otro error inesperado — nunca se expone el detalle interno
                return StatusCode(StatusCodes.Status500InternalServerError,
                                  "Ocurrió un error inesperado. Por favor intente más tarde.");
            }
        }

        // -------------------- POST: api/auth/Login --------------------
        // AllowAnonymous permite acceder sin token — es el endpoint de autenticación
        [AllowAnonymous]
        [HttpPost("Login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                // Busca el usuario por correo electrónico incluyendo su rol
                var user = await _userRepository.GetUserByEmail(dto.Email);
                if (user == null)
                    return Unauthorized("Invalid credentials.");

                if (user.Role == null)
                {
                    throw new Exception("Role is null");
                }

                // Verifica la contraseña usando BCrypt
                // BCrypt.Verify compara el texto plano con el hash almacenado en la BD
                // Nunca se desencripta — BCrypt hashea el intento y compara los hashes
                if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
                    return Unauthorized(new
                    {
                        message = "Invalid crendetials"

                    });

                // Genera el token JWT con los datos del usuario autenticado
                var token = GenerateToken(user);

                // Retorna el token y la información básica sin exponer Password
                return Ok(new AuthResponseDto
                {
                    Token = token,
                    TokenType = "Bearer",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                    User = MapToUserInfoDto(user)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // ── Métodos privados ────────────────────────────────────────────

        // Genera el token JWT con los claims del usuario
        // Se reutiliza tanto en Login como en Register
        private string GenerateToken(User user)
        {
            // Clave secreta para firmar el token, leída desde appsettings.json
            var secretKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            // Credenciales de firma usando el algoritmo HmacSha256
            var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            // Construye el token con issuer, audience, claims y expiración
            var tokenOptions = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: new List<Claim>
                {
                    // IdUser en el token — el frontend lo usa para identificar al usuario
                    new Claim(ClaimTypes.NameIdentifier, user.IdUser.ToString()),
                    // El email se usa como nombre de usuario en el token
                    new Claim(ClaimTypes.Name, user.Email),
                    // El rol permite aplicar políticas de autorización por rol
                    new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Citizen")
                },
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: signinCredentials
            );

            // Serializa el token a string y lo retorna
            return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        }

        // Mapea el modelo User al DTO de respuesta sin exponer datos sensibles
        // Se usa tanto en Login como en Register para no duplicar código
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