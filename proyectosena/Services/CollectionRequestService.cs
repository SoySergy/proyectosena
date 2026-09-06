using proyectosena.DTOs.Common;
using proyectosena.DTOs.Requests;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Services
{
    public class CollectionRequestService : ICollectionRequestService
    {
        private readonly ICollectionRequestRepository _requestRepository;

        // Maneja el bloqueo pesimista al aceptar y el aviso a los gestores
        private readonly IAssignmentService _assignmentService;

        // Cambia el estado, escribe el historial y notifica al ciudadano
        private readonly ICollectionStatusService _statusService;

        public CollectionRequestService(
            ICollectionRequestRepository requestRepository,
            IAssignmentService assignmentService,
            ICollectionStatusService statusService)
        {
            _requestRepository = requestRepository;
            _assignmentService = assignmentService;
            _statusService = statusService;
        }

        // ── Consultas ───────────────────────────────────────────────────

        public async Task<PagedResult<CollectionRequestResponseDto>> GetAll(int page, int pageSize)
        {
            (page, pageSize) = PagedResult<CollectionRequestResponseDto>.Normalize(page, pageSize);
            var (items, total) = await _requestRepository.GetCollectionRequests(page, pageSize);
            return Paged(items, page, pageSize, total);
        }

        public async Task<PagedResult<CollectionRequestResponseDto>> GetPending(int page, int pageSize)
        {
            (page, pageSize) = PagedResult<CollectionRequestResponseDto>.Normalize(page, pageSize);
            var (items, total) = await _requestRepository.GetPendingRequests(page, pageSize);
            return Paged(items, page, pageSize, total);
        }

        public async Task<PagedResult<CollectionRequestResponseDto>> GetByManager(Guid idManager, int page, int pageSize)
        {
            (page, pageSize) = PagedResult<CollectionRequestResponseDto>.Normalize(page, pageSize);
            var (items, total) = await _requestRepository.GetRequestsByManager(idManager, page, pageSize);
            return Paged(items, page, pageSize, total);
        }

        public async Task<PagedResult<CollectionRequestResponseDto>> GetByUser(Guid idUser, int page, int pageSize)
        {
            (page, pageSize) = PagedResult<CollectionRequestResponseDto>.Normalize(page, pageSize);
            var (items, total) = await _requestRepository.GetRequestsByUser(idUser, page, pageSize);
            return Paged(items, page, pageSize, total);
        }

        public async Task<CollectionRequestResponseDto?> GetById(Guid idRequest)
        {
            var request = await _requestRepository.GetCollectionRequest(idRequest);
            return request == null ? null : MapToDto(request);
        }

        // ── Escritura ───────────────────────────────────────────────────

        public async Task<CollectionRequestResponseDto> Create(CreateCollectionRequestDto dto)
        {
            var request = new CollectionRequest
            {
                IdUser = dto.IdUser,
                CollectionDate = dto.CollectionDate,
                CollectionTime = dto.CollectionTime,
                CollectionAddress = dto.CollectionAddress,
                ContactPhone = dto.ContactPhone,
                WasteTypes = dto.WasteTypes,
                CitizenObservations = dto.CitizenObservations,

                // Una solicitud siempre nace en Pending; el cliente no decide el estado
                CurrentStatus = CollectionRequestStatus.Pending,
                RequestDate = DateTime.UtcNow
            };

            var created = await _requestRepository.CreateCollectionRequest(request);

            // Modelo tipo Uber: todos los gestores ven la solicitud y el primero la toma
            await _assignmentService.NotifyAllManagersAsync(
                created.IdRequest, created.CollectionAddress);

            return MapToDto(created);
        }

        public async Task<(RequestUpdateResult Result, CollectionRequestResponseDto? Request)> Update(
            UpdateCollectionRequestDto dto)
        {
            var existing = await _requestRepository.GetCollectionRequest(dto.IdRequest);
            if (existing == null)
                return (RequestUpdateResult.RequestNotFound, null);

            // Una vez que un gestor la tomó, los datos ya no se pueden cambiar:
            // él ya organizó su ruta con la dirección y la hora originales.
            if (existing.CurrentStatus != CollectionRequestStatus.Pending)
                return (RequestUpdateResult.NotPending, null);

            // Solo se tocan los campos que vengan con valor
            if (dto.CollectionDate.HasValue) existing.CollectionDate = dto.CollectionDate.Value;
            if (dto.CollectionTime != null) existing.CollectionTime = dto.CollectionTime;
            if (dto.CollectionAddress != null) existing.CollectionAddress = dto.CollectionAddress;
            if (dto.ContactPhone != null) existing.ContactPhone = dto.ContactPhone;
            if (dto.WasteTypes != null) existing.WasteTypes = dto.WasteTypes;
            if (dto.CitizenObservations != null) existing.CitizenObservations = dto.CitizenObservations;

            var updated = await _requestRepository.UpdateCollectionRequest(existing);

            return (RequestUpdateResult.Success, MapToDto(updated));
        }

        public async Task<StatusUpdateResult> UpdateStatus(
            Guid idRequest, string newStatus, Guid idManager, string? comment)
        {
            // Primero: ¿ese estado existe siquiera?
            if (!CollectionRequestStatus.ValidStatuses.Contains(newStatus))
                return StatusUpdateResult.InvalidStatus;

            // Después: ¿se puede llegar a él desde el actual? Eso lo decide la
            // máquina de estados dentro de UpdateStatusAsync.
            return await _statusService.UpdateStatusAsync(idRequest, newStatus, idManager, comment);
        }

        public Task<(bool Success, string Message)> Accept(Guid idRequest, Guid idManager)
            => _assignmentService.AcceptRequestAsync(idRequest, idManager);

        public async Task<RequestCancelResult> Cancel(Guid idRequest, Guid idUser, string? reason)
        {
            var request = await _requestRepository.GetCollectionRequest(idRequest);
            if (request == null)
                return RequestCancelResult.RequestNotFound;

            // Solo el dueño cancela lo suyo. Cuando SEC-03 lea la identidad del
            // token en vez del parámetro, esta comprobación pasa a ser efectiva.
            if (request.IdUser != idUser)
                return RequestCancelResult.NotOwner;

            // Cancelled solo es alcanzable desde Pending: lo decide la máquina de estados
            var result = await _statusService.UpdateStatusAsync(
                idRequest, CollectionRequestStatus.Cancelled, idUser, reason);

            return result switch
            {
                StatusUpdateResult.Success => RequestCancelResult.Success,
                StatusUpdateResult.RequestNotFound => RequestCancelResult.RequestNotFound,
                _ => RequestCancelResult.NotCancellable
            };
        }

        // ── Privados ────────────────────────────────────────────────────

        private static PagedResult<CollectionRequestResponseDto> Paged(
            List<CollectionRequest> items, int page, int pageSize, int total)
            => PagedResult<CollectionRequestResponseDto>.Create(
                items.Select(MapToDto).ToList(), page, pageSize, total);

        // Aplana el ciudadano: el cliente recibe su nombre, nunca la entidad User
        private static CollectionRequestResponseDto MapToDto(CollectionRequest r) => new()
        {
            IdRequest = r.IdRequest,
            IdUser = r.IdUser,
            CitizenName = r.User?.Name ?? string.Empty,
            CitizenLastName = r.User?.LastName ?? string.Empty,
            CollectionDate = r.CollectionDate,
            CollectionTime = r.CollectionTime,
            CollectionAddress = r.CollectionAddress,
            ContactPhone = r.ContactPhone,
            CurrentStatus = r.CurrentStatus,
            RequestDate = r.RequestDate,
            WasteTypes = r.WasteTypes,
            CitizenObservations = r.CitizenObservations
        };
    }
}
