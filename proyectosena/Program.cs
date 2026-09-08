using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.OpenApi;
using proyectosena;
using proyectosena.Extensions;
using proyectosena.Middleware;
using proyectosena.Models;
using proyectosena.Services;
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
// El freno va por IP, así que es la segunda línea y no la primera: quien tenga
// muchas IP se lo salta. La defensa que no se salta es el contador de intentos
// del código, que no mira de dónde viene el golpe.
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
    options.AddPolicy(RateLimitPolicies.Email, http =>
        RateLimitPartition.GetFixedWindowLimiter(ClientKey(http), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(5)
        }));

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

// Quién es «el que llama». Detrás de un proxy la IP sería la del proxy, no la
// del navegador: hoy nginx solo sirve archivos y no hace de intermediario.
static string ClientKey(HttpContext http)
    => http.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

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