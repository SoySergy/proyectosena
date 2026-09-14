namespace proyectosena.Models
{
    // Resultado de actualizar o borrar una fila de un catálogo pequeño (rol,
    // tipo de documento): comparten la misma forma de fallar — no existe, o
    // está en uso por una relación de clave foránea (WA-06).
    public enum CatalogMutationResult
    {
        Success,
        NotFound,

        // Alguien la tiene asignada (un Usuario con ese rol o tipo de
        // documento). Borrarla igual reventaría la clave foránea; antes eso
        // llegaba como un 500 sin explicación.
        InUse,

        // Solo para roles: Administrator, Manager y Citizen no se pueden
        // renombrar (WA-07). Ver RoleService.EsRolDelSistema.
        NombreDelSistema
    }
}
