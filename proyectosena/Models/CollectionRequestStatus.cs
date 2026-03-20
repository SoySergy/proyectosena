namespace proyectosena.Models
{
    // Define los estados válidos para una solicitud de recolección
    // Usar estas constantes evita errores de tipeo y centraliza los valores permitidos
    public static class CollectionRequestStatus
    {
        // Solicitud creada por el ciudadano, esperando asignación
        public const string Pending = "Pending";

        // Gestor fue asignado a la solicitud
        public const string Assigned = "Assigned";

        // Gestor en camino a realizar la recolección
        public const string InProgress = "InProgress";

        // Recolección realizada exitosamente
        public const string Completed = "Completed";

        // Solicitud rechazada por el gestor
        public const string Rejected = "Rejected";

        // Lista de todos los estados válidos para validaciones
        public static readonly List<string> ValidStatuses = new()
        {
            Pending,
            Assigned,
            InProgress,
            Completed,
            Rejected
        };
    }
}