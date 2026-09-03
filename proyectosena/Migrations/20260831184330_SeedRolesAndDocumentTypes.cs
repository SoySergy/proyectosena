using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace proyectosena.Migrations
{
    /// <inheritdoc />
    public partial class SeedRolesAndDocumentTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DocumentType",
                columns: new[] { "IdDocumentType", "Abbreviation", "DocumentName" },
                values: new object[,]
                {
                    { new Guid("5bcb367c-1f41-4c5a-b120-f01f35159dd8"), "CE", "Cédula de extranjería" },
                    { new Guid("63d5f1a7-6c0c-4a05-adf8-d65964d2b3b1"), "CC", "Cédula de ciudadanía" },
                    { new Guid("d0000000-0000-0000-0000-000000000004"), "TI", "Tarjeta de identidad" },
                    { new Guid("fcccb874-75f3-4374-b8e5-a7a92b084d6c"), "PA", "Pasaporte" }
                });

            migrationBuilder.InsertData(
                table: "Role",
                columns: new[] { "IdRole", "RoleDescription", "RoleName" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "User with full access to the system.", "Administrator" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "User responsible for managing collection requests.", "Manager" },
                    { new Guid("7d759012-4d17-46e8-bce8-d74fdf171eab"), "User who can submit collection requests.", "Citizen" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DocumentType",
                keyColumn: "IdDocumentType",
                keyValue: new Guid("5bcb367c-1f41-4c5a-b120-f01f35159dd8"));

            migrationBuilder.DeleteData(
                table: "DocumentType",
                keyColumn: "IdDocumentType",
                keyValue: new Guid("63d5f1a7-6c0c-4a05-adf8-d65964d2b3b1"));

            migrationBuilder.DeleteData(
                table: "DocumentType",
                keyColumn: "IdDocumentType",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "DocumentType",
                keyColumn: "IdDocumentType",
                keyValue: new Guid("fcccb874-75f3-4374-b8e5-a7a92b084d6c"));

            migrationBuilder.DeleteData(
                table: "Role",
                keyColumn: "IdRole",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Role",
                keyColumn: "IdRole",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Role",
                keyColumn: "IdRole",
                keyValue: new Guid("7d759012-4d17-46e8-bce8-d74fdf171eab"));
        }
    }
}
