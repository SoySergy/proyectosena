namespace proyectosena.DTOs.Auth.Password
{
    /// <summary>
    /// El código que el recién registrado recibió por correo.
    /// </summary>
    /// <remarks>
    /// Vive junto a los DTOs de recuperación porque comparte el mismo mecanismo
    /// de códigos, aunque el propósito sea otro.
    /// </remarks>
    public class VerifyEmailDto
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
