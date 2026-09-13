
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

        // Solicitud cancelada por el ciudadano antes de que un gestor la tomara
        public const string Cancelled = "Cancelled";

        // Define a qué estados puede pasar cada estado.
        // Lo que no esté aquí, no se permite.
        //
        // Pending NO lista Assigned a propósito. Llegar a Assigned exige crear
        // a la vez la fila de CollectionManagement que dice quién es el
        // gestor —eso solo lo hace AssignmentService.AcceptRequestAsync, en su
        // propia transacción—. Si esta tabla lo permitiera, UpdateStatus (el
        // motor genérico que solo cambia la columna de estado) podría marcar
        // una solicitud como Assigned sin ligarla a ningún gestor: quedaba en
        // un limbo que no se podía aceptar, ni reasignar, ni cancelar, ni ver
        // su chat, y sin ninguna ruta de recuperación por API. Se comprobó
        // así, letra por letra, antes de este cambio.
        public static readonly Dictionary<string, List<string>> AllowedTransitions = new()
          {
           { Pending,    new List<string> { Cancelled } },
           { Assigned,   new List<string> { InProgress, Rejected } },
           { InProgress, new List<string> { Completed, Rejected } },
           { Completed,  new List<string>() },
           { Rejected,   new List<string>() },
           { Cancelled,  new List<string>() }
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