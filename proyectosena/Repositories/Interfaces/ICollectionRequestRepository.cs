using proyectosena.Models;

namespace proyectosena.Interfaces
{
    public interface ICollectionRequestRepository
    {
        // Obtiene todas las solicitudes en estado Pending disponibles para ser tomadas por un gestor
        Task<List<CollectionRequest>> GetPendingRequests();
        // Obtiene todas las solicitudes de recolección
        Task<List<CollectionRequest>> GetCollectionRequests();

        // Obtiene una solicitud de recolección por su ID
        Task<CollectionRequest> GetCollectionRequest(Guid idRequest);

        // El ciudadano consulta sus propias solicitudes directamente
        Task<IEnumerable<CollectionRequest>> GetRequestsByUser(Guid idUser);

        // Crea una nueva solicitud de recolección
        Task<CollectionRequest> CreateCollectionRequest(CollectionRequest collectionRequest);

        // Actualiza una solicitud de recolección existente
        Task<CollectionRequest> UpdateCollectionRequest(CollectionRequest collectionRequest);

        // Elimina una solicitud de recolección por su ID
        Task<bool> DeleteCollectionRequest(Guid idRequest);
    }
}