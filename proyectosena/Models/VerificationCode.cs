namespace proyectosena.Models
{
    // Un código de un solo uso emitido y todavía sin quemar (B-6 · WA-12).
    // Antes vivía en un diccionario en memoria: al reiniciar la API se perdía
    // y quien tenía un código válido en el correo recibía «código inválido».
    public class VerificationCode
    {
        // Propósito + correo en minúsculas: el código de confirmar correo y el
        // de recuperar contraseña de la misma persona no se pisan.
        public string Key { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public DateTime Expiry { get; set; }

        public int FailedAttempts { get; set; }
    }
}
