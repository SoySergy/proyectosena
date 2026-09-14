using proyectosena.Models;

namespace proyectosena.Interfaces.Services
{
    public interface IVerificationCodeService
    {
        /// <summary>Genera un código de 6 dígitos, lo guarda en la base y lo devuelve.</summary>
        /// <param name="purpose">Para qué sirve. Solo vale para ese propósito.</param>
        /// <param name="expiryMinutes">Minutos de vigencia. 15 por defecto; las invitaciones usan más.</param>
        Task<string> GenerateAndStoreCode(string email, CodePurpose purpose, int expiryMinutes = 15);

        /// <summary>Devuelve true si el código es válido, no ha expirado y se emitió para ese propósito.</summary>
        Task<bool> ValidateCode(string email, string code, CodePurpose purpose);

        /// <summary>Elimina el código tras usarlo con éxito.</summary>
        Task InvalidateCode(string email, CodePurpose purpose);
    }
}
