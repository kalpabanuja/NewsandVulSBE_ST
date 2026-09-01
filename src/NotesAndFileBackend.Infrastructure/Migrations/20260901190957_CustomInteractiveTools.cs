using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotesAndFileBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CustomInteractiveTools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "note_command_generators");

            migrationBuilder.CreateTable(
                name: "custom_interactive_tools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    HtmlSource = table.Column<string>(type: "text", nullable: false),
                    CssSource = table.Column<string>(type: "text", nullable: false),
                    JavascriptSource = table.Column<string>(type: "text", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    AssetVersion = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ValidationStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SecurityStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custom_interactive_tools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_custom_interactive_tools_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_custom_interactive_tools_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_custom_interactive_tools_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_custom_interactive_tools_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_custom_interactive_tools_ContentHash",
                table: "custom_interactive_tools",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_custom_interactive_tools_CreatedByUserId",
                table: "custom_interactive_tools",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_custom_interactive_tools_NoteId",
                table: "custom_interactive_tools",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_custom_interactive_tools_NoteId_IsDeleted",
                table: "custom_interactive_tools",
                columns: new[] { "NoteId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_custom_interactive_tools_NoteId_Name",
                table: "custom_interactive_tools",
                columns: new[] { "NoteId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_custom_interactive_tools_OwnerUserId",
                table: "custom_interactive_tools",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_custom_interactive_tools_UpdatedByUserId",
                table: "custom_interactive_tools",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "custom_interactive_tools");

            migrationBuilder.CreateTable(
                name: "note_command_generators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SchemaJsonb = table.Column<string>(type: "jsonb", nullable: false),
                    Script = table.Column<string>(type: "text", nullable: true),
                    Template = table.Column<string>(type: "text", nullable: false),
                    ToolName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note_command_generators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_note_command_generators_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_note_command_generators_NoteId",
                table: "note_command_generators",
                column: "NoteId");
        }
    }
}
