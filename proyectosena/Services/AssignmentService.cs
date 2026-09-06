using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Services
{
    public class AssignmentService : IAssignmentService
    {
        // Contexto directo para manejar transacciones atómicas
        // Una transacción garantiza que si dos gestores aceptan al mismo tiempo
        // solo uno se queda con la solicitud
        private readonly RecyRouteDbContext _context;

        // Repositorios necesarios para crear historial y notificaciones
        private readonly IHistoryRepository _historyRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;

        public AssignmentService(
            RecyRouteDbContext context,
            IHistoryRepository historyRepository,
            INotificationRepository notificationRepository,
            IUserRepository userRepository)
        {
            _context = context;
            _historyRepository = historyRepository;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
        }

        public async Task<(bool Success, string Message)> AcceptRequestAsync(Guid idRequest, Guid idManager)
        {
            // Iniciamos una transacción para garantizar que la operación sea atómica
            // Si dos gestores intentan aceptar al mismo tiempo, la base de datos
            // garantiza que solo uno pueda cambiar el estado a "Assigned"
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Buscamos la solicitud con un bloqueo pesimista
                // El bloqueo evita que otro gestor modifique el registro mientras lo procesamos
                var request = await _context.CollectionRequests
                    .FromSqlRaw("SELECT * FROM CollectionRequest WITH (UPDLOCK, ROWLOCK) WHERE IdRequest = {0}", idRequest)
                    .FirstOrDefaultAsync();

                // 2. Verifica que la solicitud exista
                if (request == null)
                    return (false, "Collection request not found.");

                // 3. Verifica que la solicitud siga en Pending
                // Si otro gestor ya la tomó, este paso la rechaza
                if (request.CurrentStatus != CollectionRequestStatus.Pending)
                    return (false, "This request has already been taken by another manager.");

                // 4. Cambia el estado a Assigned
                request.CurrentStatus = CollectionRequestStatus.Assigned;
                _context.CollectionRequests.Update(request);

                // 5. Crea el registro de gestión vinculando el gestor a la solicitud
                var management = new CollectionManagement
                {
                    IdRequest = idRequest,
                    IdManager = idManager,
                    Status = CollectionRequestStatus.Assigned,
                    StatusChangeDate = DateTime.UtcNow
                };
                await _context.CollectionManagements.AddAsync(management);

                // 6. Guarda los cambios dentro de la transacción
                await _context.SaveChangesAsync();

                // 7. Registra el cambio en el historial
                var history = new History
                {
                    IdRequest = idRequest,
                    IdUser = idManager,
                    PreviousStatus = CollectionRequestStatus.Pending,
                    NewStatus = CollectionRequestStatus.Assigned,
                    ChangeDate = DateTime.UtcNow,
                    Comment = "Request accepted by manager."
                };
                await _historyRepository.Create(history);

                // 8. Notifica al ciudadano que su solicitud fue aceptada
                var notification = new Notification
                {
                    IdUser = request.IdUser,
                    IdRequest = idRequest,
                    Title = "Request Accepted",
                    Message = "A manager has accepted your collection request and will be in touch soon.",
                    Type = "Success",
                    IsRead = false,
                    CreationDate = DateTime.UtcNow
                };
                await _notificationRepository.CreateNotification(notification);

                // 9. Confirma la transacción — en este punto la solicitud está asignada
                await transaction.CommitAsync();

                return (true, "Request accepted successfully.");
            }
            catch (Exception ex)
            {
                // Si algo falla revertimos toda la transacción
                // Esto garantiza que no queden datos inconsistentes en la BD
                await transaction.RollbackAsync();
                return (false, $"Error accepting the request: {ex.Message}");
            }
        }

        // Moves an active request from its current manager to another one.
        // CollectionManagement holds who has it now; History records that it moved.
        public async Task<(bool Success, string Message)> ReassignRequestAsync(
            Guid idRequest, Guid idNewManager, Guid idAdmin)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.CollectionRequests
                    .FirstOrDefaultAsync(r => r.IdRequest == idRequest);

                if (request == null)
                    return (false, "Collection request not found.");

                // A finished or unassigned request has nothing to move
                if (request.CurrentStatus != CollectionRequestStatus.Assigned &&
                    request.CurrentStatus != CollectionRequestStatus.InProgress)
                    return (false, "Only assigned or in-progress requests can be reassigned.");

                // The target must be a manager, and still active
                var newManager = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.IdUser == idNewManager);

                if (newManager == null || !newManager.IsActive || newManager.Role?.RoleName != "Manager")
                    return (false, "The target user is not an active manager.");

                // Current assignment row for this request
                var management = await _context.CollectionManagements
                    .Where(m => m.IdRequest == idRequest)
                    .OrderByDescending(m => m.StatusChangeDate)
                    .FirstOrDefaultAsync();

                if (management == null)
                    return (false, "This request has no assignment to move.");

                if (management.IdManager == idNewManager)
                    return (false, "The request is already assigned to that manager.");

                management.IdManager = idNewManager;
                management.StatusChangeDate = DateTime.UtcNow;

                // The status does not change — what changed is who is responsible
                var history = new History
                {
                    IdRequest = idRequest,
                    IdUser = idAdmin,
                    PreviousStatus = request.CurrentStatus,
                    NewStatus = request.CurrentStatus,
                    ChangeDate = DateTime.UtcNow,
                    Comment = $"Request reassigned to {newManager.Name} {newManager.LastName}."
                };
                await _context.Histories.AddAsync(history);

                var notification = new Notification
                {
                    IdUser = idNewManager,
                    IdRequest = idRequest,
                    Title = "Request Assigned To You",
                    Message = $"An administrator assigned you a collection request at: {request.CollectionAddress}.",
                    Type = "Info",
                    IsRead = false,
                    CreationDate = DateTime.UtcNow
                };
                await _context.Notifications.AddAsync(notification);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Request reassigned successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error reassigning the request: {ex.Message}");
            }
        }

        public async Task NotifyAllManagersAsync(Guid idRequest, string collectionAddress)
        {
            // Get every user with the Manager role
            var managers = await _userRepository.GetByRoleNameAsync("Manager");

            // Build one notification per manager, without touching the database yet
            var notifications = managers.Select(manager => new Notification
            {
                IdUser = manager.IdUser,
                IdRequest = idRequest,
                Title = "New Collection Request Available",
                Message = $"A new collection request is available at: {collectionAddress}. Be the first to accept it!",
                Type = "Info",
                IsRead = false,
                CreationDate = DateTime.UtcNow
            }).ToList();

            // Save them all in a single round trip
            await _notificationRepository.CreateNotifications(notifications);
        }
    }
}