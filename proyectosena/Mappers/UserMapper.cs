using proyectosena.DTOs.User;
using proyectosena.Models;

namespace proyectosena.Mappers
{
    /// <summary>
    /// Única definición de qué datos de un usuario salen al cliente.
    /// </summary>
    /// <remarks>
    /// Antes esta lógica existía dos veces — en AuthController y en UserService —
    /// y eso la hacía peligrosa: agregar un campo sensible a User y actualizar
    /// solo una copia lo dejaba filtrándose por la otra. Es duplicación esencial,
    /// el mismo concepto de negocio, así que va en un solo lugar.
    /// </remarks>
    public static class UserMapper
    {
        public static UserInfoDto ToInfoDto(this User user) => new()
        {
            IdUser = user.IdUser,
            IdRole = user.IdRole,
            RoleName = user.Role?.RoleName ?? string.Empty,
            IdDocumentType = user.IdDocumentType,
            DocumentTypeName = user.DocumentType?.DocumentName ?? string.Empty,
            DocumentNumber = user.DocumentNumber,
            Name = user.Name,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            RegistrationDate = user.RegistrationDate

            // Password no aparece aquí, y ese es justamente el punto:
            // el hash nunca sale de la capa de datos.
        };
    }
}
