using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Interfaces.Services;

namespace proyectosena.Services
{
    /// <summary>
    /// Tokens que dejaron de valer antes de su fecha, guardados en la base.
    /// </summary>
    /// <remarks>
    /// Existe porque cerrar sesión no cerraba nada. El botón solo borraba el
    /// token del navegador, pero el servidor lo seguía aceptando durante la
    /// hora que le quedaba de vida: se comprobó guardando un token, cerrando
    /// sesión y volviendo a usarlo, y respondía 200 igual que antes.
    ///
    /// <para>
    /// Antes la lista vivía en memoria (B-6 · WA-12): al reiniciar la API se
    /// vaciaba y los tokens anulados volvían a valer hasta caducar solos, y con
    /// dos réplicas un cierre de sesión en una no se veía en la otra. El precio
    /// de tenerla en la base es una o dos consultas más en cada petición
    /// autenticada: <c>OnTokenValidated</c>, en Program.cs, llama a
    /// <see cref="IsRevoked"/> y a <see cref="IsRevokedForUser"/> siempre.
    /// </para>
    /// </remarks>
    public class RevokedTokenService : IRevokedTokenService
    {
        private readonly RecyRouteDbContext _context;

        // Cada tantas revocaciones se borran las que ya habrían caducado solas.
        // Sin esto la tabla solo crecería. Estático: el servicio es Scoped y cada
        // petición recibe una instancia nueva.
        private const int RevocacionesEntreLimpiezas = 100;
        private static int _desdeLaUltimaLimpieza;

        public RevokedTokenService(RecyRouteDbContext context)
        {
            _context = context;
        }

        public async Task Revoke(string tokenId, DateTime expiraEn)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return;

            // Cerrar sesión dos veces con el mismo token no es un error
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""RevokedToken"" (""TokenId"", ""ExpiresAt"")
                VALUES ({tokenId}, {expiraEn})
                ON CONFLICT (""TokenId"") DO NOTHING");

            if (Interlocked.Increment(ref _desdeLaUltimaLimpieza) >= RevocacionesEntreLimpiezas)
            {
                Interlocked.Exchange(ref _desdeLaUltimaLimpieza, 0);
                var ahora = DateTime.UtcNow;
                await _context.RevokedTokens
                    .Where(t => t.ExpiresAt <= ahora)
                    .ExecuteDeleteAsync();
            }
        }

        public async Task<bool> IsRevoked(string tokenId)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return false;

            // Si ya habría caducado por su cuenta, no cuenta como revocado: el
            // propio validador del token lo rechaza por vencido.
            var ahora = DateTime.UtcNow;
            return await _context.RevokedTokens
                .AnyAsync(t => t.TokenId == tokenId && t.ExpiresAt > ahora);
        }

        public async Task RevokeAllForUser(Guid idUser)
        {
            var ahora = DateTime.UtcNow;

            // GREATEST: si dos revocaciones llegan desordenadas, la más vieja no
            // puede adelantar el corte y devolverle la validez a lo emitido entre
            // una y otra.
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""UserTokenRevocation"" (""IdUser"", ""RevokedBefore"")
                VALUES ({idUser}, {ahora})
                ON CONFLICT (""IdUser"") DO UPDATE
                SET ""RevokedBefore"" = GREATEST(""UserTokenRevocation"".""RevokedBefore"", EXCLUDED.""RevokedBefore"")");
        }

        public async Task<bool> IsRevokedForUser(Guid idUser, DateTime issuedAt)
        {
            var invalidarAntesDe = await _context.UserTokenRevocations
                .AsNoTracking()
                .Where(r => r.IdUser == idUser)
                .Select(r => (DateTime?)r.RevokedBefore)
                .FirstOrDefaultAsync();

            return invalidarAntesDe.HasValue && issuedAt < invalidarAntesDe.Value;
        }
    }
}
