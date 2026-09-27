using System.ComponentModel.DataAnnotations;
using proyectosena.Models;

namespace proyectosena.DTOs.Requests
{
    // Filtros de GetCollectionRequests. Todos son opcionales y se combinan
    // entre sí; sin ninguno, la respuesta es la de siempre: todas las
    // solicitudes, las más recientes primero. Llegan por la query, por ejemplo
    // ?status=Pending,Assigned&createdFrom=2026-09-01&search=carrera 10
    public class CollectionRequestFilterDto : IValidatableObject
    {
        // Uno o varios estados, separados por comas
        [MaxLength(200)]
        public string? Status { get; set; }

        // Días de calendario en Colombia, los dos incluidos. RequestDate se
        // guarda en UTC: el servicio hace la conversión.
        public DateOnly? CreatedFrom { get; set; }
        public DateOnly? CreatedTo { get; set; }

        // Fecha de recolección, los dos días incluidos. Es un día del
        // calendario sin zona horaria (BL-02), así que se compara tal cual.
        public DateOnly? CollectionFrom { get; set; }
        public DateOnly? CollectionTo { get; set; }

        public Guid? IdManager { get; set; }
        public Guid? IdCitizen { get; set; }

        // Dirección, teléfono o nombre del ciudadano, sin distinguir mayúsculas
        [MaxLength(100)]
        public string? Search { get; set; }

        // requestDate (por defecto) o collectionDate
        public string? OrderBy { get; set; }

        // desc (por defecto) o asc
        public string? Direction { get; set; }

        // Se corrigen en el servicio con PagedResult.Normalize, como en el resto
        // de listas: una página fuera de rango no es un error.
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Los estados pedidos, ya separados. Vacío significa "cualquier estado".
        public string[] Statuses() =>
            string.IsNullOrWhiteSpace(Status)
                ? Array.Empty<string>()
                : Status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        public bool OrdersByCollectionDate =>
            string.Equals(OrderBy, "collectionDate", StringComparison.OrdinalIgnoreCase);

        public bool Ascending =>
            string.Equals(Direction, "asc", StringComparison.OrdinalIgnoreCase);

        // [ApiController] responde 400 con estos mensajes antes de llegar al
        // controlador. Un filtro mal escrito no se ignora en silencio: devolver
        // la lista completa haría creer que el filtro funcionó.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var status in Statuses())
            {
                if (!CollectionRequestStatus.ValidStatuses.Contains(status))
                    yield return new ValidationResult(
                        $"El estado «{status}» no existe.", new[] { nameof(Status) });
            }

            if (CreatedFrom > CreatedTo)
                yield return new ValidationResult(
                    "La fecha de creación inicial es posterior a la final.", new[] { nameof(CreatedFrom) });

            if (CollectionFrom > CollectionTo)
                yield return new ValidationResult(
                    "La fecha de recolección inicial es posterior a la final.", new[] { nameof(CollectionFrom) });

            if (OrderBy != null && !OrdersByCollectionDate &&
                !string.Equals(OrderBy, "requestDate", StringComparison.OrdinalIgnoreCase))
                yield return new ValidationResult(
                    "Solo se puede ordenar por requestDate o collectionDate.", new[] { nameof(OrderBy) });

            if (Direction != null && !Ascending &&
                !string.Equals(Direction, "desc", StringComparison.OrdinalIgnoreCase))
                yield return new ValidationResult(
                    "El orden solo puede ser asc o desc.", new[] { nameof(Direction) });
        }
    }
}
