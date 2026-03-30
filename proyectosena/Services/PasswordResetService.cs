using System.Collections.Concurrent;
using proyectosena.Repositories.Interfaces;

namespace proyectosena.Services
{
    /// <summary>
    /// Almacena los códigos OTP en memoria.
    /// Se registra como Singleton en Program.cs para que el diccionario persista entre peticiones.
    /// </summary>
    public class PasswordResetService : IPasswordResetService
    {
        // Clave: email en minúsculas | Valor: (código, fecha de expiración)
        private readonly ConcurrentDictionary<string, (string Code, DateTime Expiry)> _store = new();

        private const int ExpiryMinutes = 15;

        public string GenerateAndStoreCode(string email)
        {
            // Número aleatorio de 6 dígitos (100000 – 999999)
            var code = new Random().Next(100_000, 1_000_000).ToString("D6");
            _store[email.ToLowerInvariant()] = (code, DateTime.UtcNow.AddMinutes(ExpiryMinutes));
            return code;
        }

        public bool ValidateCode(string email, string code)
        {
            var key = email.ToLowerInvariant();

            if (!_store.TryGetValue(key, out var entry))
                return false;

            // Si expiró, limpia la entrada y rechaza
            if (DateTime.UtcNow > entry.Expiry)
            {
                _store.TryRemove(key, out _);
                return false;
            }

            return entry.Code == code.Trim();
        }

        public void InvalidateCode(string email)
        {
            _store.TryRemove(email.ToLowerInvariant(), out _);
        }
    }
}
