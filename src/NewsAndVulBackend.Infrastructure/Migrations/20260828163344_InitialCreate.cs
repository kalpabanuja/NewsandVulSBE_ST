using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsAndVulBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    ResourceType = table.Column<string>(type: "text", nullable: false),
                    ResourceId = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: false),
                    IpHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    EmailVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceName = table.Column<string>(type: "text", nullable: false),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    AppVersion = table.Column<string>(type: "text", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerDeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Visibility = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Devices_OwnerDeviceId",
                        column: x => x.OwnerDeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Documents_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerDeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginalFilename = table.Column<string>(type: "text", nullable: false),
                    StoredFilename = table.Column<string>(type: "text", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: false),
                    Extension = table.Column<string>(type: "text", nullable: false),
                    ByteSize = table.Column<long>(type: "bigint", nullable: false),
                    Checksum = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RetentionExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StorageBackend = table.Column<string>(type: "text", nullable: false),
                    UploadSessionId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Files_Devices_OwnerDeviceId",
                        column: x => x.OwnerDeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Files_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockId = table.Column<Guid>(type: "uuid", nullable: true),
                    ObjectKey = table.Column<string>(type: "text", nullable: false),
                    Filename = table.Column<string>(type: "text", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: false),
                    ByteSize = table.Column<long>(type: "bigint", nullable: false),
                    Checksum = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentAttachments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockType = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    ContentJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentBlocks_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PublicDocumentShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicDocumentShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicDocumentShares_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublicDocumentShares_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessType = table.Column<string>(type: "text", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileAccesses_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileAccesses_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PublicFileShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AccessCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicFileShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicFileShares_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublicFileShares_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_UserId",
                table: "Devices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAttachments_DocumentId",
                table: "DocumentAttachments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentBlocks_DocumentId",
                table: "DocumentBlocks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_OwnerDeviceId",
                table: "Documents",
                column: "OwnerDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_OwnerUserId",
                table: "Documents",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FileAccesses_FileId",
                table: "FileAccesses",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_FileAccesses_TargetUserId",
                table: "FileAccesses",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_OwnerDeviceId",
                table: "Files",
                column: "OwnerDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_OwnerUserId",
                table: "Files",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicDocumentShares_CreatedByUserId",
                table: "PublicDocumentShares",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicDocumentShares_DocumentId",
                table: "PublicDocumentShares",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicDocumentShares_TokenHash",
                table: "PublicDocumentShares",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicFileShares_CreatedByUserId",
                table: "PublicFileShares",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicFileShares_FileId",
                table: "PublicFileShares",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicFileShares_TokenHash",
                table: "PublicFileShares",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "DocumentAttachments");

            migrationBuilder.DropTable(
                name: "DocumentBlocks");

            migrationBuilder.DropTable(
                name: "FileAccesses");

            migrationBuilder.DropTable(
                name: "PublicDocumentShares");

            migrationBuilder.DropTable(
                name: "PublicFileShares");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
