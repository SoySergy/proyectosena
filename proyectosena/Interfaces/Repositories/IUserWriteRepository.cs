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

        // Baja lógica atómica: bajo un candado de Postgres, comprueba que no
        // sea el último administrador activo y aplica la baja en el mismo
        // tramo bloqueado. Sin el candado, dos bajas simultáneas de
        // administradores distintos pueden leer "quedan 2" antes de que
        // ninguna escriba, y las dos pasan el freno a la vez (WA-16).
        Task<UserDeactivationResult> DeactivateWithLastAdminGuard(Guid idUser, string administratorRoleName);
    }
}
