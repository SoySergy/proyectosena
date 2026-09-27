using System.ComponentModel.DataAnnotations;
using proyectosena.Models;

namespace proyectosena.DTOs.User
{
    // Filtros de GetUsers. Todos son opcionales y se combinan entre sí; sin
    // ninguno, la respuesta es la de siempre: los usuarios activos por nombre.
    // Llegan por la query, por ejemplo ?role=Manager&state=all&search=ana
    public class UserFilterDto : IValidatableObject
    {
        // Citizen, Manager o Administrator
        [MaxLength(20)]
        public string? Role { get; set; }

        // active (por defecto), inactive o all. Quien está dado de baja no
        // aparece salvo que se pida: el listado de siempre son los activos.
        [MaxLength(10)]
        public string? State { get; set; }

        // Con el correo confirmado o sin confirmar; sin valor, los dos
        public bool? EmailVerified { get; set; }

        // Nombre, apellido, correo o número de documento, sin distinguir mayúsculas
        [MaxLength(100)]
        public string? Search { get; set; }

        // name (por defecto) o registrationDate
        public string? OrderBy { get; set; }

        // asc (por defecto) o desc
        public string? Direction { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public const string StateActive = "active";
        public const string StateInactive = "inactive";
        public const string StateAll = "all";

        private static readonly string[] ValidStates = { StateActive, StateInactive, StateAll };

        // null significa "los dos", y es lo que pide state=all
        public bool? IsActive => Is(StateAll) ? null : !Is(StateInactive);

        public bool OrdersByRegistrationDate =>
            string.Equals(OrderBy, "registrationDate", StringComparison.OrdinalIgnoreCase);

        // El orden por defecto es el de siempre: por nombre, de la A a la Z
        public bool Ascending =>
            !string.Equals(Direction, "desc", StringComparison.OrdinalIgnoreCase);

        private bool Is(string state) => string.Equals(State, state, StringComparison.OrdinalIgnoreCase);

        // [ApiController] responde 400 con estos mensajes antes de llegar al
        // controlador: un filtro mal escrito no puede pasar por "sin filtro".
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Role != null && !RoleNames.All.Contains(Role, StringComparer.OrdinalIgnoreCase))
                yield return new ValidationResult(
                    $"El rol «{Role}» no existe.", new[] { nameof(Role) });

            if (State != null && !ValidStates.Contains(State, StringComparer.OrdinalIgnoreCase))
                yield return new ValidationResult(
                    "El estado solo puede ser active, inactive o all.", new[] { nameof(State) });

            if (OrderBy != null && !OrdersByRegistrationDate &&
                !string.Equals(OrderBy, "name", StringComparison.OrdinalIgnoreCase))
                yield return new ValidationResult(
                    "Solo se puede ordenar por name o registrationDate.", new[] { nameof(OrderBy) });

            // Ascending no sirve para validar: cualquier texto distinto de "desc"
            // se toma como ascendente, así que hay que mirar el valor tal cual.
            if (Direction != null &&
                !string.Equals(Direction, "asc", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Direction, "desc", StringComparison.OrdinalIgnoreCase))
                yield return new ValidationResult(
                    "El orden solo puede ser asc o desc.", new[] { nameof(Direction) });
        }
    }
}
