using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace proyectosena.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentType",
                columns: table => new
                {
                    IdDocumentType = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    DocumentName = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Abbreviation = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentType", x => x.IdDocumentType);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    IdRole = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    RoleName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RoleDescription = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.IdRole);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    IdUser = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    IdRole = table.Column<Guid>(type: "uuid", nullable: false),
                    IdDocumentType = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: false),
                    LastName = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
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

            migrationBuilder.CreateTable(
                name: "CollectionRequest",
                columns: table => new
                {
                    IdRequest = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    IdUser = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CollectionTime = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CollectionAddress = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CurrentStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    RequestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    WasteTypes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CitizenObservations = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionRequest", x => x.IdRequest);
                    table.ForeignKey(
                        name: "FK_CollectionRequest_User",
                        column: x => x.IdUser,
                        principalTable: "Users",
                        principalColumn: "IdUser");
                });

            migrationBuilder.CreateTable(
                name: "ManagerApplication",
                columns: table => new
                {
                    IdApplication = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    IdUser = table.Column<Guid>(type: "uuid", nullable: false),
                    IdReviewer = table.Column<Guid>(type: "uuid", nullable: true),
                    Motivation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    RequestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ReviewDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
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

            migrationBuilder.CreateTable(
                name: "ChatHistory",
                columns: table => new
                {
                    IdChatHistory = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    IdRequest = table.Column<Guid>(type: "uuid", nullable: false),
                    IdSender = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SendDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatHistory", x => x.IdChatHistory);
                    table.ForeignKey(
                        name: "FK_ChatHistory_CollectionRequest",
                        column: x => x.IdRequest,
                        principalTable: "CollectionRequest",
                        principalColumn: "IdRequest",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatHistory_User",
                        column: x => x.IdSender,
                        principalTable: "Users",
                        principalColumn: "IdUser");
                });

            migrationBuilder.CreateTable(
                name: "CollectionManagement",
                columns: table => new
                {
                    IdManagement = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    IdRequest = table.Column<Guid>(type: "uuid", nullable: false),
                    IdManager = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StatusChangeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ManagerObservations = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
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

            migrationBuilder.CreateTable(
                name: "History",
                columns: table => new
                {
                    IdHistory = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    IdRequest = table.Column<Guid>(type: "uuid", nullable: false),
                    IdUser = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChangeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
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

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    IdNotification = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    IdUser = table.Column<Guid>(type: "uuid", nullable: true),
                    IdRequest = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.IdNotification);
                    table.ForeignKey(
                        name: "FK_Notification_CollectionRequest",
                        column: x => x.IdRequest,
                        principalTable: "CollectionRequest",
                        principalColumn: "IdRequest",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notification_User",
                        column: x => x.IdUser,
                        principalTable: "Users",
                        principalColumn: "IdUser",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DocumentType",
                columns: new[] { "IdDocumentType", "Abbreviation", "DocumentName" },
                values: new object[,]
                {
                    { new Guid("5bcb367c-1f41-4c5a-b120-f01f35159dd8"), "CE", "Cédula de extranjería" },
                    { new Guid("63d5f1a7-6c0c-4a05-adf8-d65964d2b3b1"), "CC", "Cédula de ciudadanía" },
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

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistory_IdRequest",
                table: "ChatHistory",
                column: "IdRequest");

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistory_IdSender",
                table: "ChatHistory",
                column: "IdSender");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionManagement_IdManager",
                table: "CollectionManagement",
                column: "IdManager");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionManagement_IdRequest",
                table: "CollectionManagement",
                column: "IdRequest");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionRequest_IdUser",
                table: "CollectionRequest",
                column: "IdUser");

            migrationBuilder.CreateIndex(
                name: "IX_History_IdRequest",
                table: "History",
                column: "IdRequest");

            migrationBuilder.CreateIndex(
                name: "IX_History_IdUser",
                table: "History",
                column: "IdUser");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerApplication_IdReviewer",
                table: "ManagerApplication",
                column: "IdReviewer");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerApplication_User_Status",
                table: "ManagerApplication",
                columns: new[] { "IdUser", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_IdRequest",
                table: "Notification",
                column: "IdRequest");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_IdUser",
                table: "Notification",
                column: "IdUser");

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
                name: "ChatHistory");

            migrationBuilder.DropTable(
                name: "CollectionManagement");

            migrationBuilder.DropTable(
                name: "History");

            migrationBuilder.DropTable(
                name: "ManagerApplication");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "CollectionRequest");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "DocumentType");

            migrationBuilder.DropTable(
                name: "Role");
        }
    }
}
