using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Models;
using proyectosena.Interfaces;

namespace proyectosena.Repositorios
{
    public class CollectionManagementRepository : ICollectionManagementRepository
    {
        // Contexto de la base de datos
        private readonly RecyRouteDbContext _context;

        // Constructor que recibe el contexto por inyección de dependencias
        public CollectionManagementRepository(RecyRouteDbContext context)
        {
            _context = context;
        }

        // Obtiene todas las gestiones de recolección incluyendo solicitud y gestor
        public async Task<List<CollectionManagement>> GetCollectionManagements()
        {
            return await _context.CollectionManagements
                .Include(g => g.CollectionRequest)
                .Include(g => g.Manager)
                .ToListAsync();
        }

        // Obtiene una gestión específica por ID incluyendo solicitud y gestor
        public async Task<CollectionManagement> GetCollectionManagement(Guid idManagement)
        {
            return await _context.CollectionManagements
                .Include(g => g.CollectionRequest)
                .Include(g => g.Manager)
                .FirstOrDefaultAsync(g => g.IdManagement == idManagement);
        }

        // Crea una nueva gestión de recolección y guarda los cambios en la base de datos
        public async Task<CollectionManagement> CreateCollectionManagement(CollectionManagement collectionManagement)
        {
            _context.CollectionManagements.Add(collectionManagement);
            await _context.SaveChangesAsync();
            return collectionManagement;
        }

        // Actualiza una gestión existente y guarda los cambios en la base de datos
        public async Task<CollectionManagement> UpdateCollectionManagement(CollectionManagement collectionManagement)
        {
            _context.CollectionManagements.Update(collectionManagement);
            await _context.SaveChangesAsync();
            return collectionManagement;
        }

        // Elimina una gestión de recolección por su ID
        // Retorna false si no existe
        public async Task<bool> DeleteCollectionManagement(Guid idManagement)
        {
            var management = await _context.CollectionManagements
                .FirstOrDefaultAsync(g => g.IdManagement == idManagement);
            if (management == null)
                return false;

            _context.CollectionManagements.Remove(management);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}