namespace proyectosena.Interfaces.Services
{
    /// <summary>
    /// Lleva la cuenta de los tokens que dejaron de valer antes de tiempo,
    /// para que cerrar sesión signifique algo del lado del servidor.
    /// </summary>
    public interface IRevokedTokenService
    {
        /// <summary>Marca un token como inservible desde ya.</summary>
        /// <param name="tokenId">El identificador único del token (claim "jti").</param>
        /// <param name="expiraEn">Cuándo habría caducado solo. Pasada esa fecha se olvida.</param>
        void Revoke(string tokenId, DateTime expiraEn);

        /// <summary>Devuelve true si el token fue revocado y ya no debe aceptarse.</summary>
        bool IsRevoked(string tokenId);
    }
}
