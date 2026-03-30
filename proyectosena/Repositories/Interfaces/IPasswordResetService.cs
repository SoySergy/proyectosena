namespace proyectosena.Repositories.Interfaces
{
    public interface IPasswordResetService
    {
        /// <summary>Genera un código de 6 dígitos, lo almacena en memoria y lo devuelve.</summary>
        string GenerateAndStoreCode(string email);

        /// <summary>Devuelve true si el código es válido y no ha expirado (15 min).</summary>
        bool ValidateCode(string email, string code);

        /// <summary>Elimina el código tras un restablecimiento exitoso.</summary>
        void InvalidateCode(string email);
    }
}