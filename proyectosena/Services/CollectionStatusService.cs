using proyectosena.Interfaces;
using proyectosena.Models;

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

        public async Task<bool> UpdateStatusAsync(
            Guid idRequest,
            string newStatus,
            Guid idManager,
            string? comment = null)
        {
            // 1. Verifica que la solicitud exista
            var request = await _requestRepository.GetCollectionRequest(idRequest);
            if (request == null)
                return false;

            // 2. Guarda el estado anterior antes de cambiarlo
            var previousStatus = request.CurrentStatus;

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
            var notification = new Notification
            {
                IdUser = request.IdUser,
                IdRequest = idRequest,
                Title = GetNotificationTitle(newStatus),
                Message = GetNotificationMessage(newStatus),
                Type = GetNotificationType(newStatus),
                IsRead = false,
                CreationDate = DateTime.UtcNow
            };
            await _notificationRepository.CreateNotification(notification);

            return true;
        }

        // Retorna el título de la notificación según el nuevo estado
        private static string GetNotificationTitle(string status) => status switch
        {
            CollectionRequestStatus.Assigned => "Request Assigned",
            CollectionRequestStatus.InProgress => "Collection In Progress",
            CollectionRequestStatus.Completed => "Collection Completed",
            CollectionRequestStatus.Rejected => "Request Rejected",
            _ => "Status Updated"
        };

        // Retorna el mensaje descriptivo según el nuevo estado
        private static string GetNotificationMessage(string status) => status switch
        {
            CollectionRequestStatus.Assigned => "A manager has been assigned to your collection request.",
            CollectionRequestStatus.InProgress => "The manager is on the way to collect your waste.",
            CollectionRequestStatus.Completed => "Your waste has been successfully collected. Thank you!",
            CollectionRequestStatus.Rejected => "Unfortunately your request could not be processed. Please create a new one.",
            _ => "The status of your request has been updated."
        };

        // Retorna el tipo de notificación según el nuevo estado (para estilos en el frontend)
        private static string GetNotificationType(string status) => status switch
        {
            CollectionRequestStatus.Assigned => "Info",
            CollectionRequestStatus.InProgress => "Info",
            CollectionRequestStatus.Completed => "Success",
            CollectionRequestStatus.Rejected => "Warning",
            _ => "Info"
        };
    }
}