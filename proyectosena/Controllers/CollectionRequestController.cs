using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.DTOs.Requests;
using proyectosena.Interfaces;
using proyectosena.Models;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CollectionRequestController : ControllerBase
    {
        // Repositorio de solicitudes de recolección inyectado por dependencias
        private readonly ICollectionRequestRepository _collectionRequestRepository;

        // Servicio que maneja el cambio de estado, historial y notificación
        private readonly ICollectionStatusService _collectionStatusService;

        // Servicio que maneja la asignación de solicitudes entre gestores (modelo Uber)
        private readonly IAssignmentService _assignmentService;

        // Constructor único con los tres servicios inyectados
        public CollectionRequestController(
            ICollectionRequestRepository collectionRequestRepository,
            ICollectionStatusService collectionStatusService,
            IAssignmentService assignmentService)
        {
            _collectionRequestRepository = collectionRequestRepository;
            _collectionStatusService = collectionStatusService;
            _assignmentService = assignmentService;
        }

        // -------------------- GET: api/collectionrequest/GetCollectionRequests --------------------
        // Retorna todas las solicitudes — Admin y Manager pueden verlas todas
        [HttpGet("GetCollectionRequests")]
        [Authorize(Policy = "AdminOrManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCollectionRequests()
        {
            try
            {
                var requests = await _collectionRequestRepository.GetCollectionRequests();

                // Verifica si la lista está vacía o nula
                if (requests == null || !requests.Any())
                    return NotFound("No registered collection requests were found.");

                // Mapea cada solicitud al DTO de respuesta para no exponer datos internos
                return Ok(requests.Select(MapToResponseDto).ToList());
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving collection requests.");
            }
        }

        // -------------------- GET: api/collectionrequest/GetCollectionRequestById --------------------
        // Cualquier usuario autenticado puede ver el detalle de una solicitud específica
        [HttpGet("GetCollectionRequestById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCollectionRequestById(Guid idRequest)
        {
            try
            {
                var request = await _collectionRequestRepository.GetCollectionRequest(idRequest);

                if (request == null)
                    return NotFound("The requested collection request was not found.");

                // Incluye los datos del User gracias al Include() del repositorio
                return Ok(MapToResponseDto(request));
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the collection request.");
            }
        }

        // -------------------- POST: api/collectionrequest/CreateCollectionRequest --------------------
        // Solo el ciudadano puede crear solicitudes de recolección
        [HttpPost("CreateCollectionRequest")]
        [Authorize(Policy = "CitizenOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateCollectionRequest([FromBody] CreateCollectionRequestDto dto)
        {
            try

            {
                if (dto == null)
                    return BadRequest("Collection request data cannot be null.");

                if (dto.IdUser == Guid.Empty)
                    return BadRequest("The request must have a valid IdUser.");

                // Construye el modelo CollectionRequest desde el DTO
                var collectionRequest = new CollectionRequest
                {
                    IdUser = dto.IdUser,
                    CollectionDate = dto.CollectionDate,
                    CollectionTime = dto.CollectionTime,
                    CollectionAddress = dto.CollectionAddress,
                    ContactPhone = dto.ContactPhone,
                    WasteTypes = dto.WasteTypes,
                    CitizenObservations = dto.CitizenObservations,
                    // El estado siempre inicia en Pending al crear una solicitud
                    CurrentStatus = CollectionRequestStatus.Pending,
                    RequestDate = DateTime.UtcNow
                };

                var newRequest = await _collectionRequestRepository.CreateCollectionRequest(collectionRequest);

                // Notifica a todos los gestores que hay una nueva solicitud disponible
                // Replica el modelo Uber donde todos los conductores ven el viaje
                await _assignmentService.NotifyAllManagersAsync(
                    newRequest.IdRequest,
                    newRequest.CollectionAddress);

                return Ok(MapToResponseDto(newRequest));
            } 
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating the collection request.");
            }
        }

        // -------------------- PUT: api/collectionrequest/UpdateCollectionRequest --------------------
        // Solo el ciudadano puede editar su solicitud y solo si está en Pending
        [HttpPut("UpdateCollectionRequest")]
        [Authorize(Policy = "CitizenOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateCollectionRequest([FromBody] UpdateCollectionRequestDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("Collection request data cannot be null.");

                if (dto.IdRequest == Guid.Empty)
                    return BadRequest("IdRequest is required to update a record.");

                // Busca la solicitud existente en la base de datos
                var existing = await _collectionRequestRepository.GetCollectionRequest(dto.IdRequest);
                if (existing == null)
                    return NotFound("Collection request not found.");

                // Solo se puede editar si está en Pending — una vez asignada ya no se puede modificar
                if (existing.CurrentStatus != CollectionRequestStatus.Pending)
                    return BadRequest("Only pending requests can be modified.");

                // Actualiza solo los campos que vienen en el DTO (campos opcionales)
                if (dto.CollectionDate.HasValue) existing.CollectionDate = dto.CollectionDate.Value;
                if (dto.CollectionTime != null) existing.CollectionTime = dto.CollectionTime;
                if (dto.CollectionAddress != null) existing.CollectionAddress = dto.CollectionAddress;
                if (dto.ContactPhone != null) existing.ContactPhone = dto.ContactPhone;
                if (dto.WasteTypes != null) existing.WasteTypes = dto.WasteTypes;
                if (dto.CitizenObservations != null) existing.CitizenObservations = dto.CitizenObservations;

                var updated = await _collectionRequestRepository.UpdateCollectionRequest(existing);
                return Ok(MapToResponseDto(updated));
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating the collection request.");
            }
        }

        // -------------------- DELETE: api/collectionrequest/DeleteCollectionRequest --------------------
        // Solo Admin puede eliminar solicitudes
        [HttpDelete("DeleteCollectionRequest")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCollectionRequest(Guid idRequest)
        {
            try
            {
                var deleted = await _collectionRequestRepository.DeleteCollectionRequest(idRequest);

                // Retorna false si la solicitud no existe en la base de datos
                if (!deleted)
                    return BadRequest("Could not delete the collection request. Please verify it exists.");

                return Ok("Collection request deleted successfully.");
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting the collection request.");
            }
        }

        // -------------------- PATCH: api/collectionrequest/UpdateStatus --------------------
        // Solo Admin o Manager pueden cambiar el estado de una solicitud
        [HttpPatch("UpdateStatus")]
        [Authorize(Policy = "AdminOrManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateStatus(
            Guid idRequest,
            string newStatus,
            Guid idManager,
            string? comment = null)
        {
            try
            {
                // Valida que el estado sea uno de los valores permitidos en CollectionRequestStatus
                if (!CollectionRequestStatus.ValidStatuses.Contains(newStatus))
                    return BadRequest($"Invalid status. Valid values: {string.Join(", ", CollectionRequestStatus.ValidStatuses)}");

                // El servicio se encarga de actualizar estado, historial y notificación automáticamente
                var result = await _collectionStatusService.UpdateStatusAsync(
                    idRequest, newStatus, idManager, comment);

                if (!result)
                    return NotFound("Collection request not found.");

                return Ok(new
                {
                    IdRequest = idRequest,
                    NewStatus = newStatus,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating the status.");
            }
        }

        // -------------------- GET: api/collectionrequest/GetPendingRequests --------------------
        // Endpoint que usan los gestores para ver todas las solicitudes disponibles para tomar
        [HttpGet("GetPendingRequests")]
        [Authorize(Policy = "AdminOrManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPendingRequests()
        {
            try
            {
                var requests = await _collectionRequestRepository.GetPendingRequests();

                // Si no hay solicitudes pendientes retorna 404 con mensaje descriptivo
                if (requests == null || !requests.Any())
                    return NotFound("No pending collection requests available.");

                return Ok(requests.Select(MapToResponseDto).ToList());
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving pending requests.");
            }
        }

        // -------------------- POST: api/collectionrequest/AcceptRequest --------------------
        // Endpoint que usa el gestor para tomar una solicitud pendiente
        // Usa transacción para garantizar que solo un gestor pueda aceptarla
        [HttpPost("AcceptRequest")]
        [Authorize(Policy = "AdminOrManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AcceptRequest(Guid idRequest, Guid idManager)
        {
            try
            {
                // El servicio maneja la lógica de concurrencia, historial y notificación
                var (success, message) = await _assignmentService.AcceptRequestAsync(idRequest, idManager);

                if (!success)
                    return BadRequest(message);

                return Ok(new
                {
                    Message = message,
                    IdRequest = idRequest,
                    IdManager = idManager,
                    AcceptedAt = DateTime.UtcNow
                });
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error accepting the request.");
            }
        }

        // ── Métodos privados ────────────────────────────────────────────

        // Mapea el modelo CollectionRequest al DTO de respuesta
        // Evita exponer campos innecesarios y aplana las propiedades de navegación
        private static CollectionRequestResponseDto MapToResponseDto(CollectionRequest r) => new()
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