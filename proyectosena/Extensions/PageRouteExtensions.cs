using Microsoft.AspNetCore.Rewrite;
using System.Text.RegularExpressions;

namespace proyectosena.Extensions
{
    /// <summary>
    /// URLs limpias para las páginas del frontend que se sirve desde wwwroot:
    /// la barra de direcciones muestra <c>/login</c> y no <c>/pages/auth/login.html</c>.
    /// </summary>
    /// <remarks>
    /// La misma tabla está en <c>nginx/default.conf</c>, que sirve el frontend del
    /// Docker local en el 8081. Si se cambia aquí, hay que cambiarla allí.
    /// </remarks>
    public static class PageRouteExtensions
    {
        // URL limpia → archivo bajo wwwroot (sin barra inicial).
        private static readonly (string CleanPath, string File)[] Pages =
        {
            ("/login",           "pages/auth/login.html"),
            ("/register",        "pages/auth/register.html"),
            ("/verify-email",    "pages/auth/verify-email.html"),
            ("/forgot-password", "pages/auth/password/forgot-password.html"),
            ("/reset-password",  "pages/auth/password/reset-password.html"),
            ("/citizen",         "pages/citizen/dashboard.html"),
            ("/manager",         "pages/manager/dashboard.html"),
            ("/admin",           "pages/admin/dashboard.html"),
            ("/chat",            "pages/general/chat.html"),
            ("/profile",         "pages/general/settings/profile.html"),
            ("/security",        "pages/general/settings/security.html"),
        };

        /// <summary>
        /// Redirige las rutas antiguas de archivo a su URL limpia, conservando la query.
        /// </summary>
        public static IApplicationBuilder UseLegacyPageRedirects(this IApplicationBuilder app)
        {
            // 302 y no 301: el navegador guarda un 301 para siempre, y si la tabla
            // cambia quedaría redirigiendo a una ruta vieja sin forma de corregirlo.
            var options = new RewriteOptions()
                .AddRedirect(@"^index\.html$", "/", StatusCodes.Status302Found);

            foreach (var (cleanPath, file) in Pages)
                options.AddRedirect($"^{Regex.Escape(file)}$", cleanPath, StatusCodes.Status302Found);

            // Mira la ruta tal como llega, antes de UseDefaultFiles y de los endpoints
            // que la cambian por el archivo: por eso no puede entrar en bucle. No
            // moverlo detrás de UseDefaultFiles ("/" → "/index.html" → "/" …).
            return app.UseRewriter(options);
        }

        /// <summary>
        /// Sirve cada URL limpia con su archivo, sin redirigir: la barra conserva la URL limpia.
        /// </summary>
        public static IEndpointRouteBuilder MapCleanPageRoutes(this IEndpointRouteBuilder endpoints)
        {
            foreach (var (cleanPath, file) in Pages)
                endpoints.MapFallbackToFile(cleanPath, file);

            return endpoints;
        }
    }
}
