using System.ComponentModel.DataAnnotations;

namespace proyectosena.DTOs.ManagerApplication
{
    /// <summary>
    /// Lo que envía el ciudadano para pedir el rol de gestor.
    /// </summary>
    /// <remarks>
    /// No lleva IdUser: sale del token. Sus datos personales tampoco, porque el
    /// administrador ya los tiene en su perfil.
    /// </remarks>
    public class CreateManagerApplicationDto
    {
        [Required, MinLength(20), MaxLength(500)]
        public required string Motivation { get; set; }
    }
}
