using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using proyectosena.DTOs.Auth;
using proyectosena.DTOs.Auth.Password;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        // El controlador solo traduce HTTP: las reglas viven en el servicio
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // -------------------- POST: api/auth/Register --------------------
        // AllowAnonymous permite registrarse sin token
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [HttpPost("Register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var (result, pending) = await _authService.Register(dto);

            if (result == RegisterResult.EmailAlreadyUsed)
                return BadRequest("Ya existe un usuario con este correo.");

            if (result == RegisterResult.DocumentAlreadyUsed)
                return BadRequest("El número de documento ya se encuentra registrado con este tipo de documento.");

            if (result == RegisterResult.DuplicateOnSave)
                return BadRequest("El número de identificación ya se encuentra registrado.");

            // Ya no se devuelve token: la cuenta existe pero no sirve hasta que
            // confirme el correo con el código que acaba de recibir.
            return Ok(pending);
        }

        // -------------------- POST: api/auth/Login --------------------
        // AllowAnonymous permite acceder sin token — es el endpoint de autenticación
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [HttpPost("Login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var (result, response) = await _authService.Login(dto);

            if (result == LoginResult.InvalidCredentials)
                return Unauthorized("Invalid credentials.");

            if (result == LoginResult.EmailNotVerified)
                return StatusCode(StatusCodes.Status403Forbidden,
                    "Debes confirmar tu correo antes de entrar. Revisa tu bandeja o pide un código nuevo.");

            return Ok(response);
        }

        // ─────────────────────────────────────────────────────────────────
        // POST: api/auth/forgot-password
        // Genera el código OTP, lo guarda en memoria y envía el correo.
        // ─────────────────────────────────────────────────────────────────
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Email)]
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("El correo es requerido.");

            await _authService.RequestPasswordReset(dto.Email);

            // Respuesta idéntica exista o no el correo (evita enumerar emails)
            return Ok(new { message = "Si el correo está registrado, recibirás un código." });
        }

        // ─────────────────────────────────────────────────────────────────
        // POST: api/auth/verify-reset-code
        // Verifica que el código sea válido antes de mostrar el campo
        // de nueva contraseña en el frontend.
        // ─────────────────────────────────────────────────────────────────
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [HttpPost("verify-reset-code")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult VerifyResetCode([FromBody] VerifyResetCodeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Code))
                return BadRequest("Correo y código son requeridos.");

            if (!_authService.VerifyResetCode(dto.Email, dto.Code))
                return BadRequest("Código inválido o expirado.");

            return Ok(new { message = "Código verificado correctamente." });
        }

        // ─────────────────────────────────────────────────────────────────
        // POST: api/auth/reset-password
        // Valida el código y actualiza la contraseña hasheada con BCrypt.
        // ─────────────────────────────────────────────────────────────────
        // POST: api/auth/verify-email
        // Confirma que el correo del recién registrado existe y es suyo.
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [HttpPost("verify-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Code))
                return BadRequest("Correo y código son requeridos.");

            var (result, response) = await _authService.VerifyEmail(dto);

            if (result == EmailVerificationResult.InvalidOrExpiredCode)
                return BadRequest("Código inválido o expirado.");

            if (result == EmailVerificationResult.UserNotFound)
                return NotFound("Usuario no encontrado.");

            if (result == EmailVerificationResult.AlreadyVerified)
                return Ok(new { message = "Este correo ya estaba confirmado. Puedes iniciar sesión." });

            // Queda con la sesión iniciada: acaba de demostrar que el correo es suyo
            return Ok(response);
        }

        // POST: api/auth/resend-verification
        // Vuelve a mandar el código si no llegó o venció.
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Email)]
        [HttpPost("resend-verification")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResendVerification([FromBody] ForgotPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("El correo es requerido.");

            await _authService.ResendVerificationCode(dto.Email);

            // Respuesta idéntica exista o no la cuenta, y esté o no confirmada
            return Ok(new { message = "Si la cuenta existe y falta confirmarla, recibirás un código." });
        }

        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Code) ||
                string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest("Correo, código y nueva contraseña son requeridos.");

            var result = await _authService.ResetPassword(dto);

            if (result == ResetPasswordResult.InvalidOrExpiredCode)
                return BadRequest("Código inválido o expirado.");

            if (result == ResetPasswordResult.UserNotFound)
                return NotFound("Usuario no encontrado.");

            return Ok(new { message = "Contraseña actualizada correctamente." });
        }
    }
}
