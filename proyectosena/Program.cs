using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.OpenApi;
using proyectosena;
using proyectosena.Extensions;
using proyectosena.Interfaces.Services;
using proyectosena.Middleware;
using proyectosena.Models;
using proyectosena.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── 1. LOGGING ────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ── 2. DATABASE + REPOSITORIES ────────────────────────
builder.Services.AddProjectDependencies(builder.Configuration);

// ── 2b. PUERTO AL QUE REDIRIGE EL HTTPS ───────────────
// El middleware de redirección necesita saber a qué puerto mandar. Dentro del
// contenedor no hay ningún listener HTTPS —el TLS lo termina el proxy del
// proveedor—, así que no lo puede deducir solo: se limita a dejar pasar la
// petición y a escribir "Failed to determine the https port for redirect" en el
// log. Comprobado: con el conmutador encendido y sin este puerto, una petición
// HTTP recibía 200 igual. El conmutador parecía encendido sin estarlo.
builder.Services.AddHttpsRedirection(options =>
    options.HttpsPort = builder.Configuration.GetValue("Https:Port", 443));

// ── 3. JWT AUTHENTICATION ─────────────────────────────
//
// La clave de firma ya no tiene valor por defecto: appsettings.json se versiona
// y lo que se escriba ahí queda publicado (B-1c). Si no llega por el entorno, la
// API no arranca. Arrancar sin ella era peor que no arrancar: firmaría con una
// clave que cualquiera puede leer en el repositorio, y con esa clave se fabrica
// un token de administrador sin tocar la base.
//
// El mínimo de 32 bytes no es un gusto: HS256 usa SHA-256 y la propia librería
// rechaza claves más cortas al firmar. Fallar aquí lo dice en el arranque, en
// vez de en el primer inicio de sesión.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Falta la clave de firma 'Jwt:Key', o tiene menos de 32 bytes. Defínela como " +
        "variable de entorno Jwt__Key: en local, en el .env que lee docker-compose.yml; " +
        "al desplegar, en el panel del proveedor. Los nombres están en .env.example.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };

        // La firma y la fecha no bastan: un token de alguien que ya cerró sesión
        // sigue estando bien firmado y sin caducar. Aquí se comprueba, además,
        // que no esté en la lista de anulados.
        //
        // Va en OnTokenValidated y no en un middleware aparte para que ningún
        // endpoint pueda saltárselo: todo lo que exige [Authorize] pasa por aquí.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var revocados = context.HttpContext.RequestServices
                    .GetRequiredService<IRevokedTokenService>();

                var tokenId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (await revocados.IsRevoked(tokenId ?? string.Empty))
                {
                    context.Fail("El token fue anulado al cerrar sesión.");
                    return;
                }

                // Segunda comprobación, para lo que un jti suelto no cubre: que
                // a esta persona la hayan dado de baja o le hayan cambiado la
                // contraseña desde OTRA sesión, que no conoce el jti de esta.
                //
                // Un token de antes de este cambio no trae "iat" y no hay con
                // qué compararlo: se deja pasar, igual que ya se hace en
                // Logout con el jti que falta. Caducará solo, como mucho, en
                // el tiempo de vida configurado.
                var idUserRaw = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var iatRaw = context.Principal?.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;

                if (Guid.TryParse(idUserRaw, out var idUser) && long.TryParse(iatRaw, out var iatSegundos))
                {
                    var emitidoEn = DateTimeOffset.FromUnixTimeSeconds(iatSegundos).UtcDateTime;

                    if (await revocados.IsRevokedForUser(idUser, emitidoEn))
                        context.Fail("El token fue anulado: la cuenta cambió después de emitirse.");
                }
            }
        };
    });

// ── 4. ROLE-BASED AUTHORIZATION ───────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(RoleNames.Administrator));
    options.AddPolicy("ManagerOnly", policy => policy.RequireRole(RoleNames.Manager));
    options.AddPolicy("CitizenOnly", policy => policy.RequireRole(RoleNames.Citizen));
    options.AddPolicy("AdminOrManager", policy => policy.RequireRole(RoleNames.Administrator, RoleNames.Manager));
});

// ── 5. CORS ───────────────────────────────────────────
// En producción la propia API sirve el frontend (mismo origen, ver
// api.js), así que CORS de verdad solo hace falta para el Docker local:
// ahí Nginx sirve las páginas en el 8081 y la API escucha en el 8080,
// dos orígenes distintos. Antes esto aceptaba cualquier origen
// (I-1 · WA-02): cualquier sitio web podía llamar a la API desde el
// navegador de quien tuviera un token.
//
// El origen sale de la configuración y ya no del código (N-4): el día que el
// frontend viva en su propio dominio, dejarlo entrar es una variable de entorno
// y no una recompilación. Varios se separan con comas. El valor por defecto es
// el del Docker local, que es el único montaje donde hoy hace falta.
var origenesPermitidos = (builder.Configuration["Cors:Origins"] ?? "http://localhost:8081")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("RecyRoutePolicy", policy =>
    {
        policy.WithOrigins(origenesPermitidos)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ── 5b. LIMITE DE PETICIONES ──────────────────────────
// Los endpoints anónimos son los únicos que se pueden golpear sin credencial,
// y son justo los que dan acceso. Medido sobre este proyecto: 37 intentos por
// segundo contra un código, sin que nada los frenara.
//
// Son tres capas, y cada una tapa lo que la anterior deja pasar:
//
//   1. El contador de intentos del código (5 por código, en
//      VerificationCodeService). Es la única que no se salta: no mira de dónde
//      viene el golpe.
//   2. El cupo por destinatario, más abajo: protege el buzón de cada persona.
//   3. El tope general de envíos: impide pedir códigos para mil direcciones
//      distintas, que con la capa 2 sola estrenarían sus cinco cada una.
//
// La política Auth reparte por IP. En Docker local todos los clientes llegan
// con la misma dirección, así que son diez por minuto para toda la instalación
// y dos personas probando a la vez se estorban. En Render, con
// ForwardedHeaders__Enabled=true, cada persona tiene su propio cupo.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Diez por minuto: una persona real hace uno o dos, y esto frena el ataque
    // medido más de doscientas veces.
    options.AddPolicy(RateLimitPolicies.Auth, http =>
        RateLimitPartition.GetFixedWindowLimiter(ClientKey(http), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1)
        }));

    // Más estricto porque cada llamada manda un correo a una dirección que elige
    // quien llama: sin freno se inunda el buzón de otra persona.
    //
    // Reparte por DESTINATARIO, no por IP: lo que se protege es el buzón de cada
    // persona, y así dos personas distintas dejan de estorbarse.
    options.AddPolicy(RateLimitPolicies.Email, http =>
        RateLimitPartition.GetFixedWindowLimiter(ClaveDeDestinatario(http), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(5)
        }));

    // ── Tope general de envíos ────────────────────────────────────────
    //
    // El cupo por destinatario protege el buzón de cada persona, pero no impide
    // pedir códigos para mil direcciones distintas: cada una estrenaría sus cinco.
    // Este tope cuenta TODOS los envíos juntos, sin mirar a quién van.
    //
    // Alcanza solo a los tres endpoints que mandan correo. El resto de la
    // aplicación pasa sin tope, para no frenar el uso normal.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
        EsEndpointDeCorreo(http)
            ? RateLimitPartition.GetFixedWindowLimiter("correo-total", _ => new FixedWindowRateLimiterOptions
            {
                // Holgado a propósito: una sustentación con varios equipos no debe
                // chocar con esto, pero un envío masivo sí.
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(5)
            })
            : RateLimitPartition.GetNoLimiter<string>("sin-tope"));

    // Texto plano, como el resto de errores de negocio del proyecto
    options.OnRejected = async (context, token) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();

        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "Demasiados intentos. Espera un momento y vuelve a intentarlo.", token);
    };
});

// Quién es «el que llama», por dirección IP.
//
// Ojo: en Docker local TODOS los clientes llegan con la misma IP. Se comprobó
// gastando el cupo desde la terminal y viendo al navegador recibir 429 sin haber
// pedido nada. Ahí esto no separa personas: sirve solo como tope general. En
// Render, con ForwardedHeaders__Enabled=true, es la IP real de cada persona.
static string ClientKey(HttpContext http)
    => http.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

// A quién se le va a mandar el correo. Lo deja ClientEmailMiddleware leyendo el
// cuerpo. Si no viniera, se cae a la IP para no quedarse sin ningún freno.
static string ClaveDeDestinatario(HttpContext http)
    => http.Items[ClientEmailMiddleware.ItemKey] as string ?? ClientKey(http);

// Los tres endpoints que mandan un correo a una dirección que elige quien
// llama. Son los únicos con tope general: los demás no envían nada.
static bool EsEndpointDeCorreo(HttpContext http)
    => http.Request.Path.StartsWithSegments("/api/auth/forgot-password", StringComparison.OrdinalIgnoreCase)
    || http.Request.Path.StartsWithSegments("/api/auth/resend-verification", StringComparison.OrdinalIgnoreCase)
    || http.Request.Path.StartsWithSegments("/api/auth/Register", StringComparison.OrdinalIgnoreCase);

// ── 6. SWAGGER ────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RecyRoute",
        Version = "v1",
        Description = "Proyecto para la gestión de solicitudes de recolección de residuos"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.<br/><br/>
                        Escribe: Bearer [space] y luego tu token.<br/><br/>
                        Ejemplo: 'Bearer abc123xyz'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(doc =>new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc),
            new List<string>()
        }
    });
});

// ── 7. SERVICES ───────────────────────────────────────
// Todos los registros viven en DependencyInjection.cs (paso 2), en un solo lugar.

// ── 8. GLOBAL ERROR HANDLING ──────────────────────────
// Every unhandled exception is logged here and returned as ProblemDetails,
// so internal detail never reaches the client.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();

// ── BUILD ─────────────────────────────────────────────
var app = builder.Build();

// ── MIGRACIONES DE EF CORE ────────────────────────────
// Se aplican al arrancar, con la siembra del administrador inicial (BL-03)
// dentro. Varias réplicas arrancando a la vez no chocan: ApplyMigrationsAsync
// migra dentro de un candado de PostgreSQL (B-7). Ponerlo en false apaga
// también la siembra, y la imagen final no trae `dotnet ef`: no se apaga sin
// dejar antes otro camino para migrar.
if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    await app.ApplyMigrationsAsync();
}

// ── 8. MIDDLEWARE PIPELINE ────────────────────────────

// Debe ir primero: reescribe el esquema (http/https) y la IP remota ANTES de
// que nada más los lea —el límite de peticiones por IP, CORS, la redirección
// a HTTPS—. Sin esto, detrás del proxy de Render todas las peticiones llegan
// con la IP interna del proxy, y el cupo por IP termina siendo compartido por
// todo el mundo (BL-09).
//
// Apagado por defecto. Con KnownNetworks/KnownProxies vacíos se le cree a
// X-Forwarded-For venga de quien venga, y eso solo es seguro cuando la única
// puerta de entrada es un proxy que escribe esa cabecera él mismo. Sin proxy
// delante, cualquiera cambiaba la cabecera en cada intento y nunca le llegaba
// el 429 del login. Se enciende solo en Render: ForwardedHeaders__Enabled=true.
if (app.Configuration.GetValue("ForwardedHeaders:Enabled", false))
{
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwardedHeadersOptions.KnownNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedHeadersOptions);
}

// Swagger solo en desarrollo: en producción publica el mapa entero de la API,
// con los nombres y el cuerpo que espera cada endpoint.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RecyRoute");
        options.RoutePrefix = "swagger";
    });
}

// ── HTTPS ─────────────────────────────────────────────
// Va justo después de UseForwardedHeaders y no antes: detrás de un proxy el
// esquema de verdad viaja en X-Forwarded-Proto, y sin haberlo leído esta
// redirección vería "http" en TODAS las peticiones —incluidas las que ya
// llegaron cifradas— y mandaría al navegador a un bucle de redirecciones.
//
// Apagado por defecto porque el docker-compose corre HTTP puro en el 8080:
// encenderlo ahí dejaría la API inalcanzable en local. Se enciende donde hay
// TLS delante (Https__Enforce=true en Render), y ahí importa aunque el proxy
// ya sirva HTTPS: sin esto, una petición que llegue por HTTP se atiende igual,
// con su token viajando en claro.
var forzarHttps = app.Configuration.GetValue("Https:Enforce", false);

// HSTS solo con el conmutador encendido, nunca por estar en desarrollo: el
// navegador se queda recordando la orden por meses y la aplica a TODO lo que
// se sirva en localhost, incluidos otros proyectos del mismo equipo.
if (forzarHttps)
{
    app.UseHsts();
}

if (forzarHttps || app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Must sit before everything it protects, so any exception thrown
// further down the pipeline lands in GlobalExceptionHandler.
app.UseExceptionHandler();

// Las rutas antiguas (/pages/auth/login.html) redirigen a la limpia (/login)
// antes de que nada más las toque. Tabla en Extensions/PageRouteExtensions.cs.
app.UseLegacyPageRedirects();

app.UseCors("RecyRoutePolicy");

// Antes del limitador: deja el correo del cuerpo en HttpContext.Items para que
// el cupo se pueda repartir por persona y no por IP —que detrás de Docker es la
// misma para todos—. Rebobina el cuerpo, así los controladores lo leen intacto.
app.UseMiddleware<ClientEmailMiddleware>();

// Después de UseRouting (que WebApplication añade solo) para que conozca el
// endpoint y sepa qué política aplicarle. Antes de autenticar: frenar es más
// barato que validar un token.
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
// /login, /chat, … sirven su HTML sin cambiar la URL. Van como fallback: ceden
// ante cualquier endpoint de la API.
app.MapCleanPageRoutes();
app.Run();

// Las pruebas (proyectosena.Tests) arrancan la aplicación con
// WebApplicationFactory<Program>, que necesita ver esta clase. Con top-level
// statements el compilador la genera internal; esto solo la hace pública.
public partial class Program { }