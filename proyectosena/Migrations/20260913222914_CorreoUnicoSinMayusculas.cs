using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyectosena.Migrations
{
    /// <inheritdoc />
    public partial class CorreoUnicoSinMayusculas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_User_Email",
                table: "Users");

            // Único sobre LOWER("Email"): dos correos que solo difieren en
            // mayúsculas ya no pueden coexistir. La búsqueda de la app
            // (GetUserByEmail) ya comparaba así; ahora la base lo exige
            // también, y cierra la carrera de dos registros simultáneos que
            // antes podían colarse ambos (WA-04).
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"UQ_User_Email_Lower\" ON \"Users\" (LOWER(\"Email\"));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX \"UQ_User_Email_Lower\";");

            migrationBuilder.CreateIndex(
                name: "UQ_User_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }
    }
}
