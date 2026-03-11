// =============================================
// DTO: CollectionManagementResponseDto
// Retorna el detalle de una gestión de recolección 3
// =============================================

namespace proyectosena.DTOs.Collection
{
    public class CollectionManagementResponseDto
    {
        public Guid IdManagement { get; set; }
        public Guid IdRequest { get; set; }
        public Guid IdManager { get; set; }
        public string ManagerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? StatusChangeDate { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string? ManagerObservations { get; set; }
    }
}