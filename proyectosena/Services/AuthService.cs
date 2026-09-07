using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using proyectosena.DTOs.Auth;
using proyectosena.DTOs.Auth.Password;
using proyectosena.DTOs.User;
using proyectosena.Extensions;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Mappers;
using proyectosena.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace proyectosena.Services
{
    public class AuthService : IAuthService
    {
        // Una sola definición de cuánto dura la sesión: la usan el token y la
        // fecha que se le informa al cliente. Antes el 60 estaba escrito tres veces.
        private const int TokenLifetimeMinutes = 60;

        private readonly IUserLookupRepository _userLookup;
        private readonly IUserWriteRepository _userWrite;
        private readonly IEmailService _emailService;
        private readonly IPasswordResetService _resetService;
        private readonly IConfiguration _configuration;

        public AuthService(
            IUserLookupRepository userLookup,
            IUserWriteRepository userWrite,
            IEmailService emailService,
            IPasswordResetService resetService,
            IConfiguration configuration)
        {
            _userLookup = userLookup;
            _userWrite = userWrite;
            _emailService = emailService;
            _resetService = resetService;
            _configuration = configuration;
        }

        public async Task<(RegisterResult Result, AuthResponseDto? Response)> Register(RegisterDto dto)
        {
            var existingEmail = await _userLookup.GetUserByEmail(dto.Email);
            if (existingEmail != null)
                return (RegisterResult.EmailAlreadyUsed, null);

            // Un mismo número puede existir con otro tipo de documento; solo es
            // duplicado si coinciden los dos campos a la vez.
            var existingDoc = await _userLookup.GetUserByDocument(dto.DocumentNumber, dto.IdDocumentType);
            if (existingDoc != null)
                return (RegisterResult.DocumentAlreadyUsed, null);

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
                // La contraseña se hashea antes de guardarla; nunca se guarda en texto plano
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RegistrationDate = DateTime.UtcNow
            };

            User created;
            try
            {
                created = await _userWrite.CreateUser(user);
            }
            // Las comprobaciones de arriba no bastan si dos personas registran el
            // mismo correo o documento en el mismo instante: ambas las pasan y el
            // índice único de la base es el que decide. Cualquier otra excepción
            // sube al manejador global.
            catch (DbUpdateException ex) when (ex.IsDuplicateKey())
            {
                return (RegisterResult.DuplicateOnSave, null);
            }

            return (RegisterResult.Success, BuildSession(created));
        }

        public async Task<(LoginResult Result, AuthResponseDto? Response)> Login(LoginDto dto)
        {
            var user = await _userLookup.GetUserByEmail(dto.Email);

            // Los tres motivos devuelven lo mismo a propósito. Responder "esa
            // cuenta no existe" o "esa cuenta está inactiva" le confirmaría a un
            // atacante qué correos están registrados.
            if (user == null)
                return (LoginResult.InvalidCredentials, null);

            if (!user.IsActive)
                return (LoginResult.InvalidCredentials, null);

            // BCrypt hashea el intento y compara los hashes; nunca descifra
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
                return (LoginResult.InvalidCredentials, null);

            // No debería pasar: IdRole es obligatorio y GetUserByEmail lo incluye.
            // Si pasa son datos corruptos, y es mejor que salte en el log a emitir
            // un token en silencio con el rol equivocado.
            if (user.Role == null)
                throw new InvalidOperationException($"User {user.IdUser} has no role loaded.");

            return (LoginResult.Success, BuildSession(user));
        }

        public async Task RequestPasswordReset(string email)
        {
            var normalized = Normalize(email);
            var user = await _userLookup.GetUserByEmail(normalized);

            // Si el correo no está registrado no se hace nada y no se avisa.
            // Quien llama responde igual en los dos casos.
            if (user == null)
                return;

            var code = _resetService.GenerateAndStoreCode(normalized);
            await _emailService.SendPasswordResetCodeAsync(user.Email, code);
        }

        public bool VerifyResetCode(string email, string code)
            => _resetService.ValidateCode(Normalize(email), code.Trim());

        public async Task<ResetPasswordResult> ResetPassword(ResetPasswordDto dto)
        {
            var email = Normalize(dto.Email);

            // El código se valida ANTES de tocar la base de datos
            if (!_resetService.ValidateCode(email, dto.Code.Trim()))
                return ResetPasswordResult.InvalidOrExpiredCode;

            var user = await _userLookup.GetUserByEmail(email);
            if (user == null)
                return ResetPasswordResult.UserNotFound;

            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _userWrite.UpdateUser(user);

            // El código se quema para que no pueda reutilizarse
            _resetService.InvalidateCode(email);

            return ResetPasswordResult.Success;
        }

        // ── Privados ────────────────────────────────────────────────────

        // El correo del flujo de recuperación se normaliza en un solo sitio.
        private static string Normalize(string email) => email.Trim().ToLower();

        // Arma la sesión que se devuelve tras un login o un registro correctos.
        private AuthResponseDto BuildSession(User user) => new()
        {
            Token = GenerateToken(user),
            TokenType = "Bearer",
            ExpiresAt = DateTime.UtcNow.AddMinutes(TokenLifetimeMinutes),
            User = user.ToInfoDto()
        };

        // Firma el token JWT con los datos del usuario autenticado
        private string GenerateToken(User user)
        {
            // Clave secreta para firmar el token, leída desde la configuración
            var secretKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var tokenOptions = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: new List<Claim>
                {
                    // IdUser en el token — el frontend lo usa para identificar al usuario
                    new Claim(ClaimTypes.NameIdentifier, user.IdUser.ToString()),
                    // El email hace de nombre de usuario
                    new Claim(ClaimTypes.Name, user.Email),
                    // El rol permite aplicar las políticas de autorización
                    new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Citizen")
                },
                expires: DateTime.UtcNow.AddMinutes(TokenLifetimeMinutes),
                signingCredentials: signinCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        }
    }
}
