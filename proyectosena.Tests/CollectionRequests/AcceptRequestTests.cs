using System.Net;
using Microsoft.EntityFrameworkCore;
using proyectosena.Interfaces.Services;
using proyectosena.Models;
using proyectosena.Tests.Infrastructure;

namespace proyectosena.Tests.CollectionRequests
{
    /// <summary>
    /// Modelo tipo Uber: todos los gestores ven la solicitud y el primero que la
    /// acepta se la queda. Nunca dos.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class AcceptRequestTests
    {
        private const string AlreadyTaken = "This request has already been taken by another manager.";

        private readonly RecyRouteApiFactory _api;

        public AcceptRequestTests(RecyRouteApiFactory api) => _api = api;

        [Fact]
        public async Task AcceptingATakenRequest_Returns400_AndKeepsTheManager()
        {
            var citizen = await _api.CitizenAsync();
            var first = await _api.ManagerAsync();
            var second = await _api.ManagerAsync();
            var request = await citizen.CreateRequestAsync();
            (await first.AcceptAsync(request.IdRequest)).EnsureSuccessStatusCode();

            var response = await second.AcceptAsync(request.IdRequest);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(AlreadyTaken, await response.Content.ReadAsStringAsync());
            Assert.Equal(first.Id, await _api.InDatabaseAsync(db => db.CollectionManagements
                .Where(m => m.IdRequest == request.IdRequest)
                .Select(m => m.IdManager)
                .SingleAsync()));
        }

        // Sin el FOR UPDATE de AssignmentService, varios gestores leen «Pending»
        // a la vez y todos se quedan con la misma solicitud
        [Fact]
        public async Task ManyManagersAcceptingAtOnce_OnlyOneGetsIt()
        {
            var citizen = await _api.CitizenAsync();
            var request = await citizen.CreateRequestAsync();

            var managers = new List<Guid>();
            for (var i = 0; i < 5; i++)
                managers.Add(await _api.CreateUserAsync(SeedIds.Roles.Manager, Api.NewEmail()));

            var results = await _api.RunSimultaneouslyAsync<IAssignmentService, (bool Success, string Message)>(
                managers.Count, (service, index) => service.AcceptRequestAsync(request.IdRequest, managers[index]));

            Assert.Single(results, r => r.Success);
            Assert.All(results.Where(r => !r.Success), r => Assert.Equal(AlreadyTaken, r.Message));
            Assert.Equal(1, await _api.InDatabaseAsync(db => db.CollectionManagements.CountAsync(m => m.IdRequest == request.IdRequest)));
            Assert.Equal(1, await _api.InDatabaseAsync(db => db.Histories.CountAsync(h => h.IdRequest == request.IdRequest)));
        }
    }
}
