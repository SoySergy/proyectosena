using System.ComponentModel.DataAnnotations;

namespace proyectosena.DTOs.User
{
    /// <summary>
    /// Tipo de documento tal como entra y sale por la API.
    /// </summary>
    /// <remarks>
    /// Este DTO ya existía pero nadie lo usaba: el controlador exponía la entidad
    /// <c>DocumentType</c> directamente. Las validaciones de longitud son las mismas
    /// que tiene la entidad, para que un nombre demasiado largo lo rechace el
    /// modelo y no reviente contra la base.
    ///
    /// <para>
    /// <c>IdDocumentType</c> arranca con un Guid nuevo igual que la entidad: si el
    /// cliente no lo manda al crear, se genera aquí; si lo manda, se respeta.
    /// </para>
    /// </remarks>
    public class DocumentTypeDto
    {
        public Guid IdDocumentType { get; set; } = Guid.NewGuid();

        [Required, MaxLength(30)]
        public string DocumentName { get; set; } = string.Empty;

        [Required, MaxLength(3)]
        public string Abbreviation { get; set; } = string.Empty;
    }
}
