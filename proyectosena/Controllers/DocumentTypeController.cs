using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.Models;
using proyectosena.Interfaces;

namespace proyectosena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentTypeController : ControllerBase
    {
        // Repositorio de tipos de documento inyectado por dependencias
        private readonly IDocumentTypeRepository _documentTypeRepository;

        public DocumentTypeController(IDocumentTypeRepository documentTypeRepository)
        {
            _documentTypeRepository = documentTypeRepository;
        }

        // -------------------- GET: api/documenttype/GetDocumentTypes --------------------
        [HttpGet("GetDocumentTypes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDocumentTypes()
        {
            try
            {
                var types = await _documentTypeRepository.GetDocumentTypes();

                // Verifica si la lista está vacía o nula
                if (types == null || !types.Any())
                    return NotFound("No registered document types were found.");

                return Ok(types);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving document types.");
            }
        }

        // -------------------- GET: api/documenttype/GetDocumentTypeById --------------------
        [HttpGet("GetDocumentTypeById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDocumentTypeById(Guid id)
        {
            try
            {
                var type = await _documentTypeRepository.GetDocumentType(id);

                if (type == null)
                    return NotFound("The requested document type was not found.");

                return Ok(type);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the document type.");
            }
        }

        // -------------------- POST: api/documenttype/CreateDocumentType --------------------
        [HttpPost("CreateDocumentType")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateDocumentType([FromBody] DocumentType documentType)
        {
            try
            {
                if (documentType == null)
                    return BadRequest("Document type data cannot be null.");

                var newType = await _documentTypeRepository.CreateDocumentType(documentType);
                return Ok(newType);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating the document type.");
            }
        }

        // -------------------- PUT: api/documenttype/UpdateDocumentType --------------------
        [HttpPut("UpdateDocumentType")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateDocumentType([FromBody] DocumentType documentType)
        {
            try
            {
                if (documentType == null)
                    return BadRequest("Document type data cannot be null.");

                var updated = await _documentTypeRepository.UpdateDocumentType(documentType);
                return Ok(updated);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating the document type.");
            }
        }

        // -------------------- DELETE: api/documenttype/DeleteDocumentType --------------------
        [HttpDelete("DeleteDocumentType")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteDocumentType(Guid id)
        {
            try
            {
                var deleted = await _documentTypeRepository.DeleteDocumentType(id);

                // Retorna false si el tipo de documento no existe en la base de datos
                if (!deleted)
                    return BadRequest("Could not delete the document type. Please verify it exists.");

                return Ok("Document type deleted successfully.");
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting the document type.");
            }
        }
    }
}