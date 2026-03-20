using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using proyectosena.Models;
using proyectosena.Interfaces;
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

        // -------------------- POST: api/auth/Login --------------------
        // AllowAnonymous permite acceder sin token — es el endpoint de autenticación
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] Login login)
        {
            // Valida que el cuerpo de la petición no esté vacío
            if (login == null || string.IsNullOrEmpty(login.Email) || string.IsNullOrEmpty(login.Password))
                return BadRequest("Invalid client request.");

            // Busca el usuario por correo electrónico incluyendo su rol
            var user = await _userRepository.GetUserByEmail(login.Email);

            if (user == null)
                return Unauthorized();

            // Verifica que la contraseña coincida con la almacenada
            if (login.Password == user.Password)
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
                        // El email se usa como identificador del usuario en el token
                        new Claim(ClaimTypes.Name, login.Email),

                        // El rol permite aplicar políticas de autorización por rol
                        new Claim(ClaimTypes.Role, user.Role!.RoleName)
                    },
                    expires: DateTime.UtcNow.AddMinutes(60),
                    signingCredentials: signinCredentials
                );

                // Serializa el token a string y lo retorna al cliente
                var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
                return Ok(new { Token = tokenString });
            }

            return Unauthorized();
        }
    }
}