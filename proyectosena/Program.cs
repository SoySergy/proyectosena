using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── 1. LOGGING ────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ── 2. DATABASE + REPOSITORIES ────────────────────────
builder.Services.AddProjectDependencies(builder.Configuration);

// ── 3. JWT AUTHENTICATION ─────────────────────────────
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
            IssuerSigningKey = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(
                                               builder.Configuration["Jwt:Key"]!)),
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
            OnTokenValidated = context =>
            {
                var revocados = context.HttpContext.RequestServices
                    .GetRequiredService<IRevokedTokenService>();

                var tokenId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (revocados.IsRevoked(tokenId ?? string.Empty))
                    context.Fail("El token fue anulado al cerrar sesión.");

                return Task.CompletedTask;
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
builder.Services.AddCors(options =>
{
    options.AddPolicy("RecyRoutePolicy", policy =>
    {
        policy.AllowAnyOrigin()
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
// La política Auth sigue repartiendo por IP, y eso es un problema conocido:
// detrás de Docker todos los clientes llegan con la misma dirección, así que
// son diez por minuto para toda la instalación y dos personas probando a la
// vez se estorban. Pendiente de decidir (AL-02).
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
    // Alcanza solo a los dos endpoints que mandan correo. El resto de la
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
// Ojo: detrás de Docker TODOS los clientes llegan con la misma IP. Se comprobó
// gastando el cupo desde la terminal y viendo al navegador recibir 429 sin haber
// pedido nada. Por eso esto ya no vale para separar personas: sirve solo como
// tope general.
static string ClientKey(HttpContext http)
    => http.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

// A quién se le va a mandar el correo. Lo deja ClientEmailMiddleware leyendo el
// cuerpo. Si no viniera, se cae a la IP para no quedarse sin ningún freno.
static string ClaveDeDestinatario(HttpContext http)
    => http.Items[ClientEmailMiddleware.ItemKey] as string ?? ClientKey(http);

// Los dos endpoints que mandan un correo a una dirección que elige quien llama.
// Son los únicos con tope general: los demás no envían nada.
static bool EsEndpointDeCorreo(HttpContext http)
    => http.Request.Path.StartsWithSegments("/api/auth/forgot-password", StringComparison.OrdinalIgnoreCase)
    || http.Request.Path.StartsWithSegments("/api/auth/resend-verification", StringComparison.OrdinalIgnoreCase);

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
// Aplicarlas al arrancar es cómodo en desarrollo y en Docker. En producción,
// con varias réplicas levantándose a la vez, hay carrera sobre el esquema: por
// eso está detrás de un interruptor. Se pone en false y las migraciones pasan a
// ser un paso explícito del despliegue (dotnet ef database update).
if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    await app.ApplyMigrationsAsync();
}

// ── 8. MIDDLEWARE PIPELINE ────────────────────────────
// Swagger disponible en /swagger en cualquier entorno (incluido Docker/Production)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RecyRoute");
        options.RoutePrefix = "swagger";
    });
}

// Solo redirige a HTTPS si NO estamos en Producción (Docker corre solo HTTP)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Must sit before everything it protects, so any exception thrown
// further down the pipeline lands in GlobalExceptionHandler.
app.UseExceptionHandler();

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
app.Run();