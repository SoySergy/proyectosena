using System.Collections.Concurrent;
using proyectosena.Interfaces.Services;

namespace proyectosena.Services
{
    /// <summary>
    /// Tokens que dejaron de valer antes de su fecha, guardados en memoria.
    /// </summary>
    /// <remarks>
    /// Existe porque cerrar sesión no cerraba nada. El botón solo borraba el
    /// token del navegador, pero el servidor lo seguía aceptando durante la
    /// hora que le quedaba de vida: se comprobó guardando un token, cerrando
    /// sesión y volviendo a usarlo, y respondía 200 igual que antes.
    ///
    /// <para>
    /// Se registra como Singleton en DependencyInjection.cs, igual que
    /// <see cref="VerificationCodeService"/>: como Scoped, cada petición
    /// recibiría una lista vacía y ningún token quedaría revocado.
    /// </para>
    ///
    /// <para>
    /// Guardar esto en memoria tiene un límite que conviene saber: si la API
    /// se reinicia, la lista se vacía y los tokens revocados vuelven a valer
    /// hasta que caduquen solos. Aguantarlo entre reinicios pediría una tabla
    /// en la base de datos y una consulta en cada petición.
    /// </para>
    /// </remarks>
    public class RevokedTokenService : IRevokedTokenService
    {
        // Clave: el "jti" del token | Valor: cuándo habría caducado por su cuenta.
        private readonly ConcurrentDictionary<string, DateTime> _revocados = new();

        // Cada tantas revocaciones se barren las caducadas. Sin esto, la lista
        // solo crecería: un token revocado hace una semana ya no le estorba a
        // nadie, pero seguiría ocupando sitio.
        private const int RevocacionesEntreLimpiezas = 100;

        private int _desdeLaUltimaLimpieza;

        public void Revoke(string tokenId, DateTime expiraEn)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return;

            _revocados[tokenId] = expiraEn;

            if (Interlocked.Increment(ref _desdeLaUltimaLimpieza) >= RevocacionesEntreLimpiezas)
            {
                Interlocked.Exchange(ref _desdeLaUltimaLimpieza, 0);
                LimpiarCaducados();
            }
        }

        public bool IsRevoked(string tokenId)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return false;

            if (!_revocados.TryGetValue(tokenId, out var expiraEn))
                return false;

            // Si ya habría caducado por su cuenta, sobra tenerlo en la lista:
            // el propio validador del token lo rechazará por vencido.
            if (expiraEn <= DateTime.UtcNow)
            {
                _revocados.TryRemove(tokenId, out _);
                return false;
            }

            return true;
        }

        private void LimpiarCaducados()
        {
            var ahora = DateTime.UtcNow;

            foreach (var (tokenId, expiraEn) in _revocados)
            {
                if (expiraEn <= ahora)
                    _revocados.TryRemove(tokenId, out _);
            }
        }
    }
}
