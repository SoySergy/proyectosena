namespace proyectosena.Models
{
    // Resultados posibles al enviar una solicitud para ser gestor
    public enum ManagerApplicationResult
    {
        Success,

        // Ya tiene una esperando revisión: no se apilan solicitudes
        AlreadyPending,

        // Ya se le concedió el rol antes; no tiene sentido volver a pedirlo
        AlreadyApproved
    }

    // Resultados posibles cuando un administrador decide sobre una solicitud
    public enum ManagerApplicationDecisionResult
    {
        Success,

        ApplicationNotFound,

        // Ya fue aprobada o rechazada: no se decide dos veces
        AlreadyDecided
    }
}
