using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using proyectosena.Context;

namespace proyectosena.Tests.Infrastructure
{
    public static class Concurrency
    {
        /// <summary>
        /// Corre la misma operación varias veces exactamente a la vez, cada una con
        /// su scope —como una petición HTTP— y le pasa su número de orden.
        /// </summary>
        /// <remarks>
        /// Todas abren antes su conexión y esperan la misma señal. Lanzarlas sin más
        /// no basta: abrir la conexión mete retrasos distintos en cada una, las
        /// operaciones apenas se solapan y una carrera real pasa sin detectarse. Se
        /// comprobó rompiendo a propósito el contador de fallos de los códigos:
        /// veinte intentos sueltos pasaban en verde; cuatro con señal fallaban
        /// siempre.
        /// </remarks>
        public static async Task<T[]> RunSimultaneouslyAsync<TService, T>(
            this RecyRouteApiFactory api, int times, Func<TService, int, Task<T>> operation)
            where TService : notnull
        {
            var scopes = Enumerable.Range(0, times).Select(_ => api.Services.CreateScope()).ToList();
            try
            {
                foreach (var scope in scopes)
                    await scope.ServiceProvider.GetRequiredService<RecyRouteDbContext>().Database.OpenConnectionAsync();

                var go = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var tasks = scopes.Select(async (scope, index) =>
                {
                    await go.Task;
                    return await operation(scope.ServiceProvider.GetRequiredService<TService>(), index);
                }).ToList();

                go.SetResult();
                return await Task.WhenAll(tasks);
            }
            finally
            {
                scopes.ForEach(scope => scope.Dispose());
            }
        }
    }
}
