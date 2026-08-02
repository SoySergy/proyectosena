using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyectosena.Migrations
{
    /// <inheritdoc />
    public partial class CreateUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    IdUser = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    IdRole = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdDocumentType = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.IdUser);
                    table.ForeignKey(
                        name: "FK_User_DocType",
                        column: x => x.IdDocumentType,
                        principalTable: "DocumentType",
                        principalColumn: "IdDocumentType");
                    table.ForeignKey(
                        name: "FK_User_Role",
                        column: x => x.IdRole,
                        principalTable: "Role",
                        principalColumn: "IdRole");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_IdRole",
                table: "Users",
                column: "IdRole");

            migrationBuilder.CreateIndex(
                name: "UQ_User_Document",
                table: "Users",
                columns: new[] { "IdDocumentType", "DocumentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_User_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
