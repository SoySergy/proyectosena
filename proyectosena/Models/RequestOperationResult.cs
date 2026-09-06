namespace proyectosena.Models
{
    // Resultados posibles al editar una solicitud de recolección
    public enum RequestUpdateResult
    {
        Success,

        RequestNotFound,

        // Una vez que un gestor la toma, la solicitud ya no se puede editar
        NotPending
    }

    // Resultados posibles al cancelar una solicitud
    public enum RequestCancelResult
    {
        Success,

        RequestNotFound,

        // Quien pide la cancelación no es el dueño de la solicitud
        NotOwner,

        // Solo se cancela mientras nadie la haya tomado
        NotCancellable
    }
}
