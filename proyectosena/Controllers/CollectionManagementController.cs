using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.DTOs.Collection;
using proyectosena.Interfaces.Repositories;
using proyectosena.Models;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CollectionManagementController : ControllerBase
    {
        private readonly ICollectionManagementRepository _collectionManagementRepository;

        public CollectionManagementController(ICollectionManagementRepository collectionManagementRepository)
        {
            _collectionManagementRepository = collectionManagementRepository;
        }

        // -------------------- GET: api/collectionmanagement/GetByRequest --------------------
        // Quién gestiona una solicitud y desde cuándo. La usa la vista de detalle.
        [HttpGet("GetByRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByRequest(Guid idRequest)
        {
            var management = await _collectionManagementRepository.GetByRequest(idRequest);

            // Una solicitud pendiente todavía no tiene gestor. Es una respuesta
            // legítima, pero quien pregunta necesita distinguirla de "sí lo tiene".
            if (management == null)
                return NotFound("This request has not been taken by a manager yet.");

            return Ok(MapToResponseDto(management));
        }

        // ── Mapeo privado ───────────────────────────────────────────────
        private static CollectionManagementResponseDto MapToResponseDto(CollectionManagement m) => new()
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
