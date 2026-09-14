using proyectosena.DTOs.Communication;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Services
{
    public class ChatHistoryService : IChatHistoryService
    {
        private readonly IChatHistoryRepository _chatHistoryRepository;

        // Necesario para saber quién pertenece a cada solicitud
        private readonly ICollectionRequestRepository _collectionRequestRepository;

        // Para saber a quién avisar: el gestor asignado, si quien escribió es
        // el ciudadano dueño
        private readonly ICollectionManagementRepository _managementRepository;

        // Avisa al otro participante cuando llega un mensaje nuevo
        private readonly INotificationRepository _notificationRepository;

        public ChatHistoryService(
            IChatHistoryRepository chatHistoryRepository,
            ICollectionRequestRepository collectionRequestRepository,
            ICollectionManagementRepository managementRepository,
            INotificationRepository notificationRepository)
        {
            _chatHistoryRepository = chatHistoryRepository;
            _collectionRequestRepository = collectionRequestRepository;
            _managementRepository = managementRepository;
            _notificationRepository = notificationRepository;
        }

        public async Task<(RequestAccessResult Result, ChatMessageResponseDto? Message)> SendMessage(
            SendMessageDto dto, Guid idSender)
        {
            // La regla del negocio: solo el dueño de la solicitud o un gestor
            // asignado pueden escribir en su conversación.
            var allowed = await _collectionRequestRepository.IsParticipant(dto.IdRequest, idSender);
            if (!allowed)
                return (RequestAccessResult.NotParticipant, null);

            var message = new ChatHistory
            {
                IdRequest = dto.IdRequest,
                IdSender = idSender,
                Message = dto.Message,
                SendDate = DateTime.UtcNow,
                IsRead = false
            };

            var created = await _chatHistoryRepository.CreateMessage(message);

            // Se recarga para traer remitente y rol, que el DTO necesita
            var full = await _chatHistoryRepository.GetMessage(created.IdChatHistory);

            // Un fallo aquí no debe impedir que el mensaje se haya enviado: la
            // conversación ya quedó guardada, avisar es un extra.
            await AvisarAlOtroParticipante(full!, idSender);

            return (RequestAccessResult.Success, MapToDto(full!));
        }

        // Sin esto, quien no tiene el chat abierto en pantalla no se entera de
        // que le escribieron hasta que entra por su cuenta a revisar.
        private async Task AvisarAlOtroParticipante(ChatHistory mensaje, Guid idSender)
        {
            var request = await _collectionRequestRepository.GetCollectionRequest(mensaje.IdRequest);
            if (request == null)
                return;

            // Si escribió el ciudadano dueño, el destinatario es el gestor
            // asignado —puede no haber ninguno todavía—. Si no, quien escribió
            // es un gestor asignado (lo exige IsParticipant), y el destinatario
            // es el ciudadano dueño.
            Guid? destinatario = idSender == request.IdUser
                ? (await _managementRepository.GetByRequest(mensaje.IdRequest))?.IdManager
                : request.IdUser;

            if (destinatario is null || destinatario == idSender)
                return;

            await _notificationRepository.CreateNotification(new Notification
            {
                IdUser = destinatario,
                IdRequest = mensaje.IdRequest,
                Title = "New Chat Message",
                Message = $"{mensaje.Sender?.Name} sent you a new message.",
                Type = "Info",
                IsRead = false,
                CreationDate = DateTime.UtcNow
            });
        }

        public async Task<(RequestAccessResult Result, List<ChatMessageResponseDto> Messages)> GetMessagesByRequest(
            Guid idRequest, Guid idUser)
        {
            var allowed = await _collectionRequestRepository.IsParticipant(idRequest, idUser);
            if (!allowed)
                return (RequestAccessResult.NotParticipant, new List<ChatMessageResponseDto>());

            var messages = await _chatHistoryRepository.GetMessagesByRequest(idRequest);

            // Una conversación vacía no es un error: es una que no ha empezado
            return (RequestAccessResult.Success, messages.Select(MapToDto).ToList());
        }

        public async Task<(RequestAccessResult Result, List<ChatMessageResponseDto> Messages)> GetUnreadMessages(
            Guid idUser, Guid idRequest)
        {
            // Misma regla que GetMessagesByRequest: si no participas, no lees.
            // Faltaba aquí, y el filtro del repositorio no la suple.
            var allowed = await _collectionRequestRepository.IsParticipant(idRequest, idUser);
            if (!allowed)
                return (RequestAccessResult.NotParticipant, new List<ChatMessageResponseDto>());

            var messages = await _chatHistoryRepository.GetUnreadMessages(idUser, idRequest);
            return (RequestAccessResult.Success, messages.Select(MapToDto).ToList());
        }

        public async Task<RequestAccessResult> MarkAsRead(Guid idChatHistory, Guid idUser)
        {
            var message = await _chatHistoryRepository.GetMessage(idChatHistory);

            if (message == null)
                return RequestAccessResult.NotFound;

            // Faltaba justo aquí: leer y escribir ya comprobaban participación, pero
            // marcar como leído no. Se podía marcar el mensaje de otro sin poder leerlo.
            var allowed = await _collectionRequestRepository.IsParticipant(message.IdRequest, idUser);
            if (!allowed)
                return RequestAccessResult.NotParticipant;

            await _chatHistoryRepository.MarkAsRead(idChatHistory);

            return RequestAccessResult.Success;
        }

        // ── Mapeo privado ───────────────────────────────────────────────
        // Aplana el remitente: el cliente recibe nombre y rol, nunca la entidad User
        private static ChatMessageResponseDto MapToDto(ChatHistory c) => new()
        {
            IdChatHistory = c.IdChatHistory,
            IdRequest = c.IdRequest,
            IdSender = c.IdSender,
            SenderName = c.Sender?.Name ?? string.Empty,
            SenderLastName = c.Sender?.LastName ?? string.Empty,
            SenderRole = c.Sender?.Role?.RoleName ?? string.Empty,
            Message = c.Message,
            SendDate = c.SendDate,
            IsRead = c.IsRead
        };
    }
}
