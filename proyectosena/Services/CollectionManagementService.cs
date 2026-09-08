using proyectosena.DTOs.Collection;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Services
{
    public class CollectionManagementService : ICollectionManagementService
    {
        private readonly ICollectionManagementRepository _collectionManagementRepository;

        // Necesario para saber de quién es cada solicitud
        private readonly ICollectionRequestRepository _requestRepository;

        public CollectionManagementService(
            ICollectionManagementRepository collectionManagementRepository,
            ICollectionRequestRepository requestRepository)
        {
            _collectionManagementRepository = collectionManagementRepository;
            _requestRepository = requestRepository;
        }

        public async Task<(RequestAccessResult Result, CollectionManagementResponseDto? Management)> GetByRequest(
            Guid idRequest, Guid idUser, bool esPersonal)
        {
            // Faltaba: con solo el id de la solicitud, cualquiera con token veía quién
            // atiende la solicitud de otro, con su nombre y sus fechas.
            if (!esPersonal && !await _requestRepository.IsParticipant(idRequest, idUser))
                return (RequestAccessResult.NotParticipant, null);

            var management = await _collectionManagementRepository.GetByRequest(idRequest);

            return (RequestAccessResult.Success, management == null ? null : MapToDto(management));
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
