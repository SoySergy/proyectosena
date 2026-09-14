using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyectosena.Migrations
{
    /// <inheritdoc />
    public partial class PersistirCodigosYTokensAnulados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RevokedToken",
                columns: table => new
                {
                    TokenId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevokedToken", x => x.TokenId);
                });

            migrationBuilder.CreateTable(
                name: "UserTokenRevocation",
                columns: table => new
                {
                    IdUser = table.Column<Guid>(type: "uuid", nullable: false),
                    RevokedBefore = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokenRevocation", x => x.IdUser);
                    table.ForeignKey(
                        name: "FK_UserTokenRevocation_User",
                        column: x => x.IdUser,
                        principalTable: "Users",
                        principalColumn: "IdUser",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VerificationCode",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationCode", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RevokedToken_ExpiresAt",
                table: "RevokedToken",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCode_Expiry",
                table: "VerificationCode",
                column: "Expiry");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RevokedToken");

            migrationBuilder.DropTable(
                name: "UserTokenRevocation");

            migrationBuilder.DropTable(
                name: "VerificationCode");
        }
    }
}
