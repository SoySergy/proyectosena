namespace proyectosena.Models
{
    // Resultados posibles al leer o escribir en el chat de una solicitud
    public enum ChatAccessResult
    {
        Success,

        // Quien pide no es el dueño de la solicitud ni un gestor asignado a ella
        NotParticipant
    }
}
