// =============================================
// DTO: CreateManagerDto
// Used by an administrator to register a manager.
// There is no password field on purpose: the manager
// sets their own through the emailed invitation code.
// =============================================

using System.ComponentModel.DataAnnotations;

namespace proyectosena.DTOs.User
{
    public class CreateManagerDto
    {
        [Required]
        public Guid IdDocumentType { get; set; }

        [Required, MinLength(2), MaxLength(20)]
        public required string DocumentNumber { get; set; }

        [Required, MinLength(2), MaxLength(70)]
        public required string Name { get; set; }

        [Required, MinLength(2), MaxLength(70)]
        public required string LastName { get; set; }

        [Required, EmailAddress, MaxLength(100)]
        public required string Email { get; set; }

        [Required, MinLength(7), MaxLength(20)]
        public required string PhoneNumber { get; set; }

        [Required, MinLength(5), MaxLength(200)]
        public required string Address { get; set; }
    }
}
