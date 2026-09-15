using System.Net;
using Microsoft.EntityFrameworkCore;
using proyectosena.DTOs.Requests;
using proyectosena.Models;
using proyectosena.Tests.Infrastructure;

namespace proyectosena.Tests.CollectionRequests
{
    /// <summary>
    /// Un ciudadano con sesión válida intentando ver o tocar la solicitud de otro
    /// con solo conocer su id. Cada caso fue un agujero real antes de sacar la
    /// identidad del token.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class OtherCitizensRequestTests
    {
        private readonly RecyRouteApiFactory _api;

        public OtherCitizensRequestTests(RecyRouteApiFactory api) => _api = api;

        // Trae la dirección y el teléfono de la dueña
        [Fact]
        public async Task ReadingSomeoneElsesRequest_Returns403()
        {
            var (owner, intruder, request) = await OwnerAndIntruderAsync();
            var path = $"/api/collectionrequest/GetCollectionRequestById?idRequest={request.IdRequest}";

            Assert.Equal(HttpStatusCode.Forbidden, (await intruder.SendAsync(HttpMethod.Get, path)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await owner.SendAsync(HttpMethod.Get, path)).StatusCode);
        }

        [Theory]
        [InlineData("/api/history/GetByRequest")]
        [InlineData("/api/chathistory/GetMessagesByRequest")]
        [InlineData("/api/chathistory/GetUnreadMessages")]
        [InlineData("/api/collectionmanagement/GetByRequest")]
        public async Task SomeoneElsesRequestData_Returns403(string endpoint)
        {
            var (owner, intruder, request) = await OwnerAndIntruderAsync();
            var path = $"{endpoint}?idRequest={request.IdRequest}";

            Assert.Equal(HttpStatusCode.Forbidden, (await intruder.SendAsync(HttpMethod.Get, path)).StatusCode);

            // A la dueña no se le niega: que el 403 sea por ser ajena, no porque
            // el endpoint rechace a todo el mundo
            Assert.NotEqual(HttpStatusCode.Forbidden, (await owner.SendAsync(HttpMethod.Get, path)).StatusCode);
        }

        [Fact]
        public async Task EditingSomeoneElsesRequest_Returns403_AndChangesNothing()
        {
            var (_, intruder, request) = await OwnerAndIntruderAsync();

            var response = await intruder.EditAddressAsync(request.IdRequest, "Calle del Intruso # 6-6");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(request.CollectionAddress, await _api.AddressInDatabaseAsync(request.IdRequest));
        }

        [Fact]
        public async Task CancellingSomeoneElsesRequest_Returns403_AndItStaysPending()
        {
            var (_, intruder, request) = await OwnerAndIntruderAsync();

            var response = await intruder.CancelAsync(request.IdRequest);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(CollectionRequestStatus.Pending, await _api.StatusInDatabaseAsync(request.IdRequest));
        }

        [Fact]
        public async Task WritingInSomeoneElsesChat_Returns403_AndSavesNothing()
        {
            var (_, intruder, request) = await OwnerAndIntruderAsync();

            var response = await intruder.SendAsync(HttpMethod.Post, "/api/chathistory/SendMessage",
                new { idRequest = request.IdRequest, message = "Hola, soy yo" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(0, await _api.InDatabaseAsync(db => db.ChatHistories.CountAsync(c => c.IdRequest == request.IdRequest)));
        }

        // IdUser ya no viaja en el cuerpo. Si alguien lo manda igual, se ignora:
        // la solicitud queda a nombre de quien tiene el token.
        [Fact]
        public async Task CreatingWithSomeoneElsesId_BelongsToTheCaller()
        {
            var owner = await _api.CitizenAsync();
            var intruder = await _api.CitizenAsync();
            var body = CollectionRequestSteps.RequestBody();
            body["idUser"] = owner.Id;

            var created = await (await intruder.SendAsync(HttpMethod.Post, "/api/collectionrequest/CreateCollectionRequest", body))
                .ReadAsync<CollectionRequestResponseDto>();

            Assert.Equal(intruder.Id, created.IdUser);
        }

        private async Task<(Actor Owner, Actor Intruder, CollectionRequestResponseDto Request)> OwnerAndIntruderAsync()
        {
            var owner = await _api.CitizenAsync();
            var intruder = await _api.CitizenAsync();
            var request = await owner.CreateRequestAsync();

            return (owner, intruder, request);
        }
    }
}
