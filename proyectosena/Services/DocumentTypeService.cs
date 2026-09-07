using proyectosena.DTOs.User;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Services
{
    public class DocumentTypeService : IDocumentTypeService
    {
        private readonly IDocumentTypeRepository _documentTypeRepository;

        public DocumentTypeService(IDocumentTypeRepository documentTypeRepository)
        {
            _documentTypeRepository = documentTypeRepository;
        }

        public async Task<List<DocumentTypeDto>> GetAll()
        {
            var types = await _documentTypeRepository.GetDocumentTypes();
            return types.Select(MapToDto).ToList();
        }

        public async Task<DocumentTypeDto?> GetById(Guid idDocumentType)
        {
            var type = await _documentTypeRepository.GetDocumentType(idDocumentType);
            return type == null ? null : MapToDto(type);
        }

        public async Task<DocumentTypeDto> Create(DocumentTypeDto dto)
        {
            var created = await _documentTypeRepository.CreateDocumentType(MapToEntity(dto));
            return MapToDto(created);
        }

        public async Task<DocumentTypeDto> Update(DocumentTypeDto dto)
        {
            var updated = await _documentTypeRepository.UpdateDocumentType(MapToEntity(dto));
            return MapToDto(updated);
        }

        public Task<bool> Delete(Guid idDocumentType)
            => _documentTypeRepository.DeleteDocumentType(idDocumentType);

        // ── Mapeo ───────────────────────────────────────────────────────

        private static DocumentTypeDto MapToDto(DocumentType t) => new()
        {
            IdDocumentType = t.IdDocumentType,
            DocumentName = t.DocumentName,
            Abbreviation = t.Abbreviation
        };

        private static DocumentType MapToEntity(DocumentTypeDto d) => new()
        {
            IdDocumentType = d.IdDocumentType,
            DocumentName = d.DocumentName,
            Abbreviation = d.Abbreviation
        };
    }
}
