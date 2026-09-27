namespace proyectosena.Models
{
    // Los filtros de la lista de solicitudes, ya listos para la base. Las
    // fechas son rangos semiabiertos [desde, antes de): así el último día
    // entra completo sin tener que hablar de las 23:59:59.
    //
    // Lo arma CollectionRequestService a partir de lo que pidió el cliente;
    // el repositorio no sabe nada de días de Colombia ni de UTC.
    public sealed record CollectionRequestQuery(
        string[] Statuses,
        DateTime? CreatedFromUtc,
        DateTime? CreatedBeforeUtc,
        DateTime? CollectionFrom,
        DateTime? CollectionBefore,
        Guid? IdManager,
        Guid? IdCitizen,
        string? Search,
        bool OrderByCollectionDate,
        bool Ascending);
}
