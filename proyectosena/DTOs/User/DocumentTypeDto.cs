namespace proyectosena.DTOs.User
{
    public class DocumentTypeDto
    {
        public Guid IdDocumentType { get; set; }
        public string DocumentTypeName { get; set; } = null!;
        public string Abbreviation { get; set; } = null!;
    }
}
