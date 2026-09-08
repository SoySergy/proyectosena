namespace proyectosena.Models
{
    // Resultados posibles al intentar cambiar el estado de una solicitud
    public enum StatusUpdateResult
    {
        Success,

        RequestNotFound,

        // El estado pedido no está entre los valores válidos
        InvalidStatus,

        // El estado existe, pero no se puede llegar a él desde el actual
        InvalidTransition
    }
}
