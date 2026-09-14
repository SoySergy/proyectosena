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

        /// <summary>
        /// Invalida de golpe todos los tokens de un usuario, sin importar cuántos
        /// tenga sueltos por ahí. Se usa cuando algo hace que confiar en un token
        /// viejo ya no sea seguro: lo dieron de baja, o cambió su contraseña.
        /// </summary>
        void RevokeAllForUser(Guid idUser);

        /// <summary>
        /// Devuelve true si este token de este usuario se emitió antes de la
        /// última vez que se llamó a <see cref="RevokeAllForUser"/> para él, y
        /// por tanto ya no debe aceptarse aunque su firma y su fecha sigan bien.
        /// </summary>
        bool IsRevokedForUser(Guid idUser, DateTime issuedAt);
    }
}
