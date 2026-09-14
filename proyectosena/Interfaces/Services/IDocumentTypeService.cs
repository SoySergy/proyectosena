using proyectosena.DTOs.User;
using proyectosena.Models;

namespace proyectosena.Interfaces.Services
{
    /// <summary>
    /// Administración del catálogo de tipos de documento.
    /// </summary>
    /// <remarks>
    /// Devuelve DTOs, no la entidad: así el catálogo puede crecer con columnas
    /// internas sin que se filtren por la API.
    /// </remarks>
    public interface IDocumentTypeService
    {
        Task<List<DocumentTypeDto>> GetAll();

        // Null si no existe
        Task<DocumentTypeDto?> GetById(Guid idDocumentType);

        Task<DocumentTypeDto> Create(DocumentTypeDto dto);

        // NotFound si el id no existe (WA-06).
        Task<(CatalogMutationResult Result, DocumentTypeDto? DocumentType)> Update(DocumentTypeDto dto);

        // NotFound si no existe. InUse si algún usuario todavía lo tiene (WA-06).
        Task<CatalogMutationResult> Delete(Guid idDocumentType);
    }
}
