using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyectosena.Migrations
{
    /// <inheritdoc />
    public partial class CreateCollectionManagementTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectionManagement",
                columns: table => new
                {
                    IdManagement = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    IdRequest = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdManager = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StatusChangeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManagerObservations = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionManagement", x => x.IdManagement);
                    table.ForeignKey(
                        name: "FK_CollectionManagement_CollectionRequest",
                        column: x => x.IdRequest,
                        principalTable: "CollectionRequest",
                        principalColumn: "IdRequest");
                    table.ForeignKey(
                        name: "FK_CollectionManagement_User",
                        column: x => x.IdManager,
                        principalTable: "Users",
                        principalColumn: "IdUser");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionManagement_IdManager",
                table: "CollectionManagement",
                column: "IdManager");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionManagement_IdRequest",
                table: "CollectionManagement",
                column: "IdRequest");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionManagement");
        }
    }
}
