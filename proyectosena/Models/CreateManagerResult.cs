namespace proyectosena.Models
{
    // Resultados posibles al crear un gestor desde el panel de administración
    public enum CreateManagerResult
    {
        Success,

        EmailAlreadyUsed,

        // Ese número ya existe con ese mismo tipo de documento
        DocumentAlreadyUsed,

        // La base rechazó el INSERT por índice único: dos administradores
        // creando el mismo gestor a la vez pasan ambos la comprobación previa.
        DuplicateOnSave
    }
}
