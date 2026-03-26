using proyectosena.Models;

namespace proyectosena.Interfaces
{
    public interface IUserRepository
    {
        // Obtiene todos los usuarios
        Task<List<User>> GetUsers();

        // Obtiene un usuario por su ID
        Task<User> GetUser(Guid idUser);

        // Obtiene todos los usuarios que tienen un rol específico por nombre
        Task<List<User>> GetByRoleNameAsync(string roleName);

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