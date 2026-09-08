using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyectosena.Migrations
{
    /// <inheritdoc />
    public partial class AddIsEmailVerifiedToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Las cuentas que ya existían se dan por confirmadas: no se les puede
            // exigir un paso que no existía cuando se registraron. Sin esto, al
            // activar la restricción quedarían fuera todas, incluida la del
            // administrador. Solo afecta a las filas presentes en este momento;
            // las nuevas nacen en false por el valor por defecto de la columna.
            migrationBuilder.Sql("UPDATE [Users] SET [IsEmailVerified] = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "Users");
        }
    }
}
