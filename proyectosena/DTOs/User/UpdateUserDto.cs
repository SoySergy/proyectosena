using System.ComponentModel.DataAnnotations;

namespace proyectosena.DTOs.User
{
    public class UpdateUserDto
    {
        [MinLength(2), MaxLength(70)]
        public string? Name { get; set; }

        [MinLength(2), MaxLength(70)]
        public string? LastName { get; set; }

        [MinLength(7), MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MinLength(5), MaxLength(200)]
        public string? Address { get; set; }

        [MinLength(8), MaxLength(128)]
        public string? CurrentPassword { get; set; }

        [MinLength(8), MaxLength(128)]
        public string? NewPassword { get; set; }
    }
}
