using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace proyectosena.Extensions
{
    /// <summary>
    /// Traduce errores de la base de datos a preguntas que el negocio entiende.
    /// </summary>
    public static class DbExceptionExtensions
    {
        // PostgreSQL usa un código estándar de cinco caracteres (SQLSTATE) para
        // cada tipo de error. El 23505 es "unique_violation": alguien intentó
        // guardar un valor que ya existe, sea por una constraint UNIQUE, por la
        // clave primaria o por un índice único.
        //
        // Con SQL Server esto eran dos números distintos (2627 y 2601) porque
        // separaba la constraint del índice; PostgreSQL los junta en uno.
        private const string UniqueViolation = "23505";

        /// <summary>
        /// True si el guardado falló porque el valor ya existe en la base.
        /// </summary>
        /// <remarks>
        /// Antes estos códigos estaban escritos a mano en <c>AuthService</c> y en
        /// <c>AdminController</c>. Uno mal copiado no rompe la compilación:
        /// simplemente el error deja de reconocerse y el usuario recibe un 500 en vez
        /// del mensaje que le explica qué pasó.
        /// </remarks>
        public static bool IsDuplicateKey(this DbUpdateException ex)
            => ex.InnerException is PostgresException pgEx
               && pgEx.SqlState == UniqueViolation;
    }
}
