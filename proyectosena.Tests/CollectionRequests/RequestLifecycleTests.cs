using System.Net;
using Microsoft.EntityFrameworkCore;
using proyectosena.DTOs.Requests;
using proyectosena.Models;
using proyectosena.Tests.Infrastructure;

namespace proyectosena.Tests.CollectionRequests
{
    /// <summary>
    /// La máquina de estados de una solicitud: por dónde puede avanzar, quién la
    /// mueve y qué deja de poder hacerse cuando un gestor la toma.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class RequestLifecycleTests
    {
        private readonly RecyRouteApiFactory _api;

        public RequestLifecycleTests(RecyRouteApiFactory api) => _api = api;

        [Fact]
        public async Task FullLifecycle_IsRecordedStepByStepInHistory()
        {
            var citizen = await _api.CitizenAsync();
            var manager = await _api.ManagerAsync();
            var request = await citizen.CreateRequestAsync();

            Assert.Equal(CollectionRequestStatus.Pending, request.CurrentStatus);
            Assert.Equal(HttpStatusCode.OK, (await manager.AcceptAsync(request.IdRequest)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await manager.ChangeStatusAsync(request.IdRequest, CollectionRequestStatus.InProgress)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await manager.ChangeStatusAsync(request.IdRequest, CollectionRequestStatus.Completed)).StatusCode);

            var history = await (await citizen.SendAsync(HttpMethod.Get, $"/api/history/GetByRequest?idRequest={request.IdRequest}"))
                .ReadAsync<List<HistoryResponseDto>>();

            Assert.Equal(
                new[] { "Pending→Assigned", "Assigned→InProgress", "InProgress→Completed" },
                history.OrderBy(h => h.ChangeDate).Select(h => $"{h.PreviousStatus}→{h.NewStatus}"));
            Assert.All(history, h => Assert.Equal(manager.Id, h.IdUser));
            Assert.Equal(CollectionRequestStatus.Completed, await _api.StatusInDatabaseAsync(request.IdRequest));
        }

        // Pending no lista Assigned a propósito: llegar ahí exige crear también la
        // fila que dice quién es el gestor, y eso solo lo hace AcceptRequest.
        [Fact]
        public async Task PendingToAssigned_WithoutAccepting_IsNotAllowed()
        {
            var citizen = await _api.CitizenAsync();
            var request = await citizen.CreateRequestAsync();

            // Un administrador se salta la comprobación de asignación: así quien
            // responde es la máquina de estados
            var administrator = await _api.AdministratorAsync();
            var response = await administrator.ChangeStatusAsync(request.IdRequest, CollectionRequestStatus.Assigned);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(CollectionRequestStatus.Pending, await _api.StatusInDatabaseAsync(request.IdRequest));
            Assert.Equal(0, await _api.InDatabaseAsync(db => db.CollectionManagements.CountAsync(m => m.IdRequest == request.IdRequest)));
        }

        [Theory]
        [InlineData(CollectionRequestStatus.Completed)]  // saltarse InProgress
        [InlineData(CollectionRequestStatus.Pending)]    // volver atrás
        [InlineData(CollectionRequestStatus.Cancelled)]  // cancelar lo que ya tomó un gestor
        public async Task FromAssigned_CannotLeaveThePath(string target)
        {
            var (manager, requestId) = await AssignedRequestAsync();

            var response = await manager.ChangeStatusAsync(requestId, target);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(CollectionRequestStatus.Assigned, await _api.StatusInDatabaseAsync(requestId));
        }

        [Theory]
        [InlineData(CollectionRequestStatus.Completed)]
        [InlineData(CollectionRequestStatus.Rejected)]
        public async Task FinalStatus_CannotChange(string finalStatus)
        {
            var (manager, requestId) = await AssignedRequestAsync();
            var path = finalStatus == CollectionRequestStatus.Completed
                ? new[] { CollectionRequestStatus.InProgress, CollectionRequestStatus.Completed }
                : new[] { CollectionRequestStatus.Rejected };

            foreach (var status in path)
                Assert.Equal(HttpStatusCode.OK, (await manager.ChangeStatusAsync(requestId, status)).StatusCode);

            Assert.Equal(HttpStatusCode.Conflict, (await manager.ChangeStatusAsync(requestId, CollectionRequestStatus.InProgress)).StatusCode);
            Assert.Equal(finalStatus, await _api.StatusInDatabaseAsync(requestId));
        }

        [Fact]
        public async Task UnknownStatus_Returns400()
        {
            var (manager, requestId) = await AssignedRequestAsync();

            var response = await manager.ChangeStatusAsync(requestId, "Finished");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(CollectionRequestStatus.Assigned, await _api.StatusInDatabaseAsync(requestId));
        }

        // Antes bastaba el rol: cualquier gestor cerraba la solicitud de otro y el
        // historial quedaba firmado con su nombre
        [Fact]
        public async Task ManagerWithoutTheRequest_CannotMoveIt()
        {
            var (_, requestId) = await AssignedRequestAsync();
            var otherManager = await _api.ManagerAsync();

            var response = await otherManager.ChangeStatusAsync(requestId, CollectionRequestStatus.InProgress);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(CollectionRequestStatus.Assigned, await _api.StatusInDatabaseAsync(requestId));
            Assert.Equal(0, await _api.InDatabaseAsync(db => db.Histories.CountAsync(h => h.IdRequest == requestId && h.IdUser == otherManager.Id)));
        }

        // Cuando un gestor la toma ya organizó su ruta con esa dirección
        [Fact]
        public async Task Edit_OnlyWhileNobodyHasTakenIt()
        {
            var citizen = await _api.CitizenAsync();
            var manager = await _api.ManagerAsync();
            var request = await citizen.CreateRequestAsync();

            var before = await citizen.EditAddressAsync(request.IdRequest, "Calle Nueva # 1-1");
            (await manager.AcceptAsync(request.IdRequest)).EnsureSuccessStatusCode();
            var after = await citizen.EditAddressAsync(request.IdRequest, "Otra Calle # 2-2");

            Assert.Equal(HttpStatusCode.OK, before.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, after.StatusCode);
            Assert.Equal("Calle Nueva # 1-1", await _api.AddressInDatabaseAsync(request.IdRequest));
        }

        [Fact]
        public async Task Cancel_OnlyWhileNobodyHasTakenIt()
        {
            var citizen = await _api.CitizenAsync();
            var manager = await _api.ManagerAsync();
            var free = await citizen.CreateRequestAsync();
            var taken = await citizen.CreateRequestAsync();
            (await manager.AcceptAsync(taken.IdRequest)).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.OK, (await citizen.CancelAsync(free.IdRequest)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await citizen.CancelAsync(taken.IdRequest)).StatusCode);
            Assert.Equal(CollectionRequestStatus.Cancelled, await _api.StatusInDatabaseAsync(free.IdRequest));
            Assert.Equal(CollectionRequestStatus.Assigned, await _api.StatusInDatabaseAsync(taken.IdRequest));
        }

        private async Task<(Actor Manager, Guid RequestId)> AssignedRequestAsync()
        {
            var citizen = await _api.CitizenAsync();
            var manager = await _api.ManagerAsync();
            var request = await citizen.CreateRequestAsync();

            (await manager.AcceptAsync(request.IdRequest)).EnsureSuccessStatusCode();

            return (manager, request.IdRequest);
        }
    }
}
