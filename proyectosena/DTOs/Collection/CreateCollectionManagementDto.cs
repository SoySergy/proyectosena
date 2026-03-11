// =============================================
// DTO: CreateCollectionManagementDto 1
// Usado por el gestor para tomar una solicitud
// =============================================

using System.ComponentModel.DataAnnotations;

namespace proyectosena.DTOs.Collection
{
    public class CreateCollectionManagementDto
    {
        [Required]
        public Guid IdRequest { get; set; }

        [Required, MaxLength(20)]
        public required string Status { get; set; }

        public DateTime? ScheduledDate { get; set; }

        [MaxLength(200)]
        public string? ManagerObservations { get; set; }
    }
}