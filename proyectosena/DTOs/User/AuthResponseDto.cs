// =============================================
// DTO: AuthResponseDto
// Respuesta tras login/register exitoso
// Incluye token JWT e información básica del usuario
// =============================================

namespace proyectosena.DTOs.User
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public DateTime ExpiresAt { get; set; }
        public UserInfoDto User { get; set; } = new();
    }
}