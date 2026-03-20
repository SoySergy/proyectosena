using proyectosena.Models;

namespace proyectosena.Interfaces
{
    public interface ICollectionManagementRepository
    {
        // Obtiene todas las gestiones de recolección
        Task<List<CollectionManagement>> GetCollectionManagements();

        // Obtiene una gestión de recolección por su ID
        Task<CollectionManagement> GetCollectionManagement(Guid idManagement);

        // Crea una nueva gestión de recolección
        Task<CollectionManagement> CreateCollectionManagement(CollectionManagement collectionManagement);

        // Actualiza una gestión de recolección existente
        Task<CollectionManagement> UpdateCollectionManagement(CollectionManagement collectionManagement);

        // Elimina una gestión de recolección por su ID
        Task<bool> DeleteCollectionManagement(Guid idManagement);
    }
}