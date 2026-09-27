namespace proyectosena.Extensions
{
    // Lo que escribe una persona en un buscador entra en un LIKE, y ahí % y _
    // son comodines: sin escaparlos, buscar "50%" encontraría cualquier "50".
    public static class SearchTextExtensions
    {
        public const string LikeEscape = "\\";

        public static string EscapeForLike(this string text) =>
            text.Replace(LikeEscape, LikeEscape + LikeEscape)
                .Replace("%", LikeEscape + "%")
                .Replace("_", LikeEscape + "_");
    }
}
