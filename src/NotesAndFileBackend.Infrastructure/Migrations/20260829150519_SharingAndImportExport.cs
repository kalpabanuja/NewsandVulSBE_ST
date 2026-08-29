using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotesAndFileBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SharingAndImportExport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowIndexing",
                table: "PublicNoteShares",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxViews",
                table: "PublicNoteShares",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "PublicNoteShares",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "PublicNoteShares",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "imports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TotalItems = table.Column<int>(type: "integer", nullable: true),
                    Processed = table.Column<int>(type: "integer", nullable: false),
                    Failed = table.Column<int>(type: "integer", nullable: false),
                    ErrorJsonb = table.Column<string>(type: "jsonb", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_imports_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_imports_UserId",
                table: "imports",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "imports");

            migrationBuilder.DropColumn(
                name: "AllowIndexing",
                table: "PublicNoteShares");

            migrationBuilder.DropColumn(
                name: "MaxViews",
                table: "PublicNoteShares");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "PublicNoteShares");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "PublicNoteShares");
        }
    }
}
