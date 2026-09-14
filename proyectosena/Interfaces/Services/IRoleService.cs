using proyectosena.DTOs.User;
using proyectosena.Models;

namespace proyectosena.Interfaces.Services
{
    /// <summary>
    /// Administración del catálogo de roles.
    /// </summary>
    /// <remarks>
    /// Devuelve DTOs, no la entidad: <c>Role</c> arrastra la colección de usuarios
    /// que lo tienen asignado, y eso no tiene por qué asomarse a la API.
    /// </remarks>
    public interface IRoleService
    {
        // Ordenados por nombre
        Task<List<RoleDto>> GetAll();

        // Null si no existe
        Task<RoleDto?> GetById(Guid idRole);

        Task<RoleDto> Create(RoleDto dto);

        // NotFound si el id no existe. NombreDelSistema si intenta cambiar el
        // nombre de Administrator, Manager o Citizen (WA-07): las políticas de
        // autorización y el rol que se graba en el token al iniciar sesión
        // comparan contra ese nombre escrito en el código, no contra el id —
        // renombrarlo aquí no cambiaría el código, y dejaría fuera en silencio
        // a todo el que inicie sesión después del cambio.
        Task<(CatalogMutationResult Result, RoleDto? Role)> Update(RoleDto dto);

        // NotFound si no existe. InUse si algún usuario todavía lo tiene asignado.
        Task<CatalogMutationResult> Delete(Guid idRole);
    }
}
