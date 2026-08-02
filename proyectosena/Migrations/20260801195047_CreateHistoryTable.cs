using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyectosena.Migrations
{
    /// <inheritdoc />
    public partial class CreateHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "History",
                columns: table => new
                {
                    IdHistory = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    IdRequest = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdUser = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChangeDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_History", x => x.IdHistory);
                    table.ForeignKey(
                        name: "FK_History_CollectionRequest",
                        column: x => x.IdRequest,
                        principalTable: "CollectionRequest",
                        principalColumn: "IdRequest",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_History_User",
                        column: x => x.IdUser,
                        principalTable: "Users",
                        principalColumn: "IdUser");
                });

            migrationBuilder.CreateIndex(
                name: "IX_History_IdRequest",
                table: "History",
                column: "IdRequest");

            migrationBuilder.CreateIndex(
                name: "IX_History_IdUser",
                table: "History",
                column: "IdUser");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "History");
        }
    }
}
