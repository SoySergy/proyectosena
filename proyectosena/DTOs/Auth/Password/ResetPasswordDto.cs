using System.ComponentModel.DataAnnotations;

namespace proyectosena.DTOs.Auth.Password
{
    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        // Mismo mínimo que RegisterDto.Password y UpdateUserDto.NewPassword.
        // Sin esto se podía fijar una contraseña de un solo carácter: el
        // registro validaba, pero este mismo formulario también lo usa quien
        // recupera su contraseña y el gestor que activa la suya por primera
        // vez, y ninguno de los dos pasaba por ninguna comprobación.
        [Required, MinLength(8), MaxLength(128)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
