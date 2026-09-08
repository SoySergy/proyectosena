using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace proyectosena.Extensions
{
    /// <summary>
    /// Traduce errores de la base de datos a preguntas que el negocio entiende.
    /// </summary>
    public static class DbExceptionExtensions
    {
        // SQL Server devuelve 2627 cuando se viola una constraint UNIQUE o PRIMARY KEY,
        // y 2601 cuando se viola un índice único. Para el negocio es lo mismo:
        // alguien intentó guardar un valor que ya existe.
        private const int UniqueConstraintViolation = 2627;
        private const int DuplicateKeyInIndex = 2601;

        /// <summary>
        /// True si el guardado falló porque el valor ya existe en la base.
        /// </summary>
        /// <remarks>
        /// Antes estos dos números estaban escritos a mano en <c>AuthService</c> y en
        /// <c>AdminController</c>. Un número mal copiado no rompe la compilación:
        /// simplemente el error deja de reconocerse y el usuario recibe un 500 en vez
        /// del mensaje que le explica qué pasó.
        /// </remarks>
        public static bool IsDuplicateKey(this DbUpdateException ex)
            => ex.InnerException is SqlException sqlEx
               && (sqlEx.Number == UniqueConstraintViolation || sqlEx.Number == DuplicateKeyInIndex);
    }
}
