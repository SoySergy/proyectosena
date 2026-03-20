using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Interfaces;
using proyectosena.Models;

namespace proyectosena.Repositorios
{
    public class CollectionRequestRepository : ICollectionRequestRepository
    {
        // Contexto de la base de datos
        private readonly RecyRouteDbContext _context;

        // Constructor que recibe el contexto por inyección de dependencias
        public CollectionRequestRepository(RecyRouteDbContext context)
        {
            _context = context;
        }

        // Obtiene todas las solicitudes de recolección incluyendo el usuario asociado
        public async Task<List<CollectionRequest>> GetCollectionRequests()
        {
            return await _context.CollectionRequests
                                 .Include(s => s.User)
                                 .ToListAsync();
        }

        // Obtiene una solicitud específica por ID incluyendo el usuario asociado
        public async Task<CollectionRequest> GetCollectionRequest(Guid idRequest)
        {
            return await _context.CollectionRequests
                                 .Include(s => s.User)
                                 .FirstOrDefaultAsync(s => s.IdRequest == idRequest);
        }

        // Obtiene todas las solicitudes en estado Pending ordenadas por fecha de solicitud
        // Las más antiguas aparecen primero para priorizar las que llevan más tiempo esperando
        public async Task<List<CollectionRequest>> GetPendingRequests()
        {
            return await _context.CollectionRequests
                .Include(r => r.User)
                .Where(r => r.CurrentStatus == CollectionRequestStatus.Pending)
                .OrderBy(r => r.RequestDate)
                .ToListAsync();
        }

        // Crea una nueva solicitud de recolección y guarda los cambios en la base de datos
        public async Task<CollectionRequest> CreateCollectionRequest(CollectionRequest collectionRequest)
        {
            _context.CollectionRequests.Add(collectionRequest);
            await _context.SaveChangesAsync();
            return collectionRequest;
        }

        // Actualiza una solicitud de recolección existente y guarda los cambios en la base de datos
        public async Task<CollectionRequest> UpdateCollectionRequest(CollectionRequest collectionRequest)
        {
            _context.CollectionRequests.Update(collectionRequest);
            await _context.SaveChangesAsync();
            return collectionRequest;
        }

        // Elimina una solicitud de recolección por su ID, retorna false si no existe
        public async Task<bool> DeleteCollectionRequest(Guid idRequest)
        {
            var collectionRequest = await _context.CollectionRequests
                                                  .FirstOrDefaultAsync(s => s.IdRequest == idRequest);
            if (collectionRequest == null)
                return false;

            _context.CollectionRequests.Remove(collectionRequest);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}