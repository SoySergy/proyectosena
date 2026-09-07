using proyectosena.Models;

namespace proyectosena.Interfaces.Repositories
{
    /// <summary>
    /// Preguntas sobre <b>conjuntos</b> de usuarios: listarlos, filtrarlos por
    /// rol, contarlos.
    /// </summary>
    /// <remarks>
    /// Separado a propósito de <see cref="IUserLookupRepository"/>:
    /// <c>AssignmentService</c> solo necesita saber a qué gestores avisar, y no
    /// tiene por qué depender de que se pueda crear o borrar un usuario. Antes
    /// dependía de un contrato de nueve métodos para usar uno.
    /// </remarks>
    public interface IUserDirectoryRepository
    {
        // Página de usuarios activos, ordenados por nombre
        Task<(List<User> Items, int Total)> GetUsers(int page, int pageSize);

        // Todos los usuarios activos de un rol. Sirve para avisar a los gestores.
        Task<List<User>> GetByRoleNameAsync(string roleName);

        // Cuenta activos de un rol sin traer ninguno
        Task<int> CountByRole(string roleName);
    }
}
