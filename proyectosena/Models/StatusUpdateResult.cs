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
        InvalidTransition,

        // Es gestor, pero esta solicitud no es la que tiene asignada.
        // Antes bastaba el rol: cualquier gestor con sesión válida movía la
        // solicitud de otro tomando su id de la lista general.
        NotAssigned
    }
}
