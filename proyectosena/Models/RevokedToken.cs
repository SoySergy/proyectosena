namespace proyectosena.Models
{
    // Un token anulado al cerrar sesión antes de su fecha (B-6 · WA-12). En
    // memoria, un reinicio de la API lo devolvía a la vida hasta caducar.
    public class RevokedToken
    {
        // El claim "jti" del token
        public string TokenId { get; set; } = string.Empty;

        // Cuándo habría caducado solo. Pasada esa fecha la fila sobra: el
        // propio validador lo rechaza por vencido.
        public DateTime ExpiresAt { get; set; }
    }
}
