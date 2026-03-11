using System.ComponentModel.DataAnnotations;

namespace proyectosena.DTOs.Auth
{
    public class LoginDto
    {
        [Required, EmailAddress, MaxLength(100)]
        public required string Email { get; set; }

        [Required, MinLength(8), MaxLength(128)]
        public required string Password { get; set; }
    }
}
