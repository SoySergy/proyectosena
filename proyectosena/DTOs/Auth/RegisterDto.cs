using System.ComponentModel.DataAnnotations;

namespace proyectosena.DTOs.Auth
{
    public class RegisterDto
    {
        // ── Foreign Keys ───────────────────────────
        // IdRole no viaja aquí a propósito. Cuando lo hacía, cualquiera podía
        // registrarse como administrador mandando el identificador de ese rol.
        // El registro siempre crea un ciudadano; el rol de gestor lo concede un
        // administrador tras revisar la solicitud.

        [Required]
        public Guid IdDocumentType { get; set; }

        // ── Campos ─────────────────────────────────
        [Required, MinLength(2), MaxLength(70)]
        public required string Name { get; set; }

        [Required, MinLength(2), MaxLength(70)]
        public required string LastName { get; set; }

        [Required, MinLength(2), MaxLength(20)]
        public required string DocumentNumber { get; set; }

        [Required, MinLength(7), MaxLength(20)]
        public required string PhoneNumber { get; set; }

        [Required, MinLength(5), MaxLength(200)]
        public required string Address { get; set; }

        [Required, EmailAddress, MaxLength(100)]
        public required string Email { get; set; }

        [Required, MinLength(8), MaxLength(128)]
        public required string Password { get; set; }
    }
}
