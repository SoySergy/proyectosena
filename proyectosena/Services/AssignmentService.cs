using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Interfaces;
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

                // 7. Confirma la transacción — en este punto la solicitud está asignada
                await transaction.CommitAsync();

                // 8. Registra el cambio en el historial
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

                // 9. Notifica al ciudadano que su solicitud fue aceptada
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

        public async Task NotifyAllManagersAsync(Guid idRequest, string collectionAddress)
        {
            // Obtiene todos los usuarios con rol Manager para notificarlos
            var managers = await _userRepository.GetByRoleNameAsync("Manager");

            // Crea una notificación para cada gestor informando que hay una nueva solicitud
            foreach (var manager in managers)
            {
                var notification = new Notification
                {
                    IdUser = manager.IdUser,
                    IdRequest = idRequest,
                    Title = "New Collection Request Available",
                    Message = $"A new collection request is available at: {collectionAddress}. Be the first to accept it!",
                    Type = "Info",
                    IsRead = false,
                    CreationDate = DateTime.UtcNow
                };
                await _notificationRepository.CreateNotification(notification);
            }
        }
    }
}