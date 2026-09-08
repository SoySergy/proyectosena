using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using proyectosena.Context;

namespace proyectosena.Extensions
{
    /// <summary>
    /// Aplica las migraciones pendientes al arrancar, esperando a que la base
    /// de datos acepte conexiones.
    /// </summary>
    /// <remarks>
    /// Antes esto era un <c>Migrate()</c> pelado en <c>Program.cs</c>. Si SQL
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

                    logger.LogInformation(
                        "Migraciones aplicadas correctamente (intento {Intento} de {Maximo}).",
                        attempt, MaxAttempts);
                    return;
                }
                // La base todavía no está lista: se espera y se vuelve a intentar
                catch (SqlException ex) when (attempt < MaxAttempts)
                {
                    logger.LogWarning(
                        "La base de datos aún no acepta conexiones (intento {Intento} de {Maximo}). " +
                        "Se reintenta en {Segundos} s. Motivo: {Motivo}",
                        attempt, MaxAttempts, DelayBetweenAttempts.TotalSeconds, ex.Message);

                    await Task.Delay(DelayBetweenAttempts);
                }
                // Se agotó la espera: sin esquema no tiene sentido atender peticiones
                catch (SqlException ex)
                {
                    logger.LogCritical(ex,
                        "No se pudo conectar a la base de datos tras {Maximo} intentos ({Segundos} s en total). " +
                        "La aplicación no puede arrancar sin esquema.",
                        MaxAttempts, MaxAttempts * DelayBetweenAttempts.TotalSeconds);
                    throw;
                }
            }
        }
    }
}
