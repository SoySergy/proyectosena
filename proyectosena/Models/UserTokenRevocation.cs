namespace proyectosena.Models
{
    // «Todo token de esta persona emitido antes de RevokedBefore ya no vale»:
    // se escribe al darla de baja o al cambiar su contraseña (B-6 · WA-12).
    //
    // Tabla aparte y no una columna en Users a propósito: UserRepository.UpdateUser
    // usa Users.Update(), que reescribe todas las columnas con lo que se leyó.
    // Si la baja de un administrador cayera entre la lectura y el guardado de un
    // cambio de perfil, ese guardado devolvería la marca a su valor viejo y la
    // sesión anulada volvería a valer.
    public class UserTokenRevocation
    {
        public Guid IdUser { get; set; }

        public DateTime RevokedBefore { get; set; }
    }
}
