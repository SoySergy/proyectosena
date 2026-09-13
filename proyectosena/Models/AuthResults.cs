namespace proyectosena.Models
{
    // Register ya no devuelve un resultado distinto según el motivo (WA-03):
    // que el correo ya exista, que el documento ya exista o que se cree de
    // verdad responden exactamente igual, para no confirmarle a quien llama
    // qué cuentas existen. Ver AuthService.Register.

    // Resultados posibles al iniciar sesión.
    // Solo hay un motivo de fallo a propósito: distinguir «no existe» de
    // «contraseña incorrecta» o «cuenta inactiva» le confirma a un atacante
    // qué correos están registrados en el sistema.
    public enum LoginResult
    {
        Success,

        InvalidCredentials,

        // Las credenciales son correctas pero nunca confirmó el correo. Se
        // distingue de InvalidCredentials a propósito: si no, quien se registra
        // y no confirma queda afuera sin saber por qué.
        EmailNotVerified
    }

    // Resultados posibles al confirmar el correo de una cuenta recién creada
    public enum EmailVerificationResult
    {
        Success,

        InvalidOrExpiredCode,

        UserNotFound,

        // Ya lo había confirmado: no es un error, pero no hay nada que hacer
        AlreadyVerified
    }

    // Resultados posibles al restablecer la contraseña con el código del correo
    public enum ResetPasswordResult
    {
        Success,

        InvalidOrExpiredCode,

        UserNotFound
    }
}
