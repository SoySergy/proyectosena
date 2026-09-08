namespace proyectosena.Models
{
    /// <summary>
    /// Los tres textos que acompañan a una notificación: qué dice el título,
    /// qué dice el cuerpo, y de qué tipo es (el frontend lo usa para el color).
    /// </summary>
    public record NotificationTemplate(string Title, string Message, string Type);

    /// <summary>
    /// Qué se le dice al ciudadano en cada estado de su solicitud.
    /// Una fila por estado, en un solo sitio.
    /// </summary>
    /// <remarks>
    /// Antes esto eran tres métodos con un <c>switch</c> cada uno —uno para el
    /// título, otro para el mensaje y otro para el tipo—. Agregar un estado
    /// obligaba a editar los tres y era fácil olvidar uno: el estado nuevo salía
    /// con el título correcto y el mensaje genérico. Con la tabla, un estado
    /// nuevo es un renglón.
    ///
    /// <para>
    /// <c>Pending</c> no está a propósito: es el estado inicial y no se puede
    /// transicionar hacia él, así que nunca genera notificación. Si algún día
    /// llegara, cae en <see cref="Default"/> igual que antes caía en el <c>_</c>
    /// de los switch.
    /// </para>
    /// </remarks>
    public static class NotificationTemplates
    {
        // Red de seguridad para un estado que no esté en la tabla
        private static readonly NotificationTemplate Default = new(
            "Status Updated",
            "The status of your request has been updated.",
            "Info");

        private static readonly Dictionary<string, NotificationTemplate> ByStatus = new()
        {
            [CollectionRequestStatus.Assigned] = new(
                "Request Assigned",
                "A manager has been assigned to your collection request.",
                "Info"),

            [CollectionRequestStatus.InProgress] = new(
                "Collection In Progress",
                "The manager is on the way to collect your waste.",
                "Info"),

            [CollectionRequestStatus.Completed] = new(
                "Collection Completed",
                "Your waste has been successfully collected. Thank you!",
                "Success"),

            [CollectionRequestStatus.Rejected] = new(
                "Request Rejected",
                "Unfortunately your request could not be processed. Please create a new one.",
                "Warning"),

            [CollectionRequestStatus.Cancelled] = new(
                "Request Cancelled",
                "Your collection request was cancelled.",
                "Warning")
        };

        // Devuelve los textos del estado, o los genéricos si no está en la tabla
        public static NotificationTemplate For(string status)
            => ByStatus.TryGetValue(status, out var template) ? template : Default;
    }
}
