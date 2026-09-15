using System.Net;
using Microsoft.EntityFrameworkCore;
using proyectosena.Interfaces.Services;
using proyectosena.Models;
using proyectosena.Tests.Infrastructure;

namespace proyectosena.Tests.Users
{
    /// <summary>
    /// Dar de baja a un usuario: la sesión muere con la baja, y el sistema nunca
    /// se queda sin administradores (WA-16).
    /// </summary>
    /// <remarks>
    /// Las pruebas del último administrador dejan antes la base sin ningún
    /// administrador activo, para decidir ellas exactamente cuántos hay. Es seguro
    /// porque las pruebas de la colección corren de una en una, y las que necesitan
    /// un administrador se crean el suyo.
    /// </remarks>
    [Collection(ApiCollection.Name)]
    public class UserDeactivationTests
    {
        private readonly RecyRouteApiFactory _api;

        public UserDeactivationTests(RecyRouteApiFactory api) => _api = api;

        [Fact]
        public async Task Deactivation_ClosesSessions_AndBlocksLogin()
        {
            var administrator = await _api.AdministratorAsync();
            var citizen = await _api.CitizenAsync();

            var deactivation = await administrator.SendAsync(HttpMethod.Delete, $"/api/user/DeleteUser?idUser={citizen.Id}");

            Assert.Equal(HttpStatusCode.OK, deactivation.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await citizen.Client.ProbeTokenAsync(citizen.Token)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await citizen.Client.LoginAsync(citizen.Email)).StatusCode);
        }

        [Fact]
        public async Task OnlyActiveAdministrator_CannotBeDeactivated()
        {
            await DeactivateAllAdministratorsAsync();
            var administrator = await _api.AdministratorAsync();

            var deactivation = await administrator.SendAsync(HttpMethod.Delete, $"/api/user/DeleteUser?idUser={administrator.Id}");

            Assert.Equal(HttpStatusCode.BadRequest, deactivation.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await administrator.Client.ProbeTokenAsync(administrator.Token)).StatusCode);
            Assert.Equal(1, await ActiveAdministratorsAsync());
        }

        // Sin el candado de UserRepository, todos contaban cuántos administradores
        // quedaban antes de que ninguno escribiera, todos pasaban el freno y el
        // sistema quedaba en cero. En rueda —cada uno da de baja al siguiente—
        // entran todas las bajas menos la última, que tiene que encontrarse con el
        // último administrador.
        //
        // Ocho y no cuatro: con cuatro y el candado quitado a propósito, la prueba
        // pasó en verde en una de diez corridas, porque las bajas llegaron a
        // ordenarse solas.
        [Fact]
        public async Task ManyAdministratorsDeactivatingAtOnce_OneAlwaysRemains()
        {
            await DeactivateAllAdministratorsAsync();

            var administrators = new List<Guid>();
            for (var i = 0; i < 8; i++)
                administrators.Add(await _api.CreateUserAsync(SeedIds.Roles.Administrator, Api.NewEmail()));

            var results = await _api.RunSimultaneouslyAsync<IUserService, UserDeactivationResult>(
                administrators.Count,
                (service, index) => service.Deactivate(administrators[(index + 1) % administrators.Count]));

            Assert.Equal(administrators.Count - 1, results.Count(r => r == UserDeactivationResult.Success));
            Assert.Single(results, r => r == UserDeactivationResult.LastAdministrator);
            Assert.Equal(1, await ActiveAdministratorsAsync());
        }

        private Task<int> DeactivateAllAdministratorsAsync()
            => _api.InDatabaseAsync(db => db.Users
                .Where(u => u.IdRole == SeedIds.Roles.Administrator && u.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false)));

        private Task<int> ActiveAdministratorsAsync()
            => _api.InDatabaseAsync(db => db.Users.CountAsync(u => u.IdRole == SeedIds.Roles.Administrator && u.IsActive));
    }
}
