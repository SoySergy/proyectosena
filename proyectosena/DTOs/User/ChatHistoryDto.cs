namespace proyectosena.DTOs.User
{
    public class ChatHistoryDto
    {
        public Guid IdChatHistory { get; set; }
        public Guid IdRequest { get; set; }
        public Guid IdSender { get; set; }
        public string SenderName { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime SendDate { get; set; }
        public bool IsRead { get; set; }
    }
}
