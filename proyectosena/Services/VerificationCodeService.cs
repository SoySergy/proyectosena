using System.Collections.Concurrent;
using System.Security.Cryptography;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Services
{
    /// <summary>
    /// Códigos de un solo uso, guardados en memoria: recuperar la contraseña,
    /// confirmar el correo del registro e invitar a un gestor.
    /// </summary>
    /// <remarks>
    /// Se registra como Singleton en DependencyInjection.cs para que el diccionario
    /// persista entre peticiones. Como Scoped, cada petición recibiría uno vacío.
    /// </remarks>
    public class VerificationCodeService : IVerificationCodeService
    {
        // Clave: propósito + email en minúsculas | Valor: el código y sus fallos.
        // El propósito va en la clave para que el código de confirmar correo y el de
        // recuperar contraseña no se pisen ni se sirvan el uno al otro.
        private readonly ConcurrentDictionary<string, Entry> _store = new();

        private const int ExpiryMinutes = 15;

        // Cinco intentos por código. Sin esto, un código de seis cifras se adivina
        // probando: caben 33.300 pruebas en los quince minutos que vive.
        private const int MaxFailedAttempts = 5;

        public string GenerateAndStoreCode(string email, CodePurpose purpose, int expiryMinutes = ExpiryMinutes)
        {
            // Generador criptográfico: `Random` no promete ser impredecible, y basta
            // con que alguien le pase una semilla para que los códigos se repitan.
            // Desde 0 para no descartar los que empiezan por cero: un millón de
            // combinaciones en vez de novecientas mil.
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            _store[Key(email, purpose)] = new Entry(code, DateTime.UtcNow.AddMinutes(expiryMinutes));
            return code;
        }

        public bool ValidateCode(string email, string code, CodePurpose purpose)
        {
            var key = Key(email, purpose);

            if (!_store.TryGetValue(key, out var entry))
                return false;

            // Si expiró, limpia la entrada y rechaza
            if (DateTime.UtcNow > entry.Expiry)
            {
                _store.TryRemove(key, out _);
                return false;
            }

            if (entry.Code == code.Trim())
                return true;

            // Cada fallo cuenta, y al quinto el código muere: hay que pedir otro.
            // Interlocked porque el ataque llega en paralelo y un ++ normal perdería
            // incrementos, que es justo lo que el atacante querría.
            if (Interlocked.Increment(ref entry.FailedAttempts) >= MaxFailedAttempts)
                _store.TryRemove(key, out _);

            return false;
        }

        public void InvalidateCode(string email, CodePurpose purpose)
        {
            _store.TryRemove(Key(email, purpose), out _);
        }

        // Un código emitido, con los fallos que lleva encima
        private sealed class Entry
        {
            public Entry(string code, DateTime expiry)
            {
                Code = code;
                Expiry = expiry;
            }

            public string Code { get; }
            public DateTime Expiry { get; }

            // Campo y no propiedad: Interlocked.Increment lo necesita por referencia
            public int FailedAttempts;
        }

        // Un código guardado bajo un propósito no se encuentra bajo otro
        private static string Key(string email, CodePurpose purpose)
            => $"{purpose}:{email.ToLowerInvariant()}";
    }
}
