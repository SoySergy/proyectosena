using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Interfaces;
using proyectosena.Models;

namespace proyectosena.Repositorios
{
    public class DocumentTypeRepository : IDocumentTypeRepository
    {
        // Contexto de la base de datos
        private readonly RecyRouteDbContext _context;

        // Constructor que recibe el contexto por inyección de dependencias
        public DocumentTypeRepository(RecyRouteDbContext context)
        {
            _context = context;
        }

        // Obtiene todos los tipos de documento registrados
        public async Task<List<DocumentType>> GetDocumentTypes()
        {
            return await _context.DocumentTypes.ToListAsync();
        }

        // Obtiene un tipo de documento específico por su ID
        public async Task<DocumentType> GetDocumentType(Guid idDocumentType)
        {
            return await _context.DocumentTypes
                                 .FirstOrDefaultAsync(t => t.IdDocumentType == idDocumentType);
        }

        // Crea un nuevo tipo de documento y guarda los cambios en la base de datos
        public async Task<DocumentType> CreateDocumentType(DocumentType documentType)
        {
            _context.DocumentTypes.Add(documentType);
            await _context.SaveChangesAsync();
            return documentType;
        }

        // Actualiza un tipo de documento existente y guarda los cambios en la base de datos
        public async Task<DocumentType> UpdateDocumentType(DocumentType documentType)
        {
            _context.DocumentTypes.Update(documentType);
            await _context.SaveChangesAsync();
            return documentType;
        }

        // Elimina un tipo de documento por su ID, retorna false si no existe
        public async Task<bool> DeleteDocumentType(Guid idDocumentType)
        {
            var documentType = await _context.DocumentTypes
                                             .FirstOrDefaultAsync(t => t.IdDocumentType == idDocumentType);
            if (documentType == null)
                return false;

            _context.DocumentTypes.Remove(documentType);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}