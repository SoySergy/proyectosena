using proyectosena.Models;

namespace proyectosena.Interfaces.Repositories
{
    public interface ICollectionManagementRepository
    {
        // Gestión vigente de una solicitud: quién la tiene y desde cuándo.
        // Si hubo reasignaciones devuelve la más reciente; null si nadie la ha tomado.
        Task<CollectionManagement?> GetByRequest(Guid idRequest);
    }
}
