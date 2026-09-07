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

        public CollectionStatusService(
            ICollectionRequestRepository requestRepository,
            IHistoryRepository historyRepository,
            INotificationRepository notificationRepository)
        {
            _requestRepository = requestRepository;
            _historyRepository = historyRepository;
            _notificationRepository = notificationRepository;
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

            return StatusUpdateResult.Success;
        }
    }
}
