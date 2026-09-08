namespace proyectosena.Models
{
    // Resultado de intentar acceder a los datos de una solicitud: su chat, su
    // historial o su gestión.
    public enum RequestAccessResult
    {
        Success,

        // Quien pide no es el dueño de la solicitud ni un gestor asignado a ella
        NotParticipant,

        // Lo que se pidió no existe
        NotFound
    }
}
