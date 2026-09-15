using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using proyectosena.Context;
using proyectosena.Interfaces.Services;
using Testcontainers.PostgreSql;

namespace proyectosena.Tests.Infrastructure
{
    /// <summary>
    /// La API completa, arrancada en memoria contra una PostgreSQL desechable.
    /// </summary>
    /// <remarks>
    /// El contenedor se crea al empezar la tanda de pruebas y Testcontainers lo
    /// borra al terminar: la base real (recyroute_db) no se toca nunca. No se usa
    /// una base en memoria porque el código depende de cosas que solo tiene
    /// PostgreSQL —ON CONFLICT, FOR UPDATE, candados, el índice sobre LOWER—, y
    /// una base falsa daría verde donde la real falla.
    /// </remarks>
    public sealed class RecyRouteApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        // Cabecera con la que cada cliente de prueba declara su propia IP (ver ClientIpFromHeader)
        public const string ClientIpHeader = "X-Test-Client-Ip";

        // Con qué firma y valida sus tokens la API de pruebas. Son públicos para
        // que ForgedTokenTests pueda fabricar tokens buenos y malos a propósito.
        public const string JwtKey = "recyroute-test-signing-key-with-no-real-value-0123456789";
        public const string JwtIssuer = "recyroute-tests";
        public const string JwtAudience = "recyroute-tests";

        // La misma imagen que docker-compose.yml: las pruebas tienen que fallar
        // donde fallaría la base de verdad.
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();

        // Sustituye al EmailService real: guarda los códigos en vez de mandarlos
        public FakeEmailService Emails { get; } = new();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            // Variables de entorno y no UseSetting: DependencyInjection.cs lee la
            // cadena de conexión en cuanto arranca Program.cs, y las variables de
            // entorno ya están cargadas en ese momento. La clave JWT también se
            // fija aquí para no depender del secreto de appsettings.json.
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _postgres.GetConnectionString());
            Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
            Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
            Environment.SetEnvironmentVariable("Jwt__Audience", JwtAudience);

            // Segundo cinturón: si la API hubiera tomado otra base, la tanda se
            // detiene aquí, antes de la primera prueba.
            using var scope = Services.CreateScope();
            var connectionInUse = scope.ServiceProvider.GetRequiredService<RecyRouteDbContext>().Database.GetConnectionString();

            if (connectionInUse != _postgres.GetConnectionString())
                throw new InvalidOperationException("The test API is not using the disposable database.");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Ni Swagger ni la redirección a HTTPS, que Program.cs solo enciende en Development
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailService>();
                services.AddSingleton<IEmailService>(Emails);
                services.AddSingleton<IStartupFilter, ClientIpFromHeader>();
            });
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await base.DisposeAsync();
            await _postgres.DisposeAsync();
        }

        /// <summary>
        /// Le pone a cada petición la IP que diga su cabecera, antes que nada más.
        /// </summary>
        /// <remarks>
        /// Program.cs reparte el cupo del login por IP (10 por minuto), y en memoria
        /// todas las peticiones llegan sin IP: serían diez intentos para la tanda
        /// entera. Así cada prueba tiene su propio cupo, y además se puede probar
        /// el límite mismo.
        /// </remarks>
        private sealed class ClientIpFromHeader : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
            {
                app.Use(async (context, nextMiddleware) =>
                {
                    if (IPAddress.TryParse(context.Request.Headers[ClientIpHeader].ToString(), out var ip))
                        context.Connection.RemoteIpAddress = ip;

                    await nextMiddleware();
                });

                next(app);
            };
        }
    }
}
