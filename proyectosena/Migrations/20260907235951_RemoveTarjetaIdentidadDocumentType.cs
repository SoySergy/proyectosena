using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyectosena.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTarjetaIdentidadDocumentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DocumentType",
                keyColumn: "IdDocumentType",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000004"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DocumentType",
                columns: new[] { "IdDocumentType", "Abbreviation", "DocumentName" },
                values: new object[] { new Guid("d0000000-0000-0000-0000-000000000004"), "TI", "Tarjeta de identidad" });
        }
    }
}
