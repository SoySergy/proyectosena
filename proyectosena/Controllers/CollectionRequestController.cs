using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.DTOs.Requests;
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
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCollectionRequestById(Guid idRequest)
        {
            var request = await _requestService.GetById(idRequest);

            if (request == null)
                return NotFound("The requested collection request was not found.");

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

            if (dto.IdUser == Guid.Empty)
                return BadRequest("The request must have a valid IdUser.");

            return Ok(await _requestService.Create(dto));
        }

        // -------------------- PUT: api/collectionrequest/UpdateCollectionRequest --------------------
        // Solo el ciudadano, y solo mientras la solicitud siga en Pending
        [HttpPut("UpdateCollectionRequest")]
        [Authorize(Policy = "CitizenOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCollectionRequest([FromBody] UpdateCollectionRequestDto dto)
        {
            if (dto == null)
                return BadRequest("Collection request data cannot be null.");

            if (dto.IdRequest == Guid.Empty)
                return BadRequest("IdRequest is required to update a record.");

            var (result, request) = await _requestService.Update(dto);

            if (result == RequestUpdateResult.RequestNotFound)
                return NotFound("Collection request not found.");

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
            Guid idManager,
            string? comment = null)
        {
            var result = await _requestService.UpdateStatus(idRequest, newStatus, idManager, comment);

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
        public async Task<IActionResult> AcceptRequest(Guid idRequest, Guid idManager)
        {
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
        public async Task<IActionResult> CancelRequest(Guid idRequest, Guid idUser, string? reason = null)
        {
            var result = await _requestService.Cancel(idRequest, idUser, reason);

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
        public async Task<IActionResult> GetMyAssignments(Guid idManager, int page = 1, int pageSize = 20)
        {
            return Ok(await _requestService.GetByManager(idManager, page, pageSize));
        }

        // -------------------- GET: api/collectionrequest/GetRequestsByUser --------------------
        // El ciudadano consulta sus propias solicitudes
        [HttpGet("GetRequestsByUser")]
        [Authorize(Policy = "CitizenOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRequestsByUser(Guid idUser, int page = 1, int pageSize = 20)
        {
            return Ok(await _requestService.GetByUser(idUser, page, pageSize));
        }
    }
}
