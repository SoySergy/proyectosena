using proyectosena.Models;

namespace proyectosena.Interfaces
{
    public interface ICollectionManagementRepository
    {
        // Obtiene todas las gestiones de recolección
        Task<List<CollectionManagement>> GetCollectionManagements();

        // Obtiene una gestión de recolección por su ID
        Task<CollectionManagement> GetCollectionManagement(Guid idManagement);

        // Gestión vigente de una solicitud: quién la tiene y desde cuándo.
        // Devuelve null si nadie la ha tomado todavía.
        Task<CollectionManagement?> GetByRequest(Guid idRequest);

        // Actualiza una gestión de recolección existente
        Task<CollectionManagement> UpdateCollectionManagement(CollectionManagement collectionManagement);
    }
}