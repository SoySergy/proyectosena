namespace proyectosena.Interfaces.Services
{
    public interface IPasswordResetService
    {
        /// <summary>Genera un código de 6 dígitos, lo almacena en memoria y lo devuelve.</summary>
        /// <param name="expiryMinutes">Minutos de vigencia. 15 por defecto; las invitaciones usan más.</param>
        string GenerateAndStoreCode(string email, int expiryMinutes = 15);

        /// <summary>Devuelve true si el código es válido y no ha expirado (15 min).</summary>
        bool ValidateCode(string email, string code);

        /// <summary>Elimina el código tras un restablecimiento exitoso.</summary>
        void InvalidateCode(string email);
    }
}