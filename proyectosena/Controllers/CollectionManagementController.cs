using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.Models;
using proyectosena.Interfaces;
using proyectosena.DTOs.Collection;

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

        // -------------------- GET: api/collectionmanagement/GetCollectionManagements --------------------
        [HttpGet("GetCollectionManagements")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCollectionManagements()
        {
            try
            {
                var managements = await _collectionManagementRepository.GetCollectionManagements();

                if (managements == null || !managements.Any())
                    return NotFound("No registered collection managements were found.");

                // ✅ Mapea al DTO en lugar de retornar el modelo crudo
                return Ok(managements.Select(MapToResponseDto).ToList());
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving collection managements.");
            }
        }

        // -------------------- GET: api/collectionmanagement/GetCollectionManagementById --------------------
        [HttpGet("GetCollectionManagementById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCollectionManagementById(Guid idManagement)
        {
            try
            {
                var management = await _collectionManagementRepository.GetCollectionManagement(idManagement);

                if (management == null)
                    return NotFound("The requested collection management was not found.");

                // ✅ Mapea al DTO en lugar de retornar el modelo crudo
                return Ok(MapToResponseDto(management));
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the collection management.");
            }
        }

        // -------------------- PUT: api/collectionmanagement/UpdateCollectionManagement --------------------
        [HttpPut("UpdateCollectionManagement")]
        [Authorize(Policy = "AdminOrManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateCollectionManagement(Guid idManagement, [FromBody] UpdateCollectionManagementDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("Management data cannot be null.");

                if (idManagement == Guid.Empty)
                    return BadRequest("IdManagement is required to update a record.");

                // 1. Busca el registro existente en BD
                var existing = await _collectionManagementRepository.GetCollectionManagement(idManagement);
                if (existing == null)
                    return NotFound("Collection management not found.");

                // 2. Aplica solo los campos que el gestor puede modificar
                if (dto.Status != null) existing.Status = dto.Status;
                if (dto.ScheduledDate.HasValue) existing.ScheduledDate = dto.ScheduledDate;
                if (dto.CompletionDate.HasValue) existing.CompletionDate = dto.CompletionDate;
                if (dto.ManagerObservations != null) existing.ManagerObservations = dto.ManagerObservations;

                // 3. Actualiza la fecha de cambio de estado automáticamente
                existing.StatusChangeDate = DateTime.UtcNow;

                var updated = await _collectionManagementRepository.UpdateCollectionManagement(existing);
                return Ok(MapToResponseDto(updated));
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating the collection management.");
            }
        }

        // ── Método privado de mapeo ─────────────────────────────────────
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