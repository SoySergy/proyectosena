// =============================================
// DTO: UpdateCollectionManagementDto
// Usado por el gestor para actualizar el estado
// de una recolección en curso 2
// =============================================

using System.ComponentModel.DataAnnotations;

namespace proyectosena.DTOs.Collection
{
    public class UpdateCollectionManagementDto
    {
        [MaxLength(20)]
        public string? Status { get; set; }

        [Required]
        public Guid IdManager { get; set; }

        public DateTime? ScheduledDate { get; set; }

        public DateTime? CompletionDate { get; set; }

        [MaxLength(200)]
        public string? ManagerObservations { get; set; }
    }
}