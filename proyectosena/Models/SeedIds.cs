namespace proyectosena.Models
{
    /// <summary>
    /// Identificadores de las filas sembradas por migración.
    /// Única fuente de la verdad: antes estaban escritos a mano en el DbContext
    /// y repetidos en los controladores. Cambiar uno aquí lo cambia en todas partes.
    /// </summary>
    /// <remarks>
    /// Estos valores NO se pueden modificar: ya están en las bases de datos
    /// existentes. Cambiar uno haría que EF genere una migración que borra la
    /// fila vieja y crea una nueva, rompiendo las llaves foráneas que la apuntan.
    /// </remarks>
    public static class SeedIds
    {
        // ── Roles ──────────────────────────────────
        public static class Roles
        {
            public static readonly Guid Administrator = Guid.Parse("00000000-0000-0000-0000-000000000001");
            public static readonly Guid Manager       = Guid.Parse("00000000-0000-0000-0000-000000000002");
            public static readonly Guid Citizen       = Guid.Parse("7D759012-4D17-46E8-BCE8-D74FDF171EAB");
        }

        // ── Tipos de documento ─────────────────────
        public static class DocumentTypes
        {
            public static readonly Guid CedulaCiudadania  = Guid.Parse("63D5F1A7-6C0C-4A05-ADF8-D65964D2B3B1");
            public static readonly Guid Pasaporte         = Guid.Parse("FCCCB874-75F3-4374-B8E5-A7A92B084D6C");
            public static readonly Guid CedulaExtranjeria = Guid.Parse("5BCB367C-1F41-4C5A-B120-F01F35159DD8");
            public static readonly Guid TarjetaIdentidad  = Guid.Parse("D0000000-0000-0000-0000-000000000004");
        }
    }
}
