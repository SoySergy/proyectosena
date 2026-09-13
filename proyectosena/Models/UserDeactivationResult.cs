namespace proyectosena.Models
{
    // Resultados posibles al dar de baja a un usuario
    public enum UserDeactivationResult
    {
        Success,

        UserNotFound,

        // Es administrador y es el único activo que queda. Sin este freno,
        // el sistema se queda sin nadie que pueda gestionar roles, usuarios
        // ni postulaciones — y BL-03 ya muestra que una base sin
        // administrador no tiene forma de crear uno nuevo por API (WA-16).
        LastAdministrator
    }
}
