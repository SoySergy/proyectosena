using System.Net;
using Microsoft.EntityFrameworkCore;
using proyectosena.DTOs.Common;
using proyectosena.DTOs.User;
using proyectosena.Models;
using proyectosena.Tests.Infrastructure;

namespace proyectosena.Tests.Users
{
    /// <summary>
    /// Los filtros del listado de usuarios. Cada prueba marca a los suyos con un
    /// apellido irrepetible y busca por él: la base la comparten todas las
    /// pruebas de la tanda.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class UserFilterTests
    {
        private const string ListPath = "/api/user/GetUsers";

        private readonly RecyRouteApiFactory _api;

        public UserFilterTests(RecyRouteApiFactory api) => _api = api;

        [Fact]
        public async Task WithoutFilters_ListsOnlyActiveUsers()
        {
            var administrator = await _api.AdministratorAsync();
            var leaving = await _api.CitizenAsync();
            Assert.Equal(HttpStatusCode.OK, (await DeactivateAsync(administrator, leaving.Id)).StatusCode);

            var page = await ListAsync(administrator, "pageSize=1");
            var active = await _api.InDatabaseAsync(db => db.Users.CountAsync(u => u.IsActive));

            Assert.Equal(active, page.TotalItems);
        }

        [Fact]
        public async Task ByRole_ReturnsOnlyThatRole()
        {
            var administrator = await _api.AdministratorAsync();
            var tag = await TagAsync(await _api.CitizenAsync(), await _api.ManagerAsync());

            var managers = await ListAsync(administrator, $"search={tag}&role=Manager");
            var citizens = await ListAsync(administrator, $"search={tag}&role=citizen");

            Assert.Equal(RoleNames.Manager, Assert.Single(managers.Items).RoleName);
            Assert.Equal(RoleNames.Citizen, Assert.Single(citizens.Items).RoleName);
        }

        // Quien está dado de baja no aparece salvo que se pida
        [Fact]
        public async Task ByState_HidesDeactivatedUnlessAsked()
        {
            var administrator = await _api.AdministratorAsync();
            var staying = await _api.CitizenAsync();
            var leaving = await _api.CitizenAsync();
            var tag = await TagAsync(staying, leaving);
            Assert.Equal(HttpStatusCode.OK, (await DeactivateAsync(administrator, leaving.Id)).StatusCode);

            var byDefault = await ListAsync(administrator, $"search={tag}");
            var inactive = await ListAsync(administrator, $"search={tag}&state=inactive");
            var all = await ListAsync(administrator, $"search={tag}&state=all");

            Assert.Equal(staying.Id, Assert.Single(byDefault.Items).IdUser);
            Assert.True(Assert.Single(byDefault.Items).IsActive);

            var gone = Assert.Single(inactive.Items);
            Assert.Equal(leaving.Id, gone.IdUser);
            Assert.False(gone.IsActive);

            Assert.Equal(2, all.TotalItems);
        }

        [Fact]
        public async Task ByEmailVerified_FindsWhoNeverConfirmed()
        {
            var administrator = await _api.AdministratorAsync();
            var confirmed = await _api.CitizenAsync();

            var email = Api.NewEmail();
            (await _api.NewClient().RegisterAsync(email)).EnsureSuccessStatusCode();
            var pending = await _api.InDatabaseAsync(db => db.Users
                .Where(u => u.Email == email)
                .Select(u => u.IdUser)
                .SingleAsync());

            var tag = await TagUsersAsync(confirmed.Id, pending);

            var waiting = await ListAsync(administrator, $"search={tag}&emailVerified=false");
            var done = await ListAsync(administrator, $"search={tag}&emailVerified=true");

            var unconfirmed = Assert.Single(waiting.Items);
            Assert.Equal(pending, unconfirmed.IdUser);
            Assert.False(unconfirmed.IsEmailVerified);

            Assert.Equal(confirmed.Id, Assert.Single(done.Items).IdUser);
            Assert.True(Assert.Single(done.Items).IsEmailVerified);
        }

        [Fact]
        public async Task Search_FindsNameEmailOrDocument_IgnoringCase()
        {
            var administrator = await _api.AdministratorAsync();
            var citizen = await _api.CitizenAsync();
            var tag = await TagAsync(citizen);
            var document = await _api.InDatabaseAsync(db => db.Users
                .Where(u => u.IdUser == citizen.Id)
                .Select(u => u.DocumentNumber)
                .SingleAsync());

            var byName = await ListAsync(administrator, $"search={Uri.EscapeDataString("test " + tag.ToUpperInvariant())}");
            var byEmail = await ListAsync(administrator, $"search={Uri.EscapeDataString(citizen.Email.ToUpperInvariant())}");
            var byDocument = await ListAsync(administrator, $"search={document}");

            Assert.Equal(citizen.Id, Assert.Single(byName.Items).IdUser);
            Assert.Equal(citizen.Id, Assert.Single(byEmail.Items).IdUser);
            Assert.Equal(citizen.Id, Assert.Single(byDocument.Items).IdUser);
        }

        // % y _ son comodines de LIKE: sin escaparlos, "Ana50%" encontraría también "Ana50"
        [Fact]
        public async Task Search_TakesWildcardsAsText()
        {
            var administrator = await _api.AdministratorAsync();
            var withPercent = await _api.CitizenAsync();
            var plain = await _api.CitizenAsync();
            var token = "Nom" + Guid.NewGuid().ToString("N")[..8];
            await RenameAsync(withPercent.Id, token + "50%");
            await RenameAsync(plain.Id, token + "50");

            var page = await ListAsync(administrator, $"search={Uri.EscapeDataString(token + "50%")}");

            Assert.Equal(withPercent.Id, Assert.Single(page.Items).IdUser);
        }

        [Fact]
        public async Task OrderedByRegistrationDate_PagesWithoutRepeating()
        {
            var administrator = await _api.AdministratorAsync();
            var oldest = await _api.CitizenAsync();
            var middle = await _api.CitizenAsync();
            var newest = await _api.CitizenAsync();
            var tag = await TagAsync(oldest, middle, newest);
            await SetRegistrationDateAsync(oldest.Id, new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc));
            await SetRegistrationDateAsync(middle.Id, new DateTime(2026, 2, 10, 12, 0, 0, DateTimeKind.Utc));
            await SetRegistrationDateAsync(newest.Id, new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc));

            var query = $"search={tag}&orderBy=registrationDate&direction=desc&pageSize=2";
            var pageOne = await ListAsync(administrator, query + "&page=1");
            var pageTwo = await ListAsync(administrator, query + "&page=2");

            Assert.Equal(3, pageOne.TotalItems);
            Assert.Equal(new[] { newest.Id, middle.Id }, pageOne.Items.Select(u => u.IdUser));
            Assert.Equal(new[] { oldest.Id }, pageTwo.Items.Select(u => u.IdUser));
        }

        [Theory]
        [InlineData("role=Nope")]
        [InlineData("state=quizas")]
        [InlineData("orderBy=email")]
        [InlineData("direction=up")]
        [InlineData("emailVerified=puede")]
        public async Task InvalidFilter_Returns400(string query)
        {
            var administrator = await _api.AdministratorAsync();

            var response = await administrator.SendAsync(HttpMethod.Get, $"{ListPath}?{query}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ── Ayudas ──────────────────────────────────────────────────────

        private static async Task<PagedResult<UserInfoDto>> ListAsync(Actor who, string query)
            => await (await who.SendAsync(HttpMethod.Get, $"{ListPath}?{query}"))
                .ReadAsync<PagedResult<UserInfoDto>>();

        private static Task<HttpResponseMessage> DeactivateAsync(Actor administrator, Guid idUser)
            => administrator.SendAsync(HttpMethod.Delete, $"/api/user/DeleteUser?idUser={idUser}");

        // Un apellido irrepetible para las personas de una prueba: buscando por
        // él, la prueba solo ve las suyas.
        private Task<string> TagAsync(params Actor[] actors)
            => TagUsersAsync(actors.Select(a => a.Id).ToArray());

        private async Task<string> TagUsersAsync(params Guid[] ids)
        {
            var tag = "Apellido" + Guid.NewGuid().ToString("N")[..8];
            foreach (var id in ids)
                await _api.InDatabaseAsync(db => db.Users
                    .Where(u => u.IdUser == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastName, tag)));
            return tag;
        }

        private Task<int> RenameAsync(Guid idUser, string name)
            => _api.InDatabaseAsync(db => db.Users
                .Where(u => u.IdUser == idUser)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Name, name)));

        private Task<int> SetRegistrationDateAsync(Guid idUser, DateTime utc)
            => _api.InDatabaseAsync(db => db.Users
                .Where(u => u.IdUser == idUser)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.RegistrationDate, utc)));
    }
}
