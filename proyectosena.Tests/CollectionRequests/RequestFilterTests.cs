using System.Net;
using Microsoft.EntityFrameworkCore;
using proyectosena.DTOs.Common;
using proyectosena.DTOs.Requests;
using proyectosena.Tests.Infrastructure;

namespace proyectosena.Tests.CollectionRequests
{
    /// <summary>
    /// Los filtros de la lista general de solicitudes y el gestor que viaja en
    /// cada una. Casi todas las pruebas se acotan al ciudadano que crean, porque
    /// la base la comparten todas las pruebas de la tanda.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class RequestFilterTests
    {
        private const string ListPath = "/api/collectionrequest/GetCollectionRequests";

        private readonly RecyRouteApiFactory _api;

        public RequestFilterTests(RecyRouteApiFactory api) => _api = api;

        [Fact]
        public async Task WithoutFilters_ListsEverything_NewestFirst()
        {
            var administrator = await _api.AdministratorAsync();
            await (await _api.CitizenAsync()).CreateRequestAsync();

            var page = await ListAsync(administrator, "pageSize=100");
            var total = await _api.InDatabaseAsync(db => db.CollectionRequests.CountAsync());

            Assert.Equal(total, page.TotalItems);
            Assert.Equal(
                page.Items.OrderByDescending(r => r.RequestDate).Select(r => r.IdRequest),
                page.Items.Select(r => r.IdRequest));
        }

        [Fact]
        public async Task ByCitizen_ReturnsOnlyTheirs()
        {
            var administrator = await _api.AdministratorAsync();
            var citizen = await _api.CitizenAsync();
            await citizen.CreateRequestAsync();
            await citizen.CreateRequestAsync();
            await (await _api.CitizenAsync()).CreateRequestAsync();

            var page = await ListAsync(administrator, $"idCitizen={citizen.Id}");

            Assert.Equal(2, page.TotalItems);
            Assert.All(page.Items, r => Assert.Equal(citizen.Id, r.IdUser));
        }

        // La lista también es del gestor: los filtros le sirven igual
        [Fact]
        public async Task ByStatus_TakesOneOrSeveral()
        {
            var citizen = await _api.CitizenAsync();
            var manager = await _api.ManagerAsync();
            var pending = await citizen.CreateRequestAsync();
            var assigned = await citizen.CreateRequestAsync();
            var cancelled = await citizen.CreateRequestAsync();
            Assert.Equal(HttpStatusCode.OK, (await manager.AcceptAsync(assigned.IdRequest)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await citizen.CancelAsync(cancelled.IdRequest)).StatusCode);

            var onlyPending = await ListAsync(manager, $"idCitizen={citizen.Id}&status=Pending");
            var pendingOrCancelled = await ListAsync(manager, $"idCitizen={citizen.Id}&status=Pending,Cancelled");

            Assert.Equal(pending.IdRequest, Assert.Single(onlyPending.Items).IdRequest);
            Assert.Equal(
                new[] { pending.IdRequest, cancelled.IdRequest }.OrderBy(id => id),
                pendingOrCancelled.Items.Select(r => r.IdRequest).OrderBy(id => id));
        }

        [Fact]
        public async Task ByManager_ReturnsTheirs_AndEveryListNamesTheManager()
        {
            var administrator = await _api.AdministratorAsync();
            var citizen = await _api.CitizenAsync();
            var manager = await _api.ManagerAsync();
            var managerName = await RenameAsync(manager.Id);
            var taken = await citizen.CreateRequestAsync();
            var waiting = await citizen.CreateRequestAsync();
            Assert.Equal(HttpStatusCode.OK, (await manager.AcceptAsync(taken.IdRequest)).StatusCode);

            var byManager = await ListAsync(administrator, $"idManager={manager.Id}");
            var citizensOwn = await (await citizen.SendAsync(HttpMethod.Get, "/api/collectionrequest/GetRequestsByUser"))
                .ReadAsync<PagedResult<CollectionRequestResponseDto>>();
            var detail = await (await citizen.SendAsync(HttpMethod.Get, $"/api/collectionrequest/GetCollectionRequestById?idRequest={taken.IdRequest}"))
                .ReadAsync<CollectionRequestResponseDto>();

            var listed = Assert.Single(byManager.Items);
            Assert.Equal(taken.IdRequest, listed.IdRequest);
            Assert.Equal(managerName, listed.ManagerName);

            Assert.Equal(manager.Id, detail.IdManager);
            Assert.Equal(managerName, detail.ManagerName);

            Assert.Equal(managerName, citizensOwn.Items.Single(r => r.IdRequest == taken.IdRequest).ManagerName);
            var untouched = citizensOwn.Items.Single(r => r.IdRequest == waiting.IdRequest);
            Assert.Null(untouched.IdManager);
            Assert.Null(untouched.ManagerName);
        }

        [Fact]
        public async Task AfterReassigning_TheNewManagerIsTheOneShown()
        {
            var administrator = await _api.AdministratorAsync();
            var citizen = await _api.CitizenAsync();
            var first = await _api.ManagerAsync();
            var second = await _api.ManagerAsync();
            var secondName = await RenameAsync(second.Id);
            var request = await citizen.CreateRequestAsync();
            Assert.Equal(HttpStatusCode.OK, (await first.AcceptAsync(request.IdRequest)).StatusCode);

            var reassign = await administrator.SendAsync(HttpMethod.Patch,
                $"/api/Admin/ReassignRequest?idRequest={request.IdRequest}&idNewManager={second.Id}");
            Assert.Equal(HttpStatusCode.OK, reassign.StatusCode);

            Assert.Empty((await ListAsync(administrator, $"idManager={first.Id}")).Items);
            var moved = Assert.Single((await ListAsync(administrator, $"idManager={second.Id}")).Items);
            Assert.Equal(second.Id, moved.IdManager);
            Assert.Equal(secondName, moved.ManagerName);
        }

        // Las 23:30 del 9 de marzo en Bogotá son las 04:30 UTC del 10: la
        // solicitud pertenece al 9, aunque en UTC ya sea otro día.
        [Fact]
        public async Task CreationDates_AreColombianDays()
        {
            var administrator = await _api.AdministratorAsync();
            var citizen = await _api.CitizenAsync();
            var lateOnThe9th = await citizen.CreateRequestAsync();
            var earlyOnThe10th = await citizen.CreateRequestAsync();
            await SetRequestDateAsync(lateOnThe9th.IdRequest, new DateTime(2026, 3, 10, 4, 30, 0, DateTimeKind.Utc));
            await SetRequestDateAsync(earlyOnThe10th.IdRequest, new DateTime(2026, 3, 10, 5, 30, 0, DateTimeKind.Utc));

            var the9th = await ListAsync(administrator, $"idCitizen={citizen.Id}&createdFrom=2026-03-09&createdTo=2026-03-09");
            var the10th = await ListAsync(administrator, $"idCitizen={citizen.Id}&createdFrom=2026-03-10&createdTo=2026-03-10");

            Assert.Equal(lateOnThe9th.IdRequest, Assert.Single(the9th.Items).IdRequest);
            Assert.Equal(earlyOnThe10th.IdRequest, Assert.Single(the10th.Items).IdRequest);
        }

        [Fact]
        public async Task CollectionDates_IncludeBothEnds()
        {
            var administrator = await _api.AdministratorAsync();
            var citizen = await _api.CitizenAsync();
            var today = DateTime.UtcNow.Date;
            var inThree = await CreateAsync(citizen, collectionDate: today.AddDays(3));
            var inFive = await CreateAsync(citizen, collectionDate: today.AddDays(5));
            await CreateAsync(citizen, collectionDate: today.AddDays(7));

            var page = await ListAsync(administrator,
                $"idCitizen={citizen.Id}&collectionFrom={today.AddDays(3):yyyy-MM-dd}&collectionTo={today.AddDays(5):yyyy-MM-dd}");

            Assert.Equal(
                new[] { inThree.IdRequest, inFive.IdRequest }.OrderBy(id => id),
                page.Items.Select(r => r.IdRequest).OrderBy(id => id));
        }

        [Fact]
        public async Task Search_FindsAddressPhoneOrCitizenName_IgnoringCase()
        {
            var administrator = await _api.AdministratorAsync();
            var citizen = await _api.CitizenAsync();
            var token = Guid.NewGuid().ToString("N")[..10];
            var phone = "3" + Random.Shared.NextInt64(100_000_000, 1_000_000_000);
            var byAddress = await CreateAsync(citizen, address: $"Carrera {token} # 1-1");
            var byPhone = await CreateAsync(citizen, phone: phone);

            var named = await _api.CitizenAsync();
            var fullName = await RenameAsync(named.Id);
            var byName = await named.CreateRequestAsync();

            var addressHit = await ListAsync(administrator, $"idCitizen={citizen.Id}&search={token.ToUpperInvariant()}");
            var phoneHit = await ListAsync(administrator, $"idCitizen={citizen.Id}&search={phone}");
            var nameHit = await ListAsync(administrator, $"search={Uri.EscapeDataString(fullName.ToLowerInvariant())}");

            Assert.Equal(byAddress.IdRequest, Assert.Single(addressHit.Items).IdRequest);
            Assert.Equal(byPhone.IdRequest, Assert.Single(phoneHit.Items).IdRequest);
            Assert.Equal(byName.IdRequest, Assert.Single(nameHit.Items).IdRequest);
        }

        // % y _ son comodines de LIKE. Sin escaparlos, buscar "50%" encontraría
        // cualquier dirección que tenga un 50.
        [Fact]
        public async Task Search_TakesWildcardsAsText()
        {
            var administrator = await _api.AdministratorAsync();
            var citizen = await _api.CitizenAsync();
            var withPercent = await CreateAsync(citizen, address: "Calle 50% norte");
            await CreateAsync(citizen, address: "Calle 50 norte");

            var page = await ListAsync(administrator, $"idCitizen={citizen.Id}&search={Uri.EscapeDataString("50%")}");

            Assert.Equal(withPercent.IdRequest, Assert.Single(page.Items).IdRequest);
        }

        [Fact]
        public async Task OrderedByCollectionDate_PagesWithoutRepeating()
        {
            var administrator = await _api.AdministratorAsync();
            var citizen = await _api.CitizenAsync();
            var today = DateTime.UtcNow.Date;
            var last = await CreateAsync(citizen, collectionDate: today.AddDays(9));
            var firstOne = await CreateAsync(citizen, collectionDate: today.AddDays(3));
            var middle = await CreateAsync(citizen, collectionDate: today.AddDays(6));

            var query = $"idCitizen={citizen.Id}&orderBy=collectionDate&direction=asc&pageSize=2";
            var pageOne = await ListAsync(administrator, query + "&page=1");
            var pageTwo = await ListAsync(administrator, query + "&page=2");

            Assert.Equal(3, pageOne.TotalItems);
            Assert.Equal(new[] { firstOne.IdRequest, middle.IdRequest }, pageOne.Items.Select(r => r.IdRequest));
            Assert.Equal(new[] { last.IdRequest }, pageTwo.Items.Select(r => r.IdRequest));
        }

        // Un filtro mal escrito no se ignora: devolver la lista completa haría
        // creer que el filtro funcionó.
        [Theory]
        [InlineData("status=Nope")]
        [InlineData("status=Pending,Nope")]
        [InlineData("createdFrom=2026-09-10&createdTo=2026-09-01")]
        [InlineData("collectionFrom=2026-09-10&collectionTo=2026-09-01")]
        [InlineData("orderBy=name")]
        [InlineData("direction=up")]
        [InlineData("createdFrom=2026-13-01")]
        [InlineData("idManager=not-a-guid")]
        public async Task InvalidFilter_Returns400(string query)
        {
            var administrator = await _api.AdministratorAsync();

            var response = await administrator.SendAsync(HttpMethod.Get, $"{ListPath}?{query}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ── Ayudas ──────────────────────────────────────────────────────

        private static async Task<PagedResult<CollectionRequestResponseDto>> ListAsync(Actor who, string query)
            => await (await who.SendAsync(HttpMethod.Get, $"{ListPath}?{query}"))
                .ReadAsync<PagedResult<CollectionRequestResponseDto>>();

        // El formulario del ciudadano con los campos que la prueba necesita distintos
        private static async Task<CollectionRequestResponseDto> CreateAsync(
            Actor citizen, string? address = null, string? phone = null, DateTime? collectionDate = null)
        {
            var body = CollectionRequestSteps.RequestBody();
            if (address != null) body["collectionAddress"] = address;
            if (phone != null) body["contactPhone"] = phone;
            if (collectionDate != null) body["collectionDate"] = collectionDate.Value.ToString("yyyy-MM-dd");

            return await (await citizen.SendAsync(HttpMethod.Post, "/api/collectionrequest/CreateCollectionRequest", body))
                .ReadAsync<CollectionRequestResponseDto>();
        }

        // Todos los actores se llaman igual; un nombre propio deja comprobar que
        // la respuesta trae el de esa persona y no el de otra.
        private async Task<string> RenameAsync(Guid idUser)
        {
            var name = "Nombre" + Guid.NewGuid().ToString("N")[..8];
            await _api.InDatabaseAsync(db => db.Users
                .Where(u => u.IdUser == idUser)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.Name, name)
                    .SetProperty(u => u.LastName, "Prueba")));
            return $"{name} Prueba";
        }

        private Task<int> SetRequestDateAsync(Guid idRequest, DateTime utc)
            => _api.InDatabaseAsync(db => db.CollectionRequests
                .Where(r => r.IdRequest == idRequest)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.RequestDate, utc)));
    }
}
