using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotesAndFileBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteAttachmentAndGeneratorUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ObjectKey",
                table: "NoteAttachments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "MimeType",
                table: "NoteAttachments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Filename",
                table: "NoteAttachments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Checksum",
                table: "NoteAttachments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "AttachmentType",
                table: "NoteAttachments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "NoteAttachments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "DurationSeconds",
                table: "NoteAttachments",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "NoteAttachments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "NoteAttachments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailObjectKey",
                table: "NoteAttachments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "NoteAttachments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "note_command_generators",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Script",
                table: "note_command_generators",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NoteAttachments_OwnerUserId",
                table: "NoteAttachments",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_NoteAttachments_Users_OwnerUserId",
                table: "NoteAttachments",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NoteAttachments_Users_OwnerUserId",
                table: "NoteAttachments");

            migrationBuilder.DropIndex(
                name: "IX_NoteAttachments_OwnerUserId",
                table: "NoteAttachments");

            migrationBuilder.DropColumn(
                name: "AttachmentType",
                table: "NoteAttachments");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "NoteAttachments");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "NoteAttachments");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "NoteAttachments");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "NoteAttachments");

            migrationBuilder.DropColumn(
                name: "ThumbnailObjectKey",
                table: "NoteAttachments");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "NoteAttachments");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "note_command_generators");

            migrationBuilder.DropColumn(
                name: "Script",
                table: "note_command_generators");

            migrationBuilder.AlterColumn<string>(
                name: "ObjectKey",
                table: "NoteAttachments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "MimeType",
                table: "NoteAttachments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Filename",
                table: "NoteAttachments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Checksum",
                table: "NoteAttachments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);
        }
    }
}
