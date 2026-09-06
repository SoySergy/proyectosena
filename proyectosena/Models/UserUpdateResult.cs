namespace proyectosena.Models
{
    // Resultados posibles al actualizar el perfil o la contraseña de un usuario
    public enum UserUpdateResult
    {
        Success,

        UserNotFound,

        // Pidió cambiar la contraseña pero no envió la actual
        CurrentPasswordRequired,

        // La contraseña actual que envió no coincide
        CurrentPasswordIncorrect
    }
}
