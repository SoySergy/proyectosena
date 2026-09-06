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

        public ChatHistoryService(
            IChatHistoryRepository chatHistoryRepository,
            ICollectionRequestRepository collectionRequestRepository)
        {
            _chatHistoryRepository = chatHistoryRepository;
            _collectionRequestRepository = collectionRequestRepository;
        }

        public async Task<(ChatAccessResult Result, ChatMessageResponseDto? Message)> SendMessage(SendMessageDto dto)
        {
            // La regla del negocio: solo el dueño de la solicitud o un gestor
            // asignado pueden escribir en su conversación.
            var allowed = await _collectionRequestRepository.IsParticipant(dto.IdRequest, dto.IdSender);
            if (!allowed)
                return (ChatAccessResult.NotParticipant, null);

            var message = new ChatHistory
            {
                IdRequest = dto.IdRequest,
                IdSender = dto.IdSender,
                Message = dto.Message,
                SendDate = DateTime.UtcNow,
                IsRead = false
            };

            var created = await _chatHistoryRepository.CreateMessage(message);

            // Se recarga para traer remitente y rol, que el DTO necesita
            var full = await _chatHistoryRepository.GetMessage(created.IdChatHistory);

            return (ChatAccessResult.Success, MapToDto(full));
        }

        public async Task<(ChatAccessResult Result, List<ChatMessageResponseDto> Messages)> GetMessagesByRequest(
            Guid idRequest, Guid idUser)
        {
            var allowed = await _collectionRequestRepository.IsParticipant(idRequest, idUser);
            if (!allowed)
                return (ChatAccessResult.NotParticipant, new List<ChatMessageResponseDto>());

            var messages = await _chatHistoryRepository.GetMessagesByRequest(idRequest);

            // Una conversación vacía no es un error: es una que no ha empezado
            return (ChatAccessResult.Success, messages.Select(MapToDto).ToList());
        }

        public async Task<List<ChatMessageResponseDto>> GetUnreadMessages(Guid idUser, Guid idRequest)
        {
            var messages = await _chatHistoryRepository.GetUnreadMessages(idUser, idRequest);
            return messages.Select(MapToDto).ToList();
        }

        public Task<bool> MarkAsRead(Guid idChatHistory)
            => _chatHistoryRepository.MarkAsRead(idChatHistory);

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
