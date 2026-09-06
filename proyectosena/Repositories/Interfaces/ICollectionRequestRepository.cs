using proyectosena.Models;

namespace proyectosena.Interfaces
{
    public interface ICollectionRequestRepository
    {
        // Página de solicitudes en estado Pending, disponibles para que un gestor las tome
        Task<(List<CollectionRequest> Items, int Total)> GetPendingRequests(int page, int pageSize);

        // Página de todas las solicitudes de recolección
        Task<(List<CollectionRequest> Items, int Total)> GetCollectionRequests(int page, int pageSize);

        // Obtiene una solicitud de recolección por su ID
        Task<CollectionRequest> GetCollectionRequest(Guid idRequest);

        // Solicitudes que un gestor específico tomó
        Task<(List<CollectionRequest> Items, int Total)> GetRequestsByManager(Guid idManager, int page, int pageSize);

        // El ciudadano consulta sus propias solicitudes directamente
        Task<(List<CollectionRequest> Items, int Total)> GetRequestsByUser(Guid idUser, int page, int pageSize);

        // Crea una nueva solicitud de recolección
        Task<CollectionRequest> CreateCollectionRequest(CollectionRequest collectionRequest);

        // Actualiza una solicitud de recolección existente
        Task<CollectionRequest> UpdateCollectionRequest(CollectionRequest collectionRequest);

        // Elimina una solicitud de recolección por su ID
        Task<bool> DeleteCollectionRequest(Guid idRequest);

        // Checks whether a user takes part in a request: either its owner or an assigned manager
        Task<bool> IsParticipant(Guid idRequest, Guid idUser);

        // Number of requests per current status, resolved in one grouped query
        Task<Dictionary<string, int>> GetStatusCounts();

        // Number of requests created on or after a given date
        Task<int> CountSince(DateTime since);
    }
}