using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Extensions;
using proyectosena.Interfaces.Repositories;
using proyectosena.Models;

namespace proyectosena.Repositories
{
    public class ManagerApplicationRepository : IManagerApplicationRepository
    {
        private readonly RecyRouteDbContext _context;

        public ManagerApplicationRepository(RecyRouteDbContext context)
        {
            _context = context;
        }

        // Un ciudadano puede haber solicitado varias veces si lo rechazaron:
        // interesa la última.
        public async Task<ManagerApplication?> GetLatestByUser(Guid idUser)
        {
            return await _context.ManagerApplications
                .Include(a => a.User)
                .Include(a => a.Reviewer)
                .Where(a => a.IdUser == idUser)
                .OrderByDescending(a => a.RequestDate)
                .FirstOrDefaultAsync();
        }

        public async Task<ManagerApplication?> GetById(Guid idApplication)
        {
            return await _context.ManagerApplications
                .Include(a => a.User)
                .Include(a => a.Reviewer)
                .FirstOrDefaultAsync(a => a.IdApplication == idApplication);
        }

        // La más antigua primero: quien lleva más tiempo esperando se atiende antes
        public async Task<(List<ManagerApplication> Items, int Total)> GetPending(int page, int pageSize)
        {
            return await _context.ManagerApplications
                .Include(a => a.User)
                .Where(a => a.Status == ManagerApplicationStatus.Pending)
                .OrderBy(a => a.RequestDate)
                .ToPagedAsync(page, pageSize);
        }

        // Pregunta a la base en vez de traer las solicitudes para contarlas
        public async Task<bool> HasPending(Guid idUser)
        {
            return await _context.ManagerApplications
                .AnyAsync(a => a.IdUser == idUser
                            && a.Status == ManagerApplicationStatus.Pending);
        }

        public async Task<ManagerApplication> Create(ManagerApplication application)
        {
            _context.ManagerApplications.Add(application);
            await _context.SaveChangesAsync();

            // Se recarga con el solicitante para que el DTO salga completo
            return await _context.ManagerApplications
                .Include(a => a.User)
                .FirstAsync(a => a.IdApplication == application.IdApplication);
        }

        // Un rechazo solo toca esta tabla
        public async Task<ManagerApplication> SaveDecision(ManagerApplication application)
        {
            _context.ManagerApplications.Update(application);
            await _context.SaveChangesAsync();
            return await RecargarConRevisor(application.IdApplication);
        }

        public async Task<ManagerApplication> ApproveAndGrantRole(
            ManagerApplication application, Guid idRoleToGrant)
        {
            var solicitante = await _context.Users
                .FirstOrDefaultAsync(u => u.IdUser == application.IdUser);

            // No debería pasar: la llave foránea garantiza que el usuario existe.
            // Si pasa son datos corruptos y es mejor que salte en el log.
            if (solicitante == null)
                throw new InvalidOperationException(
                    $"La solicitud {application.IdApplication} apunta a un usuario que no existe.");

            // Transacción explícita a propósito. EF manda los dos UPDATE en un solo
            // lote, pero un lote de SQL Server no se revierte solo: si fallara el
            // segundo, el primero quedaría guardado y la solicitud diría «aprobada»
            // con la persona todavía de ciudadana. Así no depende de un detalle del
            // framework, sino de algo que se lee en el código.
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                solicitante.IdRole = idRoleToGrant;
                _context.ManagerApplications.Update(application);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return await RecargarConRevisor(application.IdApplication);
        }

        // Tras decidir, el DTO necesita el nombre de quien decidió
        private async Task<ManagerApplication> RecargarConRevisor(Guid idApplication)
        {
            return await _context.ManagerApplications
                .Include(a => a.User)
                .Include(a => a.Reviewer)
                .FirstAsync(a => a.IdApplication == idApplication);
        }
    }
}
