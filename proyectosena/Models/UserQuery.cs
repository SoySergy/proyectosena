namespace proyectosena.Models
{
    // Los filtros del listado de usuarios, ya resueltos para la base.
    // IsActive en null significa "activos y dados de baja"; lo mismo
    // EmailVerified con "confirmados y sin confirmar".
    public sealed record UserQuery(
        string? RoleName,
        bool? IsActive,
        bool? EmailVerified,
        string? Search,
        bool OrderByRegistrationDate,
        bool Ascending);
}
