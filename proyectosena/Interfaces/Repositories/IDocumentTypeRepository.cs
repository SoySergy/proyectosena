using proyectosena.Models;

namespace proyectosena.Interfaces.Repositories
{
    public interface IDocumentTypeRepository
    {
        // Obtiene todos los tipos de documento
        Task<List<DocumentType>> GetDocumentTypes();

        // Obtiene un tipo de documento por su ID
        Task<DocumentType?> GetDocumentType(Guid idDocumentType);

        // Crea un nuevo tipo de documento
        Task<DocumentType> CreateDocumentType(DocumentType documentType);

        // Actualiza un tipo de documento existente. NotFound si el id no
        // existe (WA-06): antes el UPDATE de EF Core sobre una fila
        // inexistente reventaba con DbUpdateConcurrencyException, y eso
        // llegaba como 500.
        Task<(CatalogMutationResult Result, DocumentType? DocumentType)> UpdateDocumentType(DocumentType documentType);

        // Elimina un tipo de documento por su ID. InUse si algún usuario
        // todavía lo tiene (WA-06): antes la clave foránea reventaba como 500.
        Task<CatalogMutationResult> DeleteDocumentType(Guid idDocumentType);
    }
}