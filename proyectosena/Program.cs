using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using proyectosena;
using proyectosena.Context;
using proyectosena.Middleware;
using proyectosena.Interfaces.Repositories;
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
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("ManagerOnly", policy => policy.RequireRole("Manager"));
    options.AddPolicy("CitizenOnly", policy => policy.RequireRole("Citizen"));
    options.AddPolicy("AdminOrManager", policy => policy.RequireRole("Administrator", "Manager"));
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
// Pendiente por crear (BE-13): IAuthService, IUserService, ICollectionRequestService,
// INotificationService, IChatHistoryService.


// ── 8. GLOBAL ERROR HANDLING ──────────────────────────
// Every unhandled exception is logged here and returned as ProblemDetails,
// so internal detail never reaches the client.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();

// ── BUILD ─────────────────────────────────────────────
var app = builder.Build();

// ── APLICA LAS MIGRACIONES DE EF CORE AL INICIAR ──────
// Necesario en Docker: crea la base de datos y las tablas
// si aún no existen, o aplica las migraciones pendientes.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RecyRouteDbContext>();
    dbContext.Database.Migrate();
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
app.UseAuthentication();
app.UseAuthorization();

// ── 401 CUSTOM MIDDLEWARE ─────────────────────────────
//app.Use(async (context, next) =>
//{
//    await next();
//    if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
//    {
//        //context.Response.ContentType = "application/json";
//        var result = System.Text.Json.JsonSerializer.Serialize(new
//        {
//            mensaje = "Acceso no autorizado. Verifique su token o credenciales."
//        });
//        await context.Response.WriteAsync(result);
//    }
//});

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.Run();