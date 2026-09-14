using System.ComponentModel.DataAnnotations;

namespace proyectosena.DTOs.Requests
{
    // Rechaza una fecha de recolección claramente pasada (WA-09). El margen
    // de un día es a propósito: CollectionDate se guarda tal cual la manda
    // el cliente, sin pasar por UTC (BL-02, para no correr el día según la
    // zona horaria de quien pide). Comparar contra la fecha UTC exacta de
    // "ahora" volvería a meter ese mismo problema cerca de la medianoche;
    // un día de margen basta para bloquear lo que de verdad importa —pedir
    // para la semana o el mes pasado— sin rechazar un "hoy" legítimo.
    public class FechaNoPasadaAttribute : ValidationAttribute
    {
        public FechaNoPasadaAttribute()
            : base("La fecha de recolección no puede ser una fecha ya pasada.")
        {
        }

        // null es válido a propósito: en UpdateCollectionRequestDto significa
        // "no tocar este campo", igual que el resto de propiedades opcionales
        // del DTO. [Required] es quien exige el valor donde hace falta.
        public override bool IsValid(object? value)
            => value is not DateTime fecha || fecha.Date >= DateTime.UtcNow.Date.AddDays(-1);
    }
}
