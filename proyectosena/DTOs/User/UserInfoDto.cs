namespace proyectosena.DTOs.User
{
    public class UserInfoDto
    {
        public Guid IdUser { get; set; }
        public Guid IdRole { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public Guid IdDocumentType { get; set; }
        public string DocumentTypeName { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
    }
}
