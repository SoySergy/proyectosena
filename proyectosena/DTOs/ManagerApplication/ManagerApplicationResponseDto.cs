namespace proyectosena.DTOs.ManagerApplication
{
    /// <summary>
    /// Una solicitud de gestor tal como sale por la API.
    /// </summary>
    public class ManagerApplicationResponseDto
    {
        public Guid IdApplication { get; set; }
        public Guid IdUser { get; set; }

        // Datos del solicitante, aplanados: el administrador los necesita para
        // decidir sin tener que pedir el perfil aparte.
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string ApplicantDocument { get; set; } = string.Empty;
        public string ApplicantPhone { get; set; } = string.Empty;

        public string Motivation { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }

        // Null mientras esté pendiente
        public string? ReviewerName { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string? ReviewComment { get; set; }
    }
}
