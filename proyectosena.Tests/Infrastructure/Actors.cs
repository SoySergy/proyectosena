using Microsoft.Extensions.DependencyInjection;
using proyectosena.Context;
using proyectosena.Models;

namespace proyectosena.Tests.Infrastructure
{
    /// <summary>
    /// Alguien con la sesión abierta: su cliente (con su propia IP), su id y su token.
    /// </summary>
    public sealed record Actor(HttpClient Client, Guid Id, string Email, string Token)
    {
        public Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body = null)
            => Client.SendWithTokenAsync(method, path, Token, body);
    }

    /// <summary>
    /// Ciudadanos, gestores y administradores listos para usar.
    /// </summary>
    /// <remarks>
    /// Se escriben directo en la base, ya activos y con el correo confirmado: el
    /// registro y la confirmación tienen sus propias pruebas, y aquí solo hace
    /// falta alguien que exista. La sesión sí se abre por HTTP, como en la app.
    /// </remarks>
    public static class Actors
    {
        // Un solo hash para todos: BCrypt tarda a propósito, y calcularlo por
        // usuario alargaría la tanda sin probar nada más.
        private static readonly Lazy<string> PasswordHash =
            new(() => BCrypt.Net.BCrypt.HashPassword(Api.Password));

        public static Task<Actor> CitizenAsync(this RecyRouteApiFactory api) => api.ActorAsync(SeedIds.Roles.Citizen);

        public static Task<Actor> ManagerAsync(this RecyRouteApiFactory api) => api.ActorAsync(SeedIds.Roles.Manager);

        public static Task<Actor> AdministratorAsync(this RecyRouteApiFactory api) => api.ActorAsync(SeedIds.Roles.Administrator);

        /// <summary>Usuario activo y confirmado, sin sesión. Devuelve su id.</summary>
        public static async Task<Guid> CreateUserAsync(this RecyRouteApiFactory api, Guid roleId, string email)
        {
            var user = new User
            {
                IdRole = roleId,
                IdDocumentType = SeedIds.DocumentTypes.CedulaCiudadania,
                DocumentNumber = Api.NewDocumentNumber(),
                Name = "Test",
                LastName = "RecyRoute",
                Email = email,
                Password = PasswordHash.Value,
                PhoneNumber = "3001234567",
                Address = "Calle 1 # 2-3",
                IsActive = true,
                IsEmailVerified = true
            };

            await api.InDatabaseAsync(db =>
            {
                db.Users.Add(user);
                return db.SaveChangesAsync();
            });

            return user.IdUser;
        }

        /// <summary>Consulta o arreglo directo sobre la base, con su propio scope.</summary>
        public static async Task<T> InDatabaseAsync<T>(this RecyRouteApiFactory api, Func<RecyRouteDbContext, Task<T>> use)
        {
            using var scope = api.Services.CreateScope();
            return await use(scope.ServiceProvider.GetRequiredService<RecyRouteDbContext>());
        }

        private static async Task<Actor> ActorAsync(this RecyRouteApiFactory api, Guid roleId)
        {
            var email = Api.NewEmail();
            var id = await api.CreateUserAsync(roleId, email);

            var client = api.NewClient();
            var session = await (await client.LoginAsync(email)).ReadSessionAsync();

            return new Actor(client, id, email, session.Token);
        }
    }
}
