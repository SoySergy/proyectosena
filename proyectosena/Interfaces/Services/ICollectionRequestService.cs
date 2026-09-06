using proyectosena.DTOs.Common;
using proyectosena.DTOs.Requests;
using proyectosena.Models;

namespace proyectosena.Interfaces.Services
{
    /// <summary>
    /// Reglas de negocio de las solicitudes de recolección: creación, edición,
    /// cancelación y cambios de estado. Coordina con IAssignmentService para la
    /// asignación entre gestores y con ICollectionStatusService para el historial
    /// y las notificaciones. Devuelve siempre DTOs.
    /// </summary>
    public interface ICollectionRequestService
    {
        // ── Consultas paginadas ─────────────────────────────────────────
        Task<PagedResult<CollectionRequestResponseDto>> GetAll(int page, int pageSize);
        Task<PagedResult<CollectionRequestResponseDto>> GetPending(int page, int pageSize);
        Task<PagedResult<CollectionRequestResponseDto>> GetByManager(Guid idManager, int page, int pageSize);
        Task<PagedResult<CollectionRequestResponseDto>> GetByUser(Guid idUser, int page, int pageSize);

        // Una solicitud por su id. Null si no existe.
        Task<CollectionRequestResponseDto?> GetById(Guid idRequest);

        // Crea la solicitud en estado Pending y avisa a todos los gestores activos
        Task<CollectionRequestResponseDto> Create(CreateCollectionRequestDto dto);

        // Edita una solicitud. Solo mientras siga en Pending.
        Task<(RequestUpdateResult Result, CollectionRequestResponseDto? Request)> Update(UpdateCollectionRequestDto dto);

        // Cambia el estado validando primero que el valor exista y luego que la
        // transición sea legal según la máquina de estados.
        Task<StatusUpdateResult> UpdateStatus(Guid idRequest, string newStatus, Guid idManager, string? comment);

        // Un gestor toma una solicitud pendiente. La concurrencia la resuelve
        // IAssignmentService con un bloqueo pesimista.
        Task<(bool Success, string Message)> Accept(Guid idRequest, Guid idManager);

        // El ciudadano cancela su propia solicitud, solo mientras siga en Pending
        Task<RequestCancelResult> Cancel(Guid idRequest, Guid idUser, string? reason);
    }
}
