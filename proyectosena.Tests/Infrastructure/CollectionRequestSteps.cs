using Microsoft.EntityFrameworkCore;
using proyectosena.DTOs.Requests;

namespace proyectosena.Tests.Infrastructure
{
    /// <summary>
    /// Lo que se hace con una solicitud de recolección, por HTTP y con el token de quien actúa.
    /// </summary>
    public static class CollectionRequestSteps
    {
        private const string BasePath = "/api/collectionrequest";

        /// <summary>El formulario del ciudadano, como lo manda el navegador.</summary>
        /// <remarks>
        /// La fecha va como "yyyy-MM-dd", igual que un input de fecha. Mandada con
        /// zona UTC, PostgreSQL la rechazaría en su columna sin zona horaria.
        /// </remarks>
        public static Dictionary<string, object> RequestBody() => new()
        {
            ["collectionDate"] = DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd"),
            ["collectionTime"] = "10:30",
            ["collectionAddress"] = "Carrera 10 # 20-30",
            ["contactPhone"] = "3001234567",
            ["wasteTypes"] = "Cartón y vidrio"
        };

        public static async Task<CollectionRequestResponseDto> CreateRequestAsync(this Actor citizen)
            => await (await citizen.SendAsync(HttpMethod.Post, $"{BasePath}/CreateCollectionRequest", RequestBody()))
                .ReadAsync<CollectionRequestResponseDto>();

        public static Task<HttpResponseMessage> AcceptAsync(this Actor manager, Guid requestId)
            => manager.SendAsync(HttpMethod.Post, $"{BasePath}/AcceptRequest?idRequest={requestId}");

        public static Task<HttpResponseMessage> ChangeStatusAsync(this Actor actor, Guid requestId, string status)
            => actor.SendAsync(HttpMethod.Patch, $"{BasePath}/UpdateStatus?idRequest={requestId}&newStatus={status}");

        public static Task<HttpResponseMessage> CancelAsync(this Actor citizen, Guid requestId)
            => citizen.SendAsync(HttpMethod.Patch, $"{BasePath}/CancelRequest?idRequest={requestId}");

        public static Task<HttpResponseMessage> EditAddressAsync(this Actor citizen, Guid requestId, string address)
            => citizen.SendAsync(HttpMethod.Put, $"{BasePath}/UpdateCollectionRequest",
                new { idRequest = requestId, collectionAddress = address });

        /// <summary>El estado guardado en la base, sin pasar por la API.</summary>
        public static Task<string> StatusInDatabaseAsync(this RecyRouteApiFactory api, Guid requestId)
            => api.InDatabaseAsync(db => db.CollectionRequests
                .Where(r => r.IdRequest == requestId)
                .Select(r => r.CurrentStatus)
                .SingleAsync());

        /// <summary>La dirección guardada en la base, sin pasar por la API.</summary>
        public static Task<string> AddressInDatabaseAsync(this RecyRouteApiFactory api, Guid requestId)
            => api.InDatabaseAsync(db => db.CollectionRequests
                .Where(r => r.IdRequest == requestId)
                .Select(r => r.CollectionAddress)
                .SingleAsync());
    }
}
