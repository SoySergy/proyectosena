using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyectosena.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerApplicationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManagerApplication",
                columns: table => new
                {
                    IdApplication = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    IdUser = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdReviewer = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Motivation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ReviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerApplication", x => x.IdApplication);
                    table.ForeignKey(
                        name: "FK_ManagerApplication_Reviewer",
                        column: x => x.IdReviewer,
                        principalTable: "Users",
                        principalColumn: "IdUser");
                    table.ForeignKey(
                        name: "FK_ManagerApplication_User",
                        column: x => x.IdUser,
                        principalTable: "Users",
                        principalColumn: "IdUser");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerApplication_IdReviewer",
                table: "ManagerApplication",
                column: "IdReviewer");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerApplication_User_Status",
                table: "ManagerApplication",
                columns: new[] { "IdUser", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagerApplication");
        }
    }
}
