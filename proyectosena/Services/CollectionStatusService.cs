using proyectosena.Context;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Models;
using proyectosena.Repositories;


namespace proyectosena.Services
{
    public class CollectionStatusService : ICollectionStatusService
    {
        // Repositorios necesarios para la lógica de cambio de estado
        private readonly ICollectionRequestRepository _requestRepository;
        private readonly IHistoryRepository _historyRepository;
        private readonly INotificationRepository _notificationRepository;

        // Solo para abrir la transacción que envuelve a los tres repositorios
        // de arriba. Todos comparten el mismo DbContext (con alcance de
        // petición), así que sus SaveChangesAsync quedan dentro de ella.
        private readonly RecyRouteDbContext _context;

        public CollectionStatusService(
            ICollectionRequestRepository requestRepository,
            IHistoryRepository historyRepository,
            INotificationRepository notificationRepository,
            RecyRouteDbContext context)
        {
            _requestRepository = requestRepository;
            _historyRepository = historyRepository;
            _notificationRepository = notificationRepository;
            _context = context;
        }

        public async Task<StatusUpdateResult> UpdateStatusAsync(
            Guid idRequest,
            string newStatus,
            Guid idManager,
            string? comment = null)
        {
            // 1. Verifica que la solicitud exista
            var request = await _requestRepository.GetCollectionRequest(idRequest);
            if (request == null)
                return StatusUpdateResult.RequestNotFound;

            // 2. Guarda el estado anterior antes de cambiarlo
            var previousStatus = request.CurrentStatus;

            if (!CollectionRequestStatus.CanTransition(previousStatus, newStatus))
                return StatusUpdateResult.InvalidTransition;

            // Las tres escrituras de abajo (estado, historial, notificación) tienen
            // que quedar todas o ninguna. Antes cada repositorio hacía su propio
            // SaveChangesAsync por separado: si el segundo fallaba —por ejemplo, un
            // corte de conexión—, la solicitud ya había cambiado de estado sin dejar
            // rastro en el historial ni avisar al ciudadano (BL-13).
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 3. Actualiza el estado de la solicitud
                request.CurrentStatus = newStatus;
                await _requestRepository.UpdateCollectionRequest(request);

                // 4. Registra el cambio en el historial
                var history = new History
                {
                    IdRequest = idRequest,
                    IdUser = idManager,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    ChangeDate = DateTime.UtcNow,
                    Comment = comment
                };
                await _historyRepository.Create(history);

                // 5. Crea una notificación para el ciudadano dueño de la solicitud
                var template = NotificationTemplates.For(newStatus);

                var notification = new Notification
                {
                    IdUser = request.IdUser,
                    IdRequest = idRequest,
                    Title = template.Title,
                    Message = template.Message,
                    Type = template.Type,
                    IsRead = false,
                    CreationDate = DateTime.UtcNow
                };
                await _notificationRepository.CreateNotification(notification);

                await transaction.CommitAsync();
                return StatusUpdateResult.Success;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
