using proyectosena.Models;

namespace proyectosena.Interfaces
{
    public interface IUserRepository
    {
        // Obtiene una página de usuarios activos
        Task<(List<User> Items, int Total)> GetUsers(int page, int pageSize);

        // Obtiene un usuario por su ID
        Task<User> GetUser(Guid idUser);

        // Obtiene todos los usuarios que tienen un rol específico por nombre
        Task<List<User>> GetByRoleNameAsync(string roleName);

        // Counts active users of a role without loading any of them
        Task<int> CountByRole(string roleName);

        // Crea un nuevo usuario
        Task<User> CreateUser(User user);

        // Actualiza un usuario existente
        Task<User> UpdateUser(User user);

        // Obtiene un usuario por su correo electrónico
        Task<User> GetUserByEmail(string email);

        // Obtiene un usuario por su nombre
        Task<User> GetUserByName(string name);

        // Elimina un usuario por su ID
        Task<bool> DeleteUser(Guid idUser);

        // Obtiene un usuario por número de documento Y tipo de documento
        // Ambos campos juntos determinan si el documento ya está registrado
        Task<User?> GetUserByDocument(string documentNumber, Guid idDocumentType);
    }
}