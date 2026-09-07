namespace proyectosena.DTOs.User
{
    /// <summary>
    /// Lo que responde el registro ahora: la cuenta quedó creada pero sin
    /// confirmar, así que no hay sesión todavía.
    /// </summary>
    /// <remarks>
    /// Antes el registro devolvía el token de inmediato y quien se registraba
    /// entraba sin que nadie comprobara que el correo existiera.
    /// </remarks>
    public class RegistrationPendingDto
    {
        public string Message { get; set; } = string.Empty;

        // El frontend lo necesita para la pantalla donde se ingresa el código
        public string Email { get; set; } = string.Empty;

        public int ExpiresInMinutes { get; set; }
    }
}
