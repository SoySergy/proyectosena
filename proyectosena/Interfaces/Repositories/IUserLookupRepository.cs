using proyectosena.Models;

namespace proyectosena.Interfaces.Repositories
{
    /// <summary>
    /// Encontrar <b>un</b> usuario a partir de algún identificador.
    /// </summary>
    /// <remarks>
    /// Lo usan el login, el registro y el perfil: todos preguntan lo mismo
    /// —«¿existe este usuario?»— por caminos distintos.
    /// </remarks>
    public interface IUserLookupRepository
    {
        // Por su ID, con rol y tipo de documento cargados
        Task<User> GetUser(Guid idUser);

        // Por correo electrónico
        Task<User> GetUserByEmail(string email);

        // Por número Y tipo de documento a la vez: un mismo número puede existir
        // con otro tipo, y solo es duplicado si coinciden los dos campos.
        Task<User?> GetUserByDocument(string documentNumber, Guid idDocumentType);
    }
}
