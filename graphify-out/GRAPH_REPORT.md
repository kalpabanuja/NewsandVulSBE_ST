# Graph Report - NewsandVulSBE_ST  (2026-08-29)

## Corpus Check
- Corpus is ~44,907 words - fits in a single context window. You may not need a graph.

## Summary
- 407 nodes · 592 edges · 39 communities (36 shown, 3 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 32 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Auth API
- Core Entities
- API Core
- Core Entities
- API Controllers
- Core Entities
- Core Infrastructure
- API Controllers
- API Controllers
- Core Entities
- API Core
- API DTOs
- Migrations Infrastructure
- Core Entities
- API DTOs
- Core Entities
- API
- API
- Infrastructure Migrations
- API
- Infrastructure
- Core

## God Nodes (most connected - your core abstractions)
1. `AppDbContext` - 31 edges
2. `StoredFile` - 29 edges
3. `Document` - 26 edges
4. `AuditEvent` - 20 edges
5. `NotesAndFileBackend.Core.Entities` - 18 edges
6. `Device` - 18 edges
7. `PublicFileShare` - 18 edges
8. `PublicDocumentShare` - 17 edges
9. `BaseEntity` - 16 edges
10. `FilesController` - 13 edges

## Surprising Connections (you probably didn't know these)
- `AdminController` --references--> `AppDbContext`  [EXTRACTED]
  src/NotesAndFileBackend.Api/Controllers/AdminController.cs → src/NotesAndFileBackend.Infrastructure/Data/AppDbContext.cs
- `AuthController` --references--> `AppDbContext`  [EXTRACTED]
  src/NotesAndFileBackend.Api/Controllers/AuthController.cs → src/NotesAndFileBackend.Infrastructure/Data/AppDbContext.cs
- `DocumentsController` --references--> `AppDbContext`  [EXTRACTED]
  src/NotesAndFileBackend.Api/Controllers/DocumentsController.cs → src/NotesAndFileBackend.Infrastructure/Data/AppDbContext.cs
- `FilesController` --references--> `IStorageService`  [EXTRACTED]
  src/NotesAndFileBackend.Api/Controllers/FilesController.cs → src/NotesAndFileBackend.Core/Interfaces/IStorageService.cs
- `FilesController` --references--> `AppDbContext`  [EXTRACTED]
  src/NotesAndFileBackend.Api/Controllers/FilesController.cs → src/NotesAndFileBackend.Infrastructure/Data/AppDbContext.cs

## Import Cycles
- None detected.

## Communities (39 total, 3 thin omitted)

### Community 0 - "Auth API"
Cohesion: 0.06
Nodes (35): Authorize, IConfiguration, HttpPost, HttpPut, IActionResult, ILogger, Task, AuthController (+27 more)

### Community 1 - "Core Entities"
Cohesion: 0.05
Nodes (44): DbContext, DbSet, User, DateTime, Guid, ICollection, Document, Attachments (+36 more)

### Community 2 - "API Core"
Cohesion: 0.11
Nodes (11): NotesAndFileBackend.Infrastructure.Data, NotesAndFileBackend.Api.DTOs, NotesAndFileBackend.Infrastructure.Services, NotesAndFileBackend.Api.Services, NotesAndFileBackend.Core.Interfaces, NotesAndFileBackend.Api.Controllers, NotesAndFileBackend.Core.Entities, NotesAndFileBackend.Infrastructure.Migrations (+3 more)

### Community 3 - "Core Entities"
Cohesion: 0.07
Nodes (28): Guid, FileAccess, AccessType, File, FileId, TargetUser, TargetUserId, DateTime (+20 more)

### Community 4 - "API Controllers"
Cohesion: 0.18
Nodes (17): Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult, Task, DocumentsController (+9 more)

### Community 5 - "Core Entities"
Cohesion: 0.08
Nodes (23): DateTime, Guid, BaseEntity, CreatedAt, Id, UpdatedAt, Guid, DocumentBlock (+15 more)

### Community 6 - "Core Infrastructure"
Cohesion: 0.14
Nodes (13): IAmazonS3, HttpGet, IActionResult, Task, PublicController, Stream, Task, TimeSpan (+5 more)

### Community 7 - "API Controllers"
Cohesion: 0.10
Nodes (16): ControllerBase, NotesAndFileBackend.Api, DateOnly, IEnumerable, HttpGet, IActionResult, Task, AdminController (+8 more)

### Community 8 - "API Controllers"
Cohesion: 0.28
Nodes (10): DisableRequestSizeLimit, IFormFile, RequestFormLimits, Guid, HttpDelete, HttpGet, HttpPost, IActionResult (+2 more)

### Community 9 - "Core Entities"
Cohesion: 0.13
Nodes (14): AWSSDK.S3 (4.0.102.4), Microsoft.AspNetCore.Authentication.JwtBearer (10.0.11), Microsoft.AspNetCore.OpenApi (10.0.11), Microsoft.EntityFrameworkCore.Design (10.0.11), Microsoft.EntityFrameworkCore.Tools (10.0.11), Microsoft.Extensions.Configuration.Abstractions (10.0.11), Npgsql.EntityFrameworkCore.PostgreSQL (10.0.3), System.IdentityModel.Tokens.Jwt (8.22.0) (+6 more)

### Community 10 - "API Core"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 11 - "API DTOs"
Cohesion: 0.15
Nodes (12): JsonElement, CreateDocumentRequest, Description, Title, DocumentBlockDto, BlockType, ContentJson, Position (+4 more)

### Community 12 - "Migrations Infrastructure"
Cohesion: 0.18
Nodes (8): Migration, MigrationBuilder, DateTime, Guid, DateTime, Guid, ModelBuilder, InitialCreate

### Community 13 - "Core Entities"
Cohesion: 0.17
Nodes (12): DateTime, Guid, PublicFileShare, AccessCount, CreatedByUser, CreatedByUserId, ExpiresAt, File (+4 more)

### Community 14 - "API DTOs"
Cohesion: 0.18
Nodes (10): DateTime, Guid, CreateShareRequest, Alias, ExpiresInHours, ShareResponseDto, ExpiresAt, Id (+2 more)

### Community 15 - "Core Entities"
Cohesion: 0.18
Nodes (10): Guid, DocumentAttachment, BlockId, ByteSize, Checksum, Document, DocumentId, Filename (+2 more)

### Community 16 - "API"
Cohesion: 0.36
Nodes (6): BackgroundService, CancellationToken, ILogger, IServiceProvider, Task, ExpirationCleanupService

### Community 17 - "API"
Cohesion: 0.38
Nodes (4): ILogger, IServiceProvider, Task, AdminSeeder

### Community 18 - "Infrastructure Migrations"
Cohesion: 0.33
Nodes (5): ModelSnapshot, DateTime, Guid, ModelBuilder, AppDbContextModelSnapshot

## Knowledge Gaps
- **160 isolated node(s):** `Email`, `Password`, `DisplayName`, `DeviceName`, `Platform` (+155 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **3 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `AppDbContext` connect `Core Entities` to `Auth API`, `API Core`, `Core Entities`, `API Controllers`, `Core Entities`, `Core Infrastructure`, `API Controllers`, `API Controllers`, `Core Entities`, `Core Entities`, `API`, `API`?**
  _High betweenness centrality (0.298) - this node is a cross-community bridge._
- **Why does `StoredFile` connect `Core Entities` to `Auth API`, `Core Entities`, `API Core`, `Core Entities`, `API Controllers`, `Core Entities`?**
  _High betweenness centrality (0.102) - this node is a cross-community bridge._
- **Why does `Device` connect `Auth API` to `Core Entities`, `Core Entities`, `Core Entities`?**
  _High betweenness centrality (0.098) - this node is a cross-community bridge._
- **Are the 9 inferred relationships involving `AuditEvent` (e.g. with `.CreateDocument()` and `.DeleteDocument()`) actually correct?**
  _`AuditEvent` has 9 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Email`, `Password`, `DisplayName` to the rest of the system?**
  _160 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Auth API` be split into smaller, more focused modules?**
  _Cohesion score 0.06464646464646465 - nodes in this community are weakly interconnected._
- **Should `Core Entities` be split into smaller, more focused modules?**
  _Cohesion score 0.050505050505050504 - nodes in this community are weakly interconnected._