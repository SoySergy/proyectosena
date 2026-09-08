using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.DTOs.Requests;
using proyectosena.Extensions;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CollectionRequestController : ControllerBase
    {
        // El controlador solo traduce HTTP: las reglas viven en el servicio
        private readonly ICollectionRequestService _requestService;

        public CollectionRequestController(ICollectionRequestService requestService)
        {
            _requestService = requestService;
        }

        // -------------------- GET: api/collectionrequest/GetCollectionRequests --------------------
        // Todas las solicitudes — Admin y Manager pueden verlas
        [HttpGet("GetCollectionRequests")]
        [Authorize(Policy = "AdminOrManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCollectionRequests(int page = 1, int pageSize = 20)
        {
            return Ok(await _requestService.GetAll(page, pageSize));
        }

        // -------------------- GET: api/collectionrequest/GetCollectionRequestById --------------------
        [HttpGet("GetCollectionRequestById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCollectionRequestById(Guid idRequest)
        {
            var request = await _requestService.GetById(idRequest);

            if (request == null)
                return NotFound("The requested collection request was not found.");

            // Faltaba: cualquier ciudadano podía leer la solicitud de otro —con su
            // dirección y teléfono— solo con el id. Gestores y administradores sí
            // ven todas: es su trabajo.
            if (!User.IsStaff() && request.IdUser != User.GetUserId())
                return StatusCode(StatusCodes.Status403Forbidden,
                    "You can only view your own collection requests.");

            return Ok(request);
        }

        // -------------------- POST: api/collectionrequest/CreateCollectionRequest --------------------
        // Solo el ciudadano puede crear solicitudes
        [HttpPost("CreateCollectionRequest")]
        [Authorize(Policy = "CitizenOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCollectionRequest([FromBody] CreateCollectionRequestDto dto)
        {
            if (dto == null)
                return BadRequest("Collection request data cannot be null.");

            // El dueño es quien llama. Ya no hace falta validar un IdUser del cuerpo:
            // el token lo garantiza.
            return Ok(await _requestService.Create(dto, User.GetUserId()));
        }

        // -------------------- PUT: api/collectionrequest/UpdateCollectionRequest --------------------
        // Solo el ciudadano, y solo mientras la solicitud siga en Pending
        [HttpPut("UpdateCollectionRequest")]
        [Authorize(Policy = "CitizenOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCollectionRequest([FromBody] UpdateCollectionRequestDto dto)
        {
            if (dto == null)
                return BadRequest("Collection request data cannot be null.");

            if (dto.IdRequest == Guid.Empty)
                return BadRequest("IdRequest is required to update a record.");

            var (result, request) = await _requestService.Update(dto, User.GetUserId());

            if (result == RequestUpdateResult.RequestNotFound)
                return NotFound("Collection request not found.");

            if (result == RequestUpdateResult.NotOwner)
                return StatusCode(StatusCodes.Status403Forbidden,
                    "You can only edit your own collection requests.");

            if (result == RequestUpdateResult.NotPending)
                return BadRequest("Only pending requests can be modified.");

            return Ok(request);
        }

        // -------------------- PATCH: api/collectionrequest/UpdateStatus --------------------
        [HttpPatch("UpdateStatus")]
        [Authorize(Policy = "AdminOrManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateStatus(
            Guid idRequest,
            string newStatus,
            string? comment = null)
        {
            // Quién hace el cambio sale del token: antes un gestor podía mover una
            // solicitud firmando el historial con el nombre de otro.
            var result = await _requestService.UpdateStatus(idRequest, newStatus, User.GetUserId(), comment);

            if (result == StatusUpdateResult.InvalidStatus)
                return BadRequest($"Invalid status. Valid values: {string.Join(", ", CollectionRequestStatus.ValidStatuses)}");

            if (result == StatusUpdateResult.RequestNotFound)
                return NotFound("Collection request not found.");

            if (result == StatusUpdateResult.InvalidTransition)
                return Conflict($"Cannot change status to '{newStatus}' from the current state.");

            return Ok(new
            {
                IdRequest = idRequest,
                NewStatus = newStatus,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // -------------------- GET: api/collectionrequest/GetPendingRequests --------------------
        // Las que los gestores pueden tomar
        [HttpGet("GetPendingRequests")]
        [Authorize(Policy = "AdminOrManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingRequests(int page = 1, int pageSize = 20)
        {
            return Ok(await _requestService.GetPending(page, pageSize));
        }

        // -------------------- POST: api/collectionrequest/AcceptRequest --------------------
        // Un gestor toma una solicitud pendiente
        [HttpPost("AcceptRequest")]
        [Authorize(Policy = "AdminOrManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AcceptRequest(Guid idRequest)
        {
            // El gestor que acepta es el del token, no el que diga la URL
            var idManager = User.GetUserId();

            var (success, message) = await _requestService.Accept(idRequest, idManager);

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

        // -------------------- PATCH: api/collectionrequest/CancelRequest --------------------
        // El ciudadano cancela su propia solicitud, solo mientras nadie la haya tomado
        [HttpPatch("CancelRequest")]
        [Authorize(Policy = "CitizenOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CancelRequest(Guid idRequest, string? reason = null)
        {
            // La comprobación de dueño que ya hacía el servicio solo vale si el id
            // viene del token; con el parámetro se podía suplantar al dueño.
            var result = await _requestService.Cancel(idRequest, User.GetUserId(), reason);

            if (result == RequestCancelResult.RequestNotFound)
                return NotFound("Collection request not found.");

            if (result == RequestCancelResult.NotOwner)
                return StatusCode(StatusCodes.Status403Forbidden,
                    "You can only cancel your own collection requests.");

            if (result == RequestCancelResult.NotCancellable)
                return Conflict("Only pending requests can be cancelled.");

            return Ok(new
            {
                Message = "Collection request cancelled.",
                IdRequest = idRequest,
                CancelledAt = DateTime.UtcNow
            });
        }

        // -------------------- GET: api/collectionrequest/GetMyAssignments --------------------
        // Solicitudes que un gestor específico tomó
        [HttpGet("GetMyAssignments")]
        [Authorize(Policy = "AdminOrManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyAssignments(int page = 1, int pageSize = 20)
        {
            // El id del gestor sale del token: antes un gestor podía ver la carga
            // de trabajo de cualquier otro cambiando el parámetro.
            return Ok(await _requestService.GetByManager(User.GetUserId(), page, pageSize));
        }

        // -------------------- GET: api/collectionrequest/GetRequestsByUser --------------------
        // El ciudadano consulta sus propias solicitudes
        [HttpGet("GetRequestsByUser")]
        [Authorize(Policy = "CitizenOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRequestsByUser(int page = 1, int pageSize = 20)
        {
            // El id sale del token: estas solicitudes traen dirección y teléfono.
            return Ok(await _requestService.GetByUser(User.GetUserId(), page, pageSize));
        }
    }
}
