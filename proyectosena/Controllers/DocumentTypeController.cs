using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.DTOs.User;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

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
        // Anónimo a propósito: el formulario de registro necesita llenar el selector
        // de tipo de documento, y todavía no hay token. Es un catálogo público de tres
        // filas —los tipos de documento que existen en Colombia—, sin dato de nadie.
        [AllowAnonymous]
        [HttpGet("GetDocumentTypes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDocumentTypes()
        {
            // Una lista vacía no es un error: es la respuesta «no hay ninguno».
            // Devolver 404 obligaba al cliente a tratar «no hay» como fallo (BE-15).
            return Ok(await _documentTypeService.GetAll());
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
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDocumentType([FromBody] DocumentTypeDto documentType)
        {
            if (documentType == null)
                return BadRequest("Document type data cannot be null.");

            var (result, updated) = await _documentTypeService.Update(documentType);

            if (result == CatalogMutationResult.NotFound)
                return NotFound("The requested document type was not found.");

            return Ok(updated);
        }

        // -------------------- DELETE: api/documenttype/DeleteDocumentType --------------------
        [HttpDelete("DeleteDocumentType")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteDocumentType(Guid id)
        {
            var result = await _documentTypeService.Delete(id);

            if (result == CatalogMutationResult.NotFound)
                return NotFound("The requested document type was not found.");

            if (result == CatalogMutationResult.InUse)
                return Conflict("This document type is still assigned to at least one user.");

            return Ok("Document type deleted successfully.");
        }
    }
}
