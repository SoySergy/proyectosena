using proyectosena.Models;

namespace proyectosena.Interfaces
{
    public interface IDocumentTypeRepository
    {
        // Obtiene todos los tipos de documento
        Task<List<DocumentType>> GetDocumentTypes();

        // Obtiene un tipo de documento por su ID
        Task<DocumentType> GetDocumentType(Guid idDocumentType);

        // Crea un nuevo tipo de documento
        Task<DocumentType> CreateDocumentType(DocumentType documentType);

        // Actualiza un tipo de documento existente
        Task<DocumentType> UpdateDocumentType(DocumentType documentType);

        // Elimina un tipo de documento por su ID
        Task<bool> DeleteDocumentType(Guid idDocumentType);
    }
}