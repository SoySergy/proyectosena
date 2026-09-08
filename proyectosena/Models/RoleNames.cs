    namespace proyectosena.Models
{
    /// <summary>
    /// Los nombres de rol, en un solo sitio.
    /// </summary>
    /// <remarks>
    /// Estaban escritos a mano en once lugares: la semilla del DbContext, las
    /// cuatro políticas de <c>Program.cs</c>, los conteos del panel, la
    /// validación de reasignación y el claim del token. Un dedazo en cualquiera
    /// no rompe la compilación: la política deja de coincidir o el conteo
    /// devuelve cero, en silencio.
    ///
    /// <para>
    /// Son <c>const</c> y no <c>static readonly</c> porque las políticas de
    /// autorización y los atributos necesitan constantes de tiempo de compilación.
    /// El valor debe seguir coincidiendo con <c>Role.RoleName</c> en la base.
    /// </para>
    /// </remarks>
    public static class RoleNames
    {
        public const string Administrator = "Administrator";
        public const string Manager = "Manager";
        public const string Citizen = "Citizen";
    }
}
