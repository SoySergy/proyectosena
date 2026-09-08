using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.Extensions;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CollectionManagementController : ControllerBase
    {
        // El controlador solo traduce HTTP: las reglas viven en el servicio
        private readonly ICollectionManagementService _collectionManagementService;

        public CollectionManagementController(ICollectionManagementService collectionManagementService)
        {
            _collectionManagementService = collectionManagementService;
        }

        // -------------------- GET: api/collectionmanagement/GetByRequest --------------------
        // Quién gestiona una solicitud y desde cuándo. La usa la vista de detalle.
        [HttpGet("GetByRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByRequest(Guid idRequest)
        {
            var (result, management) = await _collectionManagementService
                .GetByRequest(idRequest, User.GetUserId(), User.IsStaff());

            if (result == RequestAccessResult.NotParticipant)
                return StatusCode(StatusCodes.Status403Forbidden,
                    "You can only view the management of your own requests.");

            // Una solicitud pendiente todavía no tiene gestor. Es una respuesta
            // legítima, pero quien pregunta necesita distinguirla de "sí lo tiene".
            if (management == null)
                return NotFound("This request has not been taken by a manager yet.");

            return Ok(management);
        }
    }
}
