namespace proyectosena.Models
{
    // Resultados posibles al registrar un usuario nuevo
    public enum RegisterResult
    {
        Success,

        EmailAlreadyUsed,

        // Ese número ya existe con ese mismo tipo de documento
        DocumentAlreadyUsed,

        // La base rechazó el INSERT por índice único. Ocurre cuando dos personas
        // registran el mismo correo o documento a la vez: ambas pasan la
        // comprobación previa y la base es la que decide.
        DuplicateOnSave
    }

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
