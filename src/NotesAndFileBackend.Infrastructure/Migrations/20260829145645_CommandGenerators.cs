using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotesAndFileBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommandGenerators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "note_command_generators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ToolName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Template = table.Column<string>(type: "text", nullable: false),
                    SchemaJsonb = table.Column<string>(type: "jsonb", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "note_command_generators");
        }
    }
}
