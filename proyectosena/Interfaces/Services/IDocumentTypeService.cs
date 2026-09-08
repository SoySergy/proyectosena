using proyectosena.DTOs.User;

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

        Task<DocumentTypeDto> Update(DocumentTypeDto dto);

        // False si no existe
        Task<bool> Delete(Guid idDocumentType);
    }
}
