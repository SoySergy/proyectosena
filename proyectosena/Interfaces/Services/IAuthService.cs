using proyectosena.DTOs.Auth;
using proyectosena.DTOs.Auth.Password;
using proyectosena.DTOs.User;
using proyectosena.Models;

namespace proyectosena.Interfaces.Services
{
    /// <summary>
    /// Reglas de autenticación: registro, inicio de sesión, emisión del token
    /// JWT y el flujo de recuperación de contraseña.
    /// </summary>
    /// <remarks>
    /// El controlador no decide nada sobre estas reglas: recibe un resultado y
    /// lo traduce a un código HTTP. Así la misma regla —por ejemplo, que un
    /// login fallido nunca revele si el correo existe— vale igual si mañana
    /// entra por otra vía que no sea un controlador REST.
    /// </remarks>
    public interface IAuthService
    {
        // Crea la cuenta SIN sesión: queda pendiente de confirmar el correo.
        // Pending viene en null salvo que el resultado sea Success.
        Task<(RegisterResult Result, RegistrationPendingDto? Pending)> Register(RegisterDto dto);

        // Valida credenciales y emite el token.
        // Response viene en null salvo que el resultado sea Success.
        Task<(LoginResult Result, AuthResponseDto? Response)> Login(LoginDto dto);

        // Envía el código de recuperación si el correo existe. No devuelve nada:
        // quien llama debe responder lo mismo exista o no la cuenta.
        Task RequestPasswordReset(string email);

        // True si el código sigue vigente. Solo comprueba; no consume el código.
        bool VerifyResetCode(string email, string code);

        // Cambia la contraseña y quema el código.
        Task<ResetPasswordResult> ResetPassword(ResetPasswordDto dto);

        // Confirma el correo y, si sale bien, deja la sesión iniciada: quien
        // acaba de confirmar no tiene por qué volver a escribir su contraseña.
        // Response viene en null salvo que el resultado sea Success.
        Task<(EmailVerificationResult Result, AuthResponseDto? Response)> VerifyEmail(VerifyEmailDto dto);

        // Vuelve a mandar el código si no llegó o venció. No devuelve nada: quien
        // llama responde igual exista o no la cuenta.
        Task ResendVerificationCode(string email);
    }
}
