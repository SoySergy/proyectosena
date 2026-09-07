using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.DTOs.User;
using proyectosena.Interfaces.Services;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentTypeController : ControllerBase
    {
        // El controlador solo traduce HTTP: las reglas viven en el servicio
        private readonly IDocumentTypeService _documentTypeService;

        public DocumentTypeController(IDocumentTypeService documentTypeService)
        {
            _documentTypeService = documentTypeService;
        }

        // -------------------- GET: api/documenttype/GetDocumentTypes --------------------
        [HttpGet("GetDocumentTypes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDocumentTypes()
        {
            var types = await _documentTypeService.GetAll();

            if (!types.Any())
                return NotFound("No registered document types were found.");

            return Ok(types);
        }

        // -------------------- GET: api/documenttype/GetDocumentTypeById --------------------
        [HttpGet("GetDocumentTypeById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDocumentTypeById(Guid id)
        {
            var type = await _documentTypeService.GetById(id);

            if (type == null)
                return NotFound("The requested document type was not found.");

            return Ok(type);
        }

        // -------------------- POST: api/documenttype/CreateDocumentType --------------------
        [HttpPost("CreateDocumentType")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateDocumentType([FromBody] DocumentTypeDto documentType)
        {
            if (documentType == null)
                return BadRequest("Document type data cannot be null.");

            return Ok(await _documentTypeService.Create(documentType));
        }

        // -------------------- PUT: api/documenttype/UpdateDocumentType --------------------
        [HttpPut("UpdateDocumentType")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateDocumentType([FromBody] DocumentTypeDto documentType)
        {
            if (documentType == null)
                return BadRequest("Document type data cannot be null.");

            return Ok(await _documentTypeService.Update(documentType));
        }

        // -------------------- DELETE: api/documenttype/DeleteDocumentType --------------------
        [HttpDelete("DeleteDocumentType")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteDocumentType(Guid id)
        {
            var deleted = await _documentTypeService.Delete(id);

            // Retorna false si el tipo de documento no existe en la base de datos
            if (!deleted)
                return BadRequest("Could not delete the document type. Please verify it exists.");

            return Ok("Document type deleted successfully.");
        }
    }
}
