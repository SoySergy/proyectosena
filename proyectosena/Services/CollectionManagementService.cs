using proyectosena.DTOs.Collection;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Services
{
    public class CollectionManagementService : ICollectionManagementService
    {
        private readonly ICollectionManagementRepository _collectionManagementRepository;

        public CollectionManagementService(ICollectionManagementRepository collectionManagementRepository)
        {
            _collectionManagementRepository = collectionManagementRepository;
        }

        public async Task<CollectionManagementResponseDto?> GetByRequest(Guid idRequest)
        {
            var management = await _collectionManagementRepository.GetByRequest(idRequest);
            return management == null ? null : MapToDto(management);
        }

        // ── Mapeo ───────────────────────────────────────────────────────
        // ManagerName sale del gestor que el repositorio trae con Include. Si no
        // viniera cargado queda vacío en vez de reventar.
        private static CollectionManagementResponseDto MapToDto(CollectionManagement m) => new()
        {
            IdManagement = m.IdManagement,
            IdRequest = m.IdRequest,
            ManagerName = m.Manager != null ? $"{m.Manager.Name} {m.Manager.LastName}" : string.Empty,
            Status = m.Status,
            StatusChangeDate = m.StatusChangeDate,
            ScheduledDate = m.ScheduledDate,
            CompletionDate = m.CompletionDate,
            ManagerObservations = m.ManagerObservations
        };
    }
}
