# AI Agent Implementation Instructions

This file is a focused implementation unit extracted from the larger backend/integration specification.

## How to use this file
- Treat the instructions in this file as authoritative for the sections it contains.
- Preserve the architecture and security constraints from the original specification.
- Do not silently remove requirements.
- When another part is referenced, use the master index to locate it rather than duplicating or redefining the requirement.
- Implement production-quality code and tests appropriate to the scope of this file.
- Do not introduce architecture that conflicts with the modular-monolith direction.

# Part 6 — API, Security & Cross-Cutting Backend

## Source sections included

`42`, `43`, `44`, `45`, `46`, `47`, `48`, `49`, `60`, `61`, `62`, `63`, `64`, `68`, `69`, `72`, `73`, `74`, `75`, `79`, `80`, `81`, `82`, `83`, `84`, `102`, `103`, `104`, `105`, `112`, `113`, `114`, `116`

---

# 42. MAUI Integration Contract

MAUI should treat the API as the source of truth.

Recommended client layers:

```text
Views
ViewModels
Services
ApiClient
DTOs
Local Cache
```

Example:

```text
NoteListPage
    -> NoteListViewModel
        -> INoteService
            -> IApiClient
                -> HTTPS
```

For command generators:

```text
NoteViewPage
    -> CommandGeneratorViewModel
        -> ICommandGeneratorService
            -> POST /api/v1/command-generators/{id}/generate
```

---


---

# 43. Do Not Put Security-Critical Business Rules Only in MAUI

MAUI validation improves user experience, but the backend must repeat validation.

Bad assumption:

```text
MAUI validates target
therefore server trusts target
```

Correct:

```text
MAUI validates -> fast UI feedback
Backend validates -> security boundary
```

---


---

# 44. API Authentication

Recommended:

```text
JWT access token
refresh token rotation
HTTPS only
```

Access token:

```text
short-lived
```

Refresh token:

```text
longer-lived
stored securely on MAUI
rotated after use
revoked when appropriate
```

Do not store access tokens in plaintext preferences.

Use the platform secure storage facilities available to .NET MAUI.

---


---

# 45. Authorization

Every note operation must validate ownership/permission.

Example policy:

```text
User A cannot GET /notes/{id} for User B.
```

The repository/query must include the authenticated user scope rather than relying on the controller to remember it.

For example:

```csharp
var note = await dbContext.Notes
    .Where(n => n.Id == noteId)
    .Where(n => n.UserId == currentUser.Id)
    .SingleOrDefaultAsync(ct);
```

Public shared notes use a separate controlled access path.

---


---

# 46. API Error Contract

Use one consistent problem-detail structure.

Example:

```json
{
  "type": "https://your-domain.example/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "traceId": "00-...",
  "errors": {
    "title": [
      "Title is required."
    ]
  }
}
```

Recommended status codes:

```text
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
412 Precondition Failed
413 Payload Too Large
422 Unprocessable Entity
429 Too Many Requests
500 Internal Server Error
```

---


---

# 47. Request Size Limits

Since notes can contain rich content and code blocks, establish explicit limits.

Recommended starting values:

```text
single note: 2 MB
single block: 512 KB
import file: 25 MB
title: 300 characters
summary: 1000 characters
command template: 10 KB
generated command: 32 KB
```

These are configuration values, not hard-coded assumptions.

---


---

# 48. API Pagination

Never return an unbounded note list.

Use:

```http
GET /api/v1/notes?page=1&pageSize=50
```

Enforce:

```text
default page size = 20
maximum = 100
```

Cursor pagination can be added later if datasets become large.

---


---

# 49. Sorting

Supported values:

```text
updated
created
title
favorite
pinned
```

Always validate sort fields server-side.

Never concatenate arbitrary query parameters into SQL.

---


---

# 60. Rate Limiting

Protect:

```text
login
refresh
public share lookup
search
command generation
imports
exports
```

Especially rate-limit public share URLs to reduce enumeration and abuse.

---


---

# 61. Validation Library

Use a consistent validation approach such as FluentValidation or equivalent.

Validators needed for:

```text
CreateNoteRequest
UpdateNoteRequest
CreateCategoryRequest
CreateShareLinkRequest
ImportRequest
CommandGeneratorDefinition
CommandGenerationRequest
```

---


---

# 62. API Versioning

Start with:

```text
/api/v1/
```

Do not make MAUI depend on unversioned endpoints such as:

```text
/api/notes
```

Versioning allows the mobile app and server to evolve independently.

---


---

# 63. OpenAPI

Enable OpenAPI/Swagger in development.

Document:

```text
authentication
notes
categories
tags
search
imports
exports
shares
command generators
errors
pagination
```

This becomes the main integration contract for MAUI.

---


---

# 64. API Endpoints Summary

Recommended initial endpoints:

```text
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh
POST   /api/v1/auth/logout

GET    /api/v1/notes
POST   /api/v1/notes
GET    /api/v1/notes/{id}
PUT    /api/v1/notes/{id}
DELETE /api/v1/notes/{id}

GET    /api/v1/notes/search
GET    /api/v1/notes/{id}/search

GET    /api/v1/categories
POST   /api/v1/categories
PUT    /api/v1/categories/{id}
DELETE /api/v1/categories/{id}

GET    /api/v1/tags
POST   /api/v1/tags
DELETE /api/v1/tags/{id}

GET    /api/v1/notes/{id}/revisions
GET    /api/v1/notes/{id}/revisions/{version}
POST   /api/v1/notes/{id}/restore

POST   /api/v1/notes/import
POST   /api/v1/notes/export

POST   /api/v1/notes/{id}/share
GET    /api/v1/notes/{id}/share
DELETE /api/v1/notes/{id}/share/{shareId}

GET    /api/v1/shared/{slug}

GET    /api/v1/command-generators/{id}
POST   /api/v1/command-generators/{id}/generate
```

---


---

# 68. Security Boundaries

The system should follow:

```text
MAUI = untrusted client
API = security boundary
PostgreSQL = private infrastructure
```

The client may send malicious input even when it is the official application.

Always validate on the API.

---


---

# 69. Content Sanitization

Although the canonical note model is structured JSON, any HTML rendering must sanitize content.

Never render arbitrary user input with unrestricted HTML.

Recommended allowed constructs:

```text
paragraph
heading
ordered list
unordered list
code
blockquote
link
command generator
```

Block:

```text
script
iframe
object
embed
event-handler attributes
javascript URLs
```

---


---

# 72. Backups

PostgreSQL must have automated backups.

Minimum operational plan:

```text
daily full backup
point-in-time recovery
backup encryption
restore testing
retention policy
```

A backup that has never been restored in testing should not be considered verified.

---


---

# 73. Health Checks

Add:

```http
GET /health/live
GET /health/ready
```

Liveness checks:

```text
process is alive
```

Readiness checks:

```text
database reachable
required infrastructure available
```

Do not expose sensitive diagnostic information publicly.

---


---

# 74. Configuration

Use environment-specific configuration.

Example:

```text
ConnectionStrings__Postgres
Jwt__Issuer
Jwt__Audience
Jwt__SigningKey
Share__BaseUrl
Notes__MaxSize
Import__MaxFileSize
RateLimiting__...
```

Never commit production secrets to Git.

---


---

# 75. Environment Structure

Recommended:

```text
Development
Test
Staging
Production
```

Use separate:

```text
database
JWT signing keys
storage
share base URLs
logging destinations
```

---


---

# 79. Idempotency

For operations where duplicate requests are harmful, support idempotency keys.

Especially:

```text
imports
exports
share creation
```

Example header:

```http
Idempotency-Key: <client-generated-id>
```

---


---

# 80. API Timeouts

Every database/API operation should use cancellation tokens.

Example:

```csharp
public async Task<NoteDto?> GetAsync(
    Guid noteId,
    CancellationToken cancellationToken)
{
    ...
}
```

Pass the cancellation token through EF Core operations.

---


---

# 81. Repository Strategy

Do not create generic repositories solely for abstraction.

Prefer feature-focused query/service interfaces such as:

```csharp
INoteQueries
INoteCommands
ISearchService
IShareLinkService
ICommandGeneratorService
```

This keeps queries optimized and avoids generic repository limitations.

---


---

# 82. CQRS

A lightweight CQRS approach is recommended.

Commands:

```text
CreateNote
UpdateNote
DeleteNote
CreateShareLink
GenerateCommand
ImportNotes
```

Queries:

```text
GetNotes
GetNote
SearchNotes
SearchInsideNote
GetRevisions
GetShare
```

A full event-sourcing architecture is unnecessary at this stage.

---


---

# 83. Domain Events

Optional but useful events:

```text
NoteCreated
NoteUpdated
NoteDeleted
ShareCreated
ShareRevoked
ImportCompleted
```

Use them for:

```text
audit
search index refresh
notifications
analytics
```

Do not introduce a message broker until there is a real need.

---


---

# 84. API Response Consistency

Use consistent JSON naming.

Recommended:

```text
camelCase
```

Example:

```json
{
  "createdAt": "...",
  "updatedAt": "...",
  "isFavorite": true
}
```

---


---

# 102. Security Headers

The web/API host should include appropriate security headers.

At minimum consider:

```text
HSTS
Content-Security-Policy where applicable
X-Content-Type-Options
Referrer-Policy
```

Do not assume mobile API clients eliminate web security concerns because public share links may be accessed by browsers.

---


---

# 103. CORS

Configure CORS explicitly.

Do not deploy:

```text
AllowAnyOrigin
AllowAnyHeader
AllowAnyMethod
```

unless there is a clear and reviewed reason.

For a native MAUI client, broad browser CORS support may not be needed at all.

---


---

# 104. Database Connection Security

Production PostgreSQL should:

```text
not be exposed directly to the public internet
use TLS where appropriate
use a dedicated application user
use least-privilege permissions
```

The API should be the normal network boundary.

---


---

# 105. Secrets and Connection Strings

Development may use environment variables or developer secrets.

Production should use:

```text
secret manager
environment secret injection
managed identity
```

Never put a production PostgreSQL password in:

```text
appsettings.json
Git
MAUI application
```

---


---

# 112. Recommended Coding Rules

Use:

```text
nullable reference types
async/await
CancellationToken
dependency injection
structured logging
immutable request/response records where practical
centralized validation
explicit authorization
```

Avoid:

```text
static global state
service locator
God classes
controller business logic
SQL string concatenation
client-controlled ownership fields
arbitrary command execution
```

---


---

# 113. Critical Ownership Rule

The client must never choose:

```json
{
  "userId": "someone-else"
}
```

and expect the server to trust it.

The authenticated identity comes from the authentication context.

The server determines:

```text
currentUserId
```

then applies that identity to the database query.

---


---

# 114. Critical Share Rule

A share URL is a capability token.

Treat it accordingly.

Anyone possessing the active unprotected link may potentially view the shared note.

Therefore the UI must clearly communicate:

```text
This link grants access to this shared note.
```

The backend should support revocation.

---


---

# 116. Critical Generator Rule

The command generator produces text.

It does not execute text.

Architecturally:

```text
Command Template
       +
Validated Values
       |
       v
Deterministic C# Renderer
       |
       v
Command String
       |
       v
MAUI Copy Button
```

Not:

```text
Command Template
       |
       v
Shell
       |
       v
Operating System
```

---
