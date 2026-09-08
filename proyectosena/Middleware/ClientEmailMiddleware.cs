using System.Text.Json;

namespace proyectosena.Middleware
{
    /// <summary>
    /// Deja en <c>HttpContext.Items</c> el correo que viene en el cuerpo de la
    /// petición, para que el límite de peticiones pueda repartir el cupo por
    /// persona en vez de por dirección IP.
    /// </summary>
    /// <remarks>
    /// El límite repartía por IP, pero detrás de Docker todos los clientes llegan
    /// con la misma dirección: eran cinco intentos <em>para todo el mundo junto</em>,
    /// no cinco por persona. Se comprobó gastando el cupo desde la terminal y viendo
    /// al navegador recibir 429 sin haber pedido nada.
    ///
    /// <para>
    /// El cuerpo se rebobina con <c>EnableBuffering</c>, así el controlador lo vuelve
    /// a leer intacto. Solo se mira en peticiones JSON pequeñas: un cuerpo grande no
    /// es un formulario de acceso y no vale la pena bufferizarlo.
    /// </para>
    /// </remarks>
    public class ClientEmailMiddleware
    {
        /// <summary>Clave con la que el correo queda en <c>HttpContext.Items</c>.</summary>
        public const string ItemKey = "ClientEmail";

        // Un cuerpo de acceso son unos cientos de bytes. Más que esto no se lee.
        private const int MaxBodyBytes = 4 * 1024;

        // Los únicos endpoints con freno por persona cuelgan de aquí
        private static readonly PathString RutaAutenticacion = new("/api/auth");

        private readonly RequestDelegate _next;

        public ClientEmailMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correo = await LeerCorreo(context.Request);

            if (correo != null)
                context.Items[ItemKey] = correo;

            await _next(context);
        }

        private static async Task<string?> LeerCorreo(HttpRequest request)
        {
            // Solo las rutas de autenticación llevan el freno por persona. Sin este
            // filtro se bufferiaba el cuerpo de los catorce POST del sistema para
            // servir a siete.
            if (!request.Path.StartsWithSegments(RutaAutenticacion, StringComparison.OrdinalIgnoreCase))
                return null;

            if (!HttpMethods.IsPost(request.Method))
                return null;

            if (request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) != true)
                return null;

            if (request.ContentLength is null or 0 or > MaxBodyBytes)
                return null;

            // Permite releer el cuerpo: sin esto, el controlador lo encontraría vacío
            request.EnableBuffering();

            try
            {
                using var documento = await JsonDocument.ParseAsync(request.Body);

                if (documento.RootElement.ValueKind != JsonValueKind.Object)
                    return null;

                // Se recorre entero y se guarda la ÚLTIMA coincidencia, sin distinguir
                // mayúsculas: es exactamente lo que hace el enlazador de ASP.NET.
                //
                // Elegir otra cosa abría un agujero que anulaba este freno por completo:
                // con {"email":"basura-1@x.com","email":"victima@gmail.com"} el limitador
                // contaba el cupo de «basura-1» —clave nueva en cada petición, siempre
                // con sus cinco libres— mientras el correo se le mandaba a la víctima.
                // Y con «Email» en mayúscula la extracción no veía nada y caía a la IP.
                string? ultimo = null;
                var encontrado = false;

                foreach (var propiedad in documento.RootElement.EnumerateObject())
                {
                    if (!string.Equals(propiedad.Name, "email", StringComparison.OrdinalIgnoreCase))
                        continue;

                    encontrado = true;

                    // Si el último valor no es texto, el enlazador tampoco lo aceptará:
                    // se deja en null y la petición acabará rechazada más adelante.
                    ultimo = propiedad.Value.ValueKind == JsonValueKind.String
                        ? propiedad.Value.GetString()
                        : null;
                }

                if (!encontrado)
                    return null;

                var valor = ultimo?.Trim().ToLowerInvariant();

                // Sin correo utilizable se cae a la IP, que es la bolsa más estricta:
                // esquivar la extracción no le sirve de nada a quien lo intente.
                return string.IsNullOrEmpty(valor) ? null : valor;
            }
            catch (JsonException)
            {
                // Cuerpo mal formado: no es asunto de este middleware. El modelo
                // de ASP.NET lo rechazará más adelante con su 400 de siempre.
                return null;
            }
            finally
            {
                // Se deja como estaba para quien venga detrás
                request.Body.Position = 0;
            }
        }
    }
}
