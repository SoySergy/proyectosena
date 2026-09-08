using proyectosena.Models;

namespace proyectosena.Interfaces.Repositories
{
    /// <summary>
    /// Lo que modifica usuarios: crear, actualizar y dar de baja.
    /// </summary>
    /// <remarks>
    /// Aparte de las lecturas para que se vea de un vistazo qué servicios pueden
    /// cambiar datos de un usuario y cuáles solo los consultan. Hoy solo tres lo
    /// inyectan: autenticación, perfil y alta de gestores.
    /// </remarks>
    public interface IUserWriteRepository
    {
        // Crea y recarga con rol y tipo de documento, para que el token y el DTO
        // salgan completos
        Task<User> CreateUser(User user);

        Task<User> UpdateUser(User user);

        // Baja lógica: la fila se conserva para auditoría. False si no existe.
        Task<bool> DeleteUser(Guid idUser);
    }
}
