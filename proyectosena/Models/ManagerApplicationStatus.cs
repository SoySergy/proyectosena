namespace proyectosena.Models
{
    /// <summary>
    /// Estados por los que pasa una solicitud para ser gestor.
    /// </summary>
    /// <remarks>
    /// Mismo patrón que <see cref="CollectionRequestStatus"/>: constantes en vez
    /// de texto suelto, para que un dedazo lo detecte el compilador y no la
    /// base de datos.
    /// </remarks>
    public static class ManagerApplicationStatus
    {
        // Enviada, esperando que un administrador la revise
        public const string Pending = "Pending";

        // Aprobada: al usuario se le concedió el rol de gestor
        public const string Approved = "Approved";

        // Rechazada, con un motivo escrito. El ciudadano puede volver a solicitar.
        public const string Rejected = "Rejected";
    }
}
