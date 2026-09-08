namespace proyectosena.Models
{
    // Para qué se emitió un código de un solo uso. Un código solo sirve para su
    // propósito: el que confirma un correo no puede cambiar una contraseña.
    public enum CodePurpose
    {
        // Recuperar la contraseña. La invitación de gestor comparte este propósito
        // a propósito: su correo remite a «¿Olvidaste tu contraseña?» si expira.
        AccountAccess,

        // Confirmar que el correo del registro existe y es de quien se registró
        EmailConfirmation
    }
}
