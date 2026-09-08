using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Interfaces.Repositories;
using proyectosena.Models;

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
    }
}
