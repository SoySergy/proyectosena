using System.Security.Claims;

namespace proyectosena.Extensions
{
    /// <summary>
    /// Lee del token quién está llamando.
    /// </summary>
    /// <remarks>
    /// Hasta ahora ningún controlador leía el token: el cliente decía quién era
    /// en la query string y el backend le creía. Bastaba cambiar un número en la
    /// URL para leer los datos de otra persona.
    ///
    /// <para>
    /// El identificador sale del claim <c>NameIdentifier</c>, que
    /// <c>AuthService</c> firma al emitir el token. Como está firmado, el cliente
    /// no puede alterarlo sin invalidar la firma.
    /// </para>
    ///
    /// <para>
    /// El controlador es quien lee esto y le pasa al servicio un identificador
    /// en el que ya se puede confiar. Así los servicios siguen sin saber nada de
    /// HTTP, que es como quedaron tras <c>BE-13</c> y <c>BE-24</c>.
    /// </para>
    /// </remarks>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>Id del usuario autenticado que hace la petición.</summary>
        public static Guid GetUserId(this ClaimsPrincipal principal)
        {
            var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            // No debería pasar: estos endpoints exigen [Authorize], así que el
            // token ya se validó. Si pasa, es un token corrupto y es mejor que
            // salte en el log a servir datos de alguien equivocado.
            if (!Guid.TryParse(raw, out var idUser))
                throw new InvalidOperationException(
                    "El token no trae un identificador de usuario válido.");

            return idUser;
        }

        /// <summary>True si quien llama es administrador.</summary>
        public static bool IsAdministrator(this ClaimsPrincipal principal)
            => principal.IsInRole("Administrator");
    }
}
