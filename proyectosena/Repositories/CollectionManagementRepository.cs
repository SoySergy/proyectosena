using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Models;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;

namespace proyectosena.Repositories
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

        // Gestión vigente de una solicitud. Si fue reasignada hay varias filas
        // históricas, así que se toma la más reciente.
        public async Task<CollectionManagement?> GetByRequest(Guid idRequest)
        {
            return await _context.CollectionManagements
                .Include(g => g.Manager)
                .Where(g => g.IdRequest == idRequest)
                .OrderByDescending(g => g.StatusChangeDate)
                .FirstOrDefaultAsync();
        }

        // Actualiza una gestión existente y guarda los cambios en la base de datos
        public async Task<CollectionManagement> UpdateCollectionManagement(CollectionManagement collectionManagement)
        {
            _context.CollectionManagements.Update(collectionManagement);
            await _context.SaveChangesAsync();
            return collectionManagement;
        }
    }
}