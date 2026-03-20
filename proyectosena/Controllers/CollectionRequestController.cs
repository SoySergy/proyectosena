using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.Models;
using proyectosena.Interfaces;

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

        public CollectionRequestController(
            ICollectionRequestRepository collectionRequestRepository,
            ICollectionStatusService collectionStatusService)
        {
            _collectionRequestRepository = collectionRequestRepository;
            _collectionStatusService = collectionStatusService;
        }

        // -------------------- GET: api/collectionrequest/GetCollectionRequests --------------------
        [HttpGet("GetCollectionRequests")]
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

                return Ok(requests);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving collection requests.");
            }
        }

        // -------------------- GET: api/collectionrequest/GetCollectionRequestById --------------------
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
                return Ok(request);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the collection request.");
            }
        }

        // -------------------- POST: api/collectionrequest/CreateCollectionRequest --------------------
        [HttpPost("CreateCollectionRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateCollectionRequest([FromBody] CollectionRequest collectionRequest)
        {
            try
            {
                if (collectionRequest == null)
                    return BadRequest("Collection request data cannot be null.");

                // Validación simple de clave foránea
                if (collectionRequest.IdUser == Guid.Empty)
                    return BadRequest("The request must have a valid IdUser.");

                var newRequest = await _collectionRequestRepository.CreateCollectionRequest(collectionRequest);
                return Ok(newRequest);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating the collection request.");
            }
        }

        // -------------------- PUT: api/collectionrequest/UpdateCollectionRequest --------------------
        [HttpPut("UpdateCollectionRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateCollectionRequest([FromBody] CollectionRequest collectionRequest)
        {
            try
            {
                if (collectionRequest == null)
                    return BadRequest("Collection request data cannot be null.");

                // El IdRequest es obligatorio para identificar el registro a actualizar
                if (collectionRequest.IdRequest == Guid.Empty)
                    return BadRequest("IdRequest is required to update a record.");

                var updated = await _collectionRequestRepository.UpdateCollectionRequest(collectionRequest);
                return Ok(updated);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating the collection request.");
            }
        }

        // -------------------- DELETE: api/collectionrequest/DeleteCollectionRequest --------------------
        [HttpDelete("DeleteCollectionRequest")]
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

                // El servicio se encarga de actualizar estado, historial y notificación
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
    }
}