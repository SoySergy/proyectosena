using Npgsql;
using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Models;
using System.Security.Cryptography;

namespace proyectosena.Extensions
{
    /// <summary>
    /// Aplica las migraciones pendientes al arrancar, esperando a que la base
    /// de datos acepte conexiones.
    /// </summary>
    /// <remarks>
    /// Antes esto era un <c>Migrate()</c> pelado en <c>Program.cs</c>. Si PostgreSQL
    /// Server todavía no estaba escuchando, la aplicación reventaba con una
    /// excepción sin controlar y Docker la reiniciaba una y otra vez hasta que
    /// la base respondiera. El <c>depends_on: service_healthy</c> del compose no
    /// lo evita: solo ordena el arranque de <c>docker compose up</c>, no los
    /// reinicios automáticos de <c>restart: unless-stopped</c>.
    ///
    /// <para>
    /// Se reintenta solo ante fallos de conexión. Un error real de migración
    /// —SQL mal formado, una columna que ya existe— no se reintenta: sube de
    /// inmediato, porque esperar no lo va a arreglar.
    /// </para>
    /// </remarks>
    public static class MigrationExtensions
    {
        private const int MaxAttempts = 12;
        private static readonly TimeSpan DelayBetweenAttempts = TimeSpan.FromSeconds(5);

        public static async Task ApplyMigrationsAsync(this WebApplication app)
        {
            var logger = app.Services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("RecyRoute.Migraciones");

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    using var scope = app.Services.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<RecyRouteDbContext>();

                    await dbContext.Database.MigrateAsync();

                    await SeedAdminUserAsync(dbContext, app.Configuration, logger);

                    logger.LogInformation(
                        "Migraciones aplicadas correctamente (intento {Intento} de {Maximo}).",
                        attempt, MaxAttempts);
                    return;
                }
                // La base todavía no está lista: se espera y se vuelve a intentar
                catch (NpgsqlException ex) when (attempt < MaxAttempts)
                {
                    logger.LogWarning(
                        "La base de datos aún no acepta conexiones (intento {Intento} de {Maximo}). " +
                        "Se reintenta en {Segundos} s. Motivo: {Motivo}",
                        attempt, MaxAttempts, DelayBetweenAttempts.TotalSeconds, ex.Message);

                    await Task.Delay(DelayBetweenAttempts);
                }
                // Se agotó la espera: sin esquema no tiene sentido atender peticiones
                catch (NpgsqlException ex)
                {
                    logger.LogCritical(ex,
                        "No se pudo conectar a la base de datos tras {Maximo} intentos ({Segundos} s en total). " +
                        "La aplicación no puede arrancar sin esquema.",
                        MaxAttempts, MaxAttempts * DelayBetweenAttempts.TotalSeconds);
                    throw;
                }
            }
        }

        private static async Task SeedAdminUserAsync(RecyRouteDbContext dbContext, IConfiguration configuration, ILogger logger)
        {
            var adminExists = await dbContext.Users.AnyAsync(u => u.IdRole == SeedIds.Roles.Administrator);
            if (adminExists)
            {
                logger.LogInformation("Ya existe un usuario administrador en la base de datos.");
                return;
            }

            // Estos cuatro no son secretos: quién es el administrador se puede
            // saber sin que eso abra ninguna puerta.
            var adminEmail = Ajuste(configuration, "Admin:Email") ?? "admin@recyroute.com";
            var adminDoc = Ajuste(configuration, "Admin:DocumentNumber") ?? "1000000000";
            var adminName = Ajuste(configuration, "Admin:Name") ?? "Administrador";
            var adminLastName = Ajuste(configuration, "Admin:LastName") ?? "RecyRoute";

            // La contraseña es lo único que NO puede tener un valor por defecto.
            //
            // Antes aquí decía "Admin123*". Como nada define Admin:Password —ni el
            // compose, ni appsettings, ni .env.example—, ese valor era el real: toda
            // instalación nueva nacía con una cuenta de administrador cuya clave
            // estaba escrita en el código fuente. Se comprobó levantando una pila
            // limpia y entrando con ella.
            //
            // Si no está configurada se genera una al azar y se escribe una sola vez
            // en el log. Sigue habiendo administrador en una base vacía —que era el
            // problema que este método vino a resolver— pero ya no hay ninguna clave
            // que se pueda adivinar leyendo el repositorio.
            var adminPassword = Ajuste(configuration, "Admin:Password");
            var contrasenaGenerada = adminPassword is null;
            adminPassword ??= GenerarContrasenaInicial();

            var adminUser = new User
            {
                IdUser = Guid.NewGuid(),
                IdRole = SeedIds.Roles.Administrator,
                IdDocumentType = SeedIds.DocumentTypes.CedulaCiudadania,
                DocumentNumber = adminDoc,
                Name = adminName,
                LastName = adminLastName,
                Email = adminEmail.Trim().ToLowerInvariant(),
                Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                PhoneNumber = "3000000000",
                Address = "Sede Central",
                IsActive = true,
                IsEmailVerified = true,
                RegistrationDate = DateTime.UtcNow
            };

            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync();

            if (!contrasenaGenerada)
            {
                logger.LogInformation(
                    "Usuario Administrador inicial sembrado exitosamente ({Email}) " +
                    "con la contraseña de Admin:Password.",
                    adminEmail);
                return;
            }

            // Critical y no Information a propósito: si esto se pierde entre el
            // ruido del arranque, nadie puede entrar y la cuenta ya está creada,
            // así que el método no volverá a pasar por aquí.
            logger.LogCritical(
                "{Separador}" +
                "ADMINISTRADOR INICIAL CREADO — APUNTA ESTA CONTRASEÑA AHORA{Salto}" +
                "  Correo:      {Email}{Salto}" +
                "  Contraseña:  {Contrasena}{Salto}" +
                "Se generó al azar porque Admin:Password no está configurada. " +
                "No se vuelve a mostrar: cámbiala al entrar. Para fijarla tú, define " +
                "ADMIN_PASSWORD antes del primer arranque contra una base vacía." +
                "{Separador}",
                Separador, Environment.NewLine, adminEmail, Environment.NewLine,
                adminPassword, Environment.NewLine, Separador);
        }

        private const string Separador = "\n══════════════════════════════════════════════════════════════\n";

        /// <summary>
        /// Lee un ajuste tratando el vacío como ausente.
        /// </summary>
        /// <remarks>
        /// <c>docker compose</c> sustituye por cadena vacía una variable que no
        /// existe, y <c>""</c> no es <c>null</c>: con el <c>??</c> pelado, declarar
        /// <c>Admin__Password: "${ADMIN_PASSWORD:-}"</c> sin ponerla en el .env
        /// crearía el administrador con la contraseña vacía en vez de generarla.
        /// </remarks>
        private static string? Ajuste(IConfiguration configuration, string clave)
        {
            var valor = configuration[clave];
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        /// <summary>
        /// Contraseña de un solo uso para el primer administrador.
        /// </summary>
        /// <remarks>
        /// Generador criptográfico, igual que los códigos de verificación:
        /// <c>Random</c> no promete ser impredecible. El alfabeto deja fuera los
        /// caracteres que se confunden al copiarlos de una consola —O con 0, l
        /// con 1 y con I— porque esto se lee de un log y se teclea a mano.
        /// </remarks>
        private static string GenerarContrasenaInicial()
        {
            const string alfabeto =
                "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789-_#@%+=";

            var caracteres = new char[24];
            for (var i = 0; i < caracteres.Length; i++)
                caracteres[i] = alfabeto[RandomNumberGenerator.GetInt32(alfabeto.Length)];

            return new string(caracteres);
        }
    }
}
