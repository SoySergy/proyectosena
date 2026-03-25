using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using proyectosena;
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

// ── 7. SERVICES (uncomment when created) ─────────────
// builder.Services.AddScoped<IAuthService, AuthService>();
// builder.Services.AddScoped<IUserService, UserService>();
// builder.Services.AddScoped<ICollectionRequestService, CollectionRequestService>();
// builder.Services.AddScoped<ICollectionManagementService, CollectionManagementService>();
// builder.Services.AddScoped<INotificationService, NotificationService>();
// builder.Services.AddScoped<IChatHistoryService, ChatHistoryService>();

builder.Services.AddControllers();

// ── BUILD ─────────────────────────────────────────────
var app = builder.Build();

// ── 8. MIDDLEWARE PIPELINE ────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RecyRoute");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
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