namespace proyectosena.DTOs.User
{
    public class RoleDto
    {
        public Guid IdRole { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string RoleDescription { get; set; } = string.Empty;
    }
}
