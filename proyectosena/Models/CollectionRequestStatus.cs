
using System.Linq;
namespace proyectosena.Models
{
    // Define los estados válidos para una solicitud de recolección
    // Usar estas constantes evita errores de tipeo y centraliza los valores permitidos
    public static class CollectionRequestStatus
    {
        // Solicitud creada por el ciudadano, esperando asignación
        public const string Pending = "Pending";

        // Gestor fue asignado a la solicitud
        public const string Assigned = "Assigned";

        // Gestor en camino a realizar la recolección
        public const string InProgress = "InProgress";

        // Recolección realizada exitosamente
        public const string Completed = "Completed";

        // Solicitud rechazada por el gestor
        public const string Rejected = "Rejected";

        // Define a qué estados puede pasar cada estado.
        // Lo que no esté aquí, no se permite.
        public static readonly Dictionary<string, List<string>> AllowedTransitions = new()
          {
           { Pending,    new List<string> { Assigned } },
           { Assigned,   new List<string> { InProgress, Rejected } },
           { InProgress, new List<string> { Completed, Rejected } },
           { Completed,  new List<string>() },
           { Rejected,   new List<string>() }
          };
         public static readonly List<string> ValidStatuses = 
            AllowedTransitions.Keys.ToList();
        // Responde si se puede pasar de un estado a otro
        public static bool CanTransition(string from, string to)
        {
            return AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
        }
    }
}