# Graph Report - NewsandVulSBE_ST  (2026-09-02)

## Corpus Check
- 193 files · ~96,384 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1009 nodes · 1663 edges · 84 communities (82 shown, 2 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 71 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Community 0
- Community 1
- Community 2
- Community 3
- Community 4
- Community 5
- Community 6
- Community 7
- Community 8
- Community 9
- Community 10
- Community 11
- Community 12
- Community 13
- Community 14
- Community 15
- Community 16
- Community 17
- Community 18
- Community 19
- Community 20
- Community 21
- Community 22
- Community 23
- Community 24
- Community 25
- Community 26
- Community 27
- Community 28
- Community 29
- Community 30
- Community 31
- Community 32
- Community 33
- Community 34
- Community 35
- Community 36
- Community 37
- Community 38
- Community 39
- Community 40
- Community 41
- Community 42
- Community 43
- Community 44
- Community 45
- Community 46
- Community 47
- Community 48
- Community 49
- Community 50
- Community 51
- Community 52
- Community 53
- Community 54
- Community 55
- Community 56
- Community 57
- Community 58
- Community 59
- Community 60
- Community 61
- Community 62
- Community 63
- Community 64

## God Nodes (most connected - your core abstractions)
1. `AppDbContext` - 48 edges
2. `Note` - 45 edges
3. `StoredFile` - 29 edges
4. `NotesAndFileBackend.Domain.Entities` - 28 edges
5. `AuditEvent` - 26 edges
6. `NotesController` - 24 edges
7. `PublicController` - 24 edges
8. `NoteAttachment` - 24 edges
9. `PublicNoteShare` - 21 edges
10. `NotesAndFileBackend.Infrastructure.Data` - 21 edges

## Surprising Connections (you probably didn't know these)
- `CommandTemplateRendererTests` --references--> `CommandTemplateRenderer`  [EXTRACTED]
  tests/NotesAndFileBackend.UnitTests/CommandTemplateRendererTests.cs → src/NotesAndFileBackend.Application/Services/CommandTemplateRenderer.cs
- `AdminController` --references--> `AppDbContext`  [EXTRACTED]
  src/NotesAndFileBackend.Api/Controllers/AdminController.cs → src/NotesAndFileBackend.Infrastructure/Data/AppDbContext.cs
- `AuthController` --references--> `ITokenService`  [EXTRACTED]
  src/NotesAndFileBackend.Api/Controllers/AuthController.cs → src/NotesAndFileBackend.Application/Interfaces/ITokenService.cs
- `AuthController` --references--> `AppDbContext`  [EXTRACTED]
  src/NotesAndFileBackend.Api/Controllers/AuthController.cs → src/NotesAndFileBackend.Infrastructure/Data/AppDbContext.cs
- `CategoriesController` --references--> `AppDbContext`  [EXTRACTED]
  src/NotesAndFileBackend.Api/Controllers/CategoriesController.cs → src/NotesAndFileBackend.Infrastructure/Data/AppDbContext.cs

## Import Cycles
- None detected.

## Communities (84 total, 2 thin omitted)

### Community 0 - "Community 0"
Cohesion: 0.05
Nodes (47): GenerationResult, IReadOnlyList, Dictionary, Guid, HttpDelete, HttpGet, HttpPost, HttpPut (+39 more)

### Community 1 - "Community 1"
Cohesion: 0.05
Nodes (49): DateTime, Guid, JsonElement, List, ExportedNote, Category, ContentJsonb, CreatedAt (+41 more)

### Community 2 - "Community 2"
Cohesion: 0.06
Nodes (28): ActionExecutingContext, ActionExecutionDelegate, ControllerBase, NotesAndFileBackend.Infrastructure.Data, NotesAndFileBackend.Api.DTOs, NotesAndFileBackend.Infrastructure.Services, NotesAndFileBackend.Api.Services, NotesAndFileBackend.Application.Models (+20 more)

### Community 3 - "Community 3"
Cohesion: 0.12
Nodes (25): NotesAndFileBackend.UnitTests.Services, ServiceFilter, CancellationToken, Guid, HttpDelete, HttpGet, HttpPost, HttpPut (+17 more)

### Community 4 - "Community 4"
Cohesion: 0.09
Nodes (31): DisableRequestSizeLimit, Guid, HttpDelete, HttpGet, HttpPost, IActionResult, IFormFile, RequestFormLimits (+23 more)

### Community 5 - "Community 5"
Cohesion: 0.12
Nodes (21): IAmazonS3, CancellationToken, DisableRequestSizeLimit, Guid, HashSet, HttpDelete, HttpGet, HttpPost (+13 more)

### Community 6 - "Community 6"
Cohesion: 0.08
Nodes (28): AWSSDK.S3 (4.0.102.4), BCrypt.Net-Next (4.2.0), coverlet.collector (6.0.4), FluentValidation.AspNetCore (11.3.1), Jint (3.1.2), Microsoft.AspNetCore.Authentication.JwtBearer (10.0.11), Microsoft.AspNetCore.OpenApi (10.0.11), Microsoft.EntityFrameworkCore.Design (10.0.11) (+20 more)

### Community 7 - "Community 7"
Cohesion: 0.06
Nodes (33): DateTime, Guid, ICollection, Note, Attachments, Category, CategoryId, ContentJsonb (+25 more)

### Community 8 - "Community 8"
Cohesion: 0.20
Nodes (10): HttpRequest, CancellationToken, DateTime, Guid, HttpGet, IActionResult, JsonElement, Task (+2 more)

### Community 9 - "Community 9"
Cohesion: 0.21
Nodes (11): JsonElement, Program, HashSet, JsonElement, List, Regex, NoteContentValidator, ValidationError (+3 more)

### Community 10 - "Community 10"
Cohesion: 0.11
Nodes (14): NotesAndFileBackend.Api.Middleware, HttpContext, RequestDelegate, Task, ClientVersionValidationMiddleware, HttpContext, ILogger, RequestDelegate (+6 more)

### Community 11 - "Community 11"
Cohesion: 0.15
Nodes (16): Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult, Task, CategoriesController (+8 more)

### Community 12 - "Community 12"
Cohesion: 0.10
Nodes (21): DateTime, Guid, ICollection, StoredFile, AccessList, ByteSize, Checksum, DeletedAt (+13 more)

### Community 13 - "Community 13"
Cohesion: 0.11
Nodes (19): DbContext, DbSet, AppDbContext, AuditEvents, Categories, Devices, FileAccesses, Files (+11 more)

### Community 14 - "Community 14"
Cohesion: 0.11
Nodes (18): Guid, NoteAttachment, AttachmentType, BlockId, ByteSize, Checksum, DisplayName, DurationSeconds (+10 more)

### Community 15 - "Community 15"
Cohesion: 0.12
Nodes (16): DateTime, Guid, NoteCommandGenerator, CreatedAt, Description, Id, IsEnabled, Language (+8 more)

### Community 16 - "Community 16"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 17 - "Community 17"
Cohesion: 0.12
Nodes (15): DateTime, Guid, PublicNoteShare, AllowIndexing, CreatedByUser, CreatedByUserId, ExpiresAt, LastAccessedAt (+7 more)

### Community 18 - "Community 18"
Cohesion: 0.16
Nodes (12): User, DateTime, Guid, Device, AppVersion, DeviceName, LastSeenAt, Platform (+4 more)

### Community 19 - "Community 19"
Cohesion: 0.13
Nodes (14): DateTime, ICollection, User, Devices, DisplayName, Email, EmailVerifiedAt, IsActive (+6 more)

### Community 20 - "Community 20"
Cohesion: 0.21
Nodes (7): NotesAndFileBackend.Infrastructure.Migrations, Migration, DateTime, Guid, MigrationBuilder, InitialCreate, FoundationRework

### Community 21 - "Community 21"
Cohesion: 0.15
Nodes (12): DateTime, Guid, PublicFileShare, AccessCount, CreatedByUser, CreatedByUserId, ExpiresAt, File (+4 more)

### Community 22 - "Community 22"
Cohesion: 0.33
Nodes (7): Authorize, HttpPost, HttpPut, IActionResult, ILogger, Task, AuthController

### Community 23 - "Community 23"
Cohesion: 0.18
Nodes (10): NotesAndFileBackend.Domain.Enums, List, CommandGenerationResultDto, Command, Errors, Success, ValidationErrorDto, Code (+2 more)

### Community 24 - "Community 24"
Cohesion: 0.17
Nodes (10): NotesAndFileBackend.Api, DateOnly, IEnumerable, HttpGet, WeatherForecast, Date, Summary, TemperatureC (+2 more)

### Community 25 - "Community 25"
Cohesion: 0.17
Nodes (11): SignInRequest, DeviceName, Email, Password, Platform, SignUpRequest, DeviceName, DisplayName (+3 more)

### Community 26 - "Community 26"
Cohesion: 0.17
Nodes (12): DateTime, NoteSearchItem, Category, Id, IsFavorite, IsPinned, MatchedBlocks, Summary (+4 more)

### Community 27 - "Community 27"
Cohesion: 0.17
Nodes (12): UpdateNoteRequest, CategoryId, Content, IsArchived, IsFavorite, IsPinned, Summary, Tags (+4 more)

### Community 28 - "Community 28"
Cohesion: 0.18
Nodes (7): DateTime, Guid, MigrationBuilder, DateTime, Guid, ModelBuilder, DatabasePersistence

### Community 29 - "Community 29"
Cohesion: 0.18
Nodes (7): DateTime, Guid, MigrationBuilder, DateTime, Guid, ModelBuilder, CommandGenerators

### Community 30 - "Community 30"
Cohesion: 0.18
Nodes (7): DateTime, Guid, MigrationBuilder, DateTime, Guid, ModelBuilder, SharingAndImportExport

### Community 31 - "Community 31"
Cohesion: 0.31
Nodes (5): InlineData, Regex, CommandFieldValidators, CommandFieldValidatorsTests, Theory

### Community 32 - "Community 32"
Cohesion: 0.18
Nodes (11): Guid, CreateNoteRequest, CategoryId, Content, IsFavorite, IsPinned, Summary, Tags (+3 more)

### Community 33 - "Community 33"
Cohesion: 0.18
Nodes (11): CommandFieldDefinition, Key, Label, Options, Placeholder, Presets, Required, Type (+3 more)

### Community 34 - "Community 34"
Cohesion: 0.18
Nodes (10): Guid, NoteRevision, ContentJsonb, EditedByUser, EditedByUserId, Note, NoteId, Summary (+2 more)

### Community 35 - "Community 35"
Cohesion: 0.20
Nodes (6): DateTime, MigrationBuilder, DateTime, Guid, ModelBuilder, AddRefreshToken

### Community 36 - "Community 36"
Cohesion: 0.20
Nodes (6): Guid, MigrationBuilder, DateTime, Guid, ModelBuilder, AddNoteAttachmentAndGeneratorUpdates

### Community 37 - "Community 37"
Cohesion: 0.27
Nodes (5): NotesAndFileBackend.Api.Helpers, NotesAndFileBackend.UnitTests.Helpers, TokenHelper, Fact, TokenHelperTests

### Community 38 - "Community 38"
Cohesion: 0.36
Nodes (5): InvalidOperationException, IReadOnlyDictionary, CommandTemplateRenderer, Fact, CommandTemplateRendererTests

### Community 39 - "Community 39"
Cohesion: 0.25
Nodes (7): Guid, NoteTag, Note, NoteId, Tag, TagId, ModelBuilder

### Community 40 - "Community 40"
Cohesion: 0.22
Nodes (9): CommandFieldType, Boolean, Integer, MultiSelect, PortSelector, Preset, Select, Target (+1 more)

### Community 41 - "Community 41"
Cohesion: 0.36
Nodes (6): BackgroundService, CancellationToken, ILogger, IServiceProvider, Task, ExpirationCleanupService

### Community 42 - "Community 42"
Cohesion: 0.50
Nodes (4): ILogger, IServiceProvider, Task, AdminSeeder

### Community 43 - "Community 43"
Cohesion: 0.25
Nodes (7): Guid, Category, Description, Name, Slug, SortOrder, UserId

### Community 44 - "Community 44"
Cohesion: 0.25
Nodes (7): Guid, FileAccess, AccessType, File, FileId, TargetUser, TargetUserId

### Community 45 - "Community 45"
Cohesion: 0.25
Nodes (7): Guid, NoteLink, BlockId, Note, NoteId, Title, Url

### Community 47 - "Community 47"
Cohesion: 0.29
Nodes (5): ModelSnapshot, DateTime, Guid, ModelBuilder, AppDbContextModelSnapshot

### Community 48 - "Community 48"
Cohesion: 0.29
Nodes (7): Guid, CommandGeneratorDefinition, Fields, Id, Name, Template, ToolName

### Community 49 - "Community 49"
Cohesion: 0.48
Nodes (4): Dictionary, JsonElement, CommandGeneratorService, ICommandGenerator

### Community 50 - "Community 50"
Cohesion: 0.29
Nodes (6): DateTime, Guid, BaseEntity, CreatedAt, Id, UpdatedAt

### Community 51 - "Community 51"
Cohesion: 0.29
Nodes (6): Guid, Tag, Name, Normalized, User, UserId

### Community 52 - "Community 52"
Cohesion: 0.47
Nodes (5): AbstractValidator, NotesAndFileBackend.Api.Validators, CreateNoteRequestValidator, CreateShareRequestValidator, UpdateNoteRequestValidator

### Community 53 - "Community 53"
Cohesion: 0.33
Nodes (6): Guid, AuthResponse, AccessToken, DeviceId, RefreshToken, UserId

### Community 54 - "Community 54"
Cohesion: 0.33
Nodes (6): List, NoteSearchResponse, Items, Page, PageSize, Total

### Community 55 - "Community 55"
Cohesion: 0.40
Nodes (4): renderedPortArg, error, isValid, JsonElement

### Community 56 - "Community 56"
Cohesion: 0.40
Nodes (4): NoteBlockMatch, BlockId, BlockType, Snippet

### Community 57 - "Community 57"
Cohesion: 0.40
Nodes (5): JsonElement, NoteBlockDto, BlockType, ContentJson, Position

### Community 58 - "Community 58"
Cohesion: 0.40
Nodes (4): Guid, RefreshRequest, DeviceId, RefreshToken

### Community 59 - "Community 59"
Cohesion: 0.50
Nodes (3): ITokenService, IConfiguration, TokenService

### Community 60 - "Community 60"
Cohesion: 0.60
Nodes (3): DateTime, Guid, MigrationBuilder

### Community 61 - "Community 61"
Cohesion: 0.50
Nodes (3): UpdateProfileRequest, DisplayName, Password

### Community 62 - "Community 62"
Cohesion: 0.50
Nodes (3): DateTime, Guid, ModelBuilder

### Community 63 - "Community 63"
Cohesion: 0.50
Nodes (3): DateTime, Guid, ModelBuilder

## Knowledge Gaps
- **376 isolated node(s):** `Email`, `Password`, `DisplayName`, `DeviceName`, `Platform` (+371 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **2 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `AppDbContext` connect `Community 13` to `Community 0`, `Community 1`, `Community 2`, `Community 3`, `Community 4`, `Community 5`, `Community 7`, `Community 8`, `Community 11`, `Community 12`, `Community 14`, `Community 15`, `Community 17`, `Community 18`, `Community 21`, `Community 22`, `Community 34`, `Community 39`, `Community 41`, `Community 42`, `Community 43`, `Community 44`, `Community 45`?**
  _High betweenness centrality (0.326) - this node is a cross-community bridge._
- **Why does `NotesAndFileBackend.Infrastructure.Data` connect `Community 2` to `Community 1`, `Community 35`, `Community 36`, `Community 47`, `Community 20`, `Community 28`, `Community 29`, `Community 30`?**
  _High betweenness centrality (0.190) - this node is a cross-community bridge._
- **Why does `AuditEvent` connect `Community 3` to `Community 0`, `Community 4`, `Community 5`, `Community 8`, `Community 13`, `Community 50`?**
  _High betweenness centrality (0.108) - this node is a cross-community bridge._
- **Are the 15 inferred relationships involving `AuditEvent` (e.g. with `.GenerateCommand()` and `.DeleteFile()`) actually correct?**
  _`AuditEvent` has 15 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Email`, `Password`, `DisplayName` to the rest of the system?**
  _376 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Community 0` be split into smaller, more focused modules?**
  _Cohesion score 0.05109126984126984 - nodes in this community are weakly interconnected._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.0512987012987013 - nodes in this community are weakly interconnected._