using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.Models;
using proyectosena.Interfaces;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CollectionManagementController : ControllerBase
    {
        // Repositorio de gestiones de recolección inyectado por dependencias
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

                // Verifica si la lista está vacía o nula
                if (managements == null || !managements.Any())
                    return NotFound("No registered collection managements were found.");

                return Ok(managements);
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

                // Incluye los datos de CollectionRequest y Manager gracias al Include() del repositorio
                return Ok(management);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the collection management.");
            }
        }

        // -------------------- POST: api/collectionmanagement/CreateCollectionManagement --------------------
        [HttpPost("CreateCollectionManagement")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateCollectionManagement([FromBody] CollectionManagement collectionManagement)
        {
            try
            {
                if (collectionManagement == null)
                    return BadRequest("Collection management data cannot be null.");

                // Valida que las claves foráneas sean válidas
                if (collectionManagement.IdRequest == Guid.Empty)
                    return BadRequest("The management must have a valid IdRequest.");

                if (collectionManagement.IdManager == Guid.Empty)
                    return BadRequest("The management must have a valid IdManager.");

                var newManagement = await _collectionManagementRepository.CreateCollectionManagement(collectionManagement);
                return Ok(newManagement);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating the collection management.");
            }
        }

        // -------------------- PUT: api/collectionmanagement/UpdateCollectionManagement --------------------
        [HttpPut("UpdateCollectionManagement")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateCollectionManagement([FromBody] CollectionManagement collectionManagement)
        {
            try
            {
                if (collectionManagement == null)
                    return BadRequest("Collection management data cannot be null.");

                // El IdManagement es obligatorio para identificar el registro a actualizar
                if (collectionManagement.IdManagement == Guid.Empty)
                    return BadRequest("IdManagement is required to update a record.");

                var updated = await _collectionManagementRepository.UpdateCollectionManagement(collectionManagement);
                return Ok(updated);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating the collection management.");
            }
        }

        // -------------------- DELETE: api/collectionmanagement/DeleteCollectionManagement --------------------
        [HttpDelete("DeleteCollectionManagement")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCollectionManagement(Guid idManagement)
        {
            try
            {
                var deleted = await _collectionManagementRepository.DeleteCollectionManagement(idManagement);

                // Retorna false si la gestión no existe en la base de datos
                if (!deleted)
                    return BadRequest("Could not delete the collection management. Please verify it exists.");

                return Ok("Collection management deleted successfully.");
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting the collection management.");
            }
        }
    }
}