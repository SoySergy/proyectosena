using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Services
{
    /// <summary>
    /// Códigos de un solo uso, guardados en la base: recuperar la contraseña,
    /// confirmar el correo del registro e invitar a un gestor.
    /// </summary>
    /// <remarks>
    /// Antes vivían en un diccionario en memoria (B-6 · WA-12): un reinicio de la
    /// API los borraba todos, y con dos réplicas cada una tenía los suyos, así que
    /// el código emitido por una no validaba en la otra.
    /// </remarks>
    public class VerificationCodeService : IVerificationCodeService
    {
        private readonly RecyRouteDbContext _context;

        private const int ExpiryMinutes = 15;

        // Cinco intentos por código. Sin esto, un código de seis cifras se adivina
        // probando: caben 33.300 pruebas en los quince minutos que vive.
        private const int MaxFailedAttempts = 5;

        // Cada tantos códigos emitidos se borran los vencidos que nadie usó.
        // Estático: el servicio es Scoped y cada petición recibe una instancia nueva.
        private const int EmisionesEntreLimpiezas = 50;
        private static int _desdeLaUltimaLimpieza;

        public VerificationCodeService(RecyRouteDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateAndStoreCode(string email, CodePurpose purpose, int expiryMinutes = ExpiryMinutes)
        {
            // Generador criptográfico: `Random` no promete ser impredecible, y basta
            // con que alguien le pase una semilla para que los códigos se repitan.
            // Desde 0 para no descartar los que empiezan por cero: un millón de
            // combinaciones en vez de novecientas mil.
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var key = Key(email, purpose);
            var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

            // Pedir otro código reemplaza al anterior y reinicia los fallos. Upsert y
            // no leer-y-escribir: dos peticiones a la vez para el mismo correo
            // chocarían contra la clave primaria.
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""VerificationCode"" (""Key"", ""Code"", ""Expiry"", ""FailedAttempts"")
                VALUES ({key}, {code}, {expiry}, 0)
                ON CONFLICT (""Key"") DO UPDATE
                SET ""Code"" = EXCLUDED.""Code"",
                    ""Expiry"" = EXCLUDED.""Expiry"",
                    ""FailedAttempts"" = 0");

            if (Interlocked.Increment(ref _desdeLaUltimaLimpieza) >= EmisionesEntreLimpiezas)
            {
                Interlocked.Exchange(ref _desdeLaUltimaLimpieza, 0);
                var ahora = DateTime.UtcNow;
                await _context.VerificationCodes
                    .Where(v => v.Expiry <= ahora)
                    .ExecuteDeleteAsync();
            }

            return code;
        }

        public async Task<bool> ValidateCode(string email, string code, CodePurpose purpose)
        {
            var key = Key(email, purpose);
            var ahora = DateTime.UtcNow;

            var entry = await _context.VerificationCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Key == key);

            if (entry == null)
                return false;

            // Si expiró, limpia la entrada y rechaza. El filtro por fecha evita
            // borrar uno nuevo que se haya pedido mientras tanto.
            if (entry.Expiry <= ahora)
            {
                await _context.VerificationCodes
                    .Where(v => v.Key == key && v.Expiry <= ahora)
                    .ExecuteDeleteAsync();
                return false;
            }

            if (entry.Code == code.Trim())
                return true;

            // Cada fallo cuenta, y al quinto el código muere: hay que pedir otro.
            // La suma la hace la base (FailedAttempts + 1) y no esta petición: el
            // ataque llega en paralelo y leer-sumar-escribir perdería fallos.
            await _context.VerificationCodes
                .Where(v => v.Key == key)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.FailedAttempts, v => v.FailedAttempts + 1));

            await _context.VerificationCodes
                .Where(v => v.Key == key && v.FailedAttempts >= MaxFailedAttempts)
                .ExecuteDeleteAsync();

            return false;
        }

        public async Task InvalidateCode(string email, CodePurpose purpose)
        {
            var key = Key(email, purpose);
            await _context.VerificationCodes
                .Where(v => v.Key == key)
                .ExecuteDeleteAsync();
        }

        // Un código guardado bajo un propósito no se encuentra bajo otro
        private static string Key(string email, CodePurpose purpose)
            => $"{purpose}:{email.ToLowerInvariant()}";
    }
}
