# AI Agent Implementation Instructions

This file is a focused implementation unit extracted from the larger backend/integration specification.

## How to use this file
- Treat the instructions in this file as authoritative for the sections it contains.
- Preserve the architecture and security constraints from the original specification.
- Do not silently remove requirements.
- When another part is referenced, use the master index to locate it rather than duplicating or redefining the requirement.
- Implement production-quality code and tests appropriate to the scope of this file.
- Do not introduce architecture that conflicts with the modular-monolith direction.

# Part 8 — Testing, Deployment & Operations

## Source sections included

`56`, `57`, `58`, `59`, `72`, `73`, `76`, `77`, `79`, `80`, `106`, `107`, `108`, `109`, `110`, `111`, `117`, `118`

---

# 56. Background Jobs

The first implementation can keep simple CRUD synchronous.

Add background processing for:

```text
large imports
large exports
search re-indexing
cleanup of old revisions
expired share cleanup
audit-log maintenance
```

A hosted service is sufficient for an early deployment; a durable job system can be introduced when scale requires it.

---


---

# 57. Caching

Recommended:

```text
In-memory cache
```

for small reference data such as:

```text
tool definitions
port presets
generator metadata
```

Later:

```text
Redis
```

for multi-instance deployments.

Never cache user-private note data without including user scope in cache keys.

---


---

# 58. Audit Logging

Create an audit trail for security-relevant operations.

Record:

```text
login
logout/revocation
note created
note updated
note deleted
note permanently deleted
share created
share revoked
share accessed
import started/completed/failed
export generated
command template changed
```

Do not store secrets or raw access tokens.

---


---

# 59. Logging

Use structured logging with:

```text
ILogger<T>
```

Every request should have a correlation/trace ID.

Log:

```text
request ID
user ID where appropriate
route
status
duration
exception type
```

Do not log:

```text
passwords
JWTs
refresh tokens
API keys
session secrets
```

For command generation, logging the complete generated command should be configurable because generated targets can contain sensitive internal information.

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

# 76. Docker

Recommended backend development environment:

```yaml
services:
  postgres:
    image: postgres
    ports:
      - "5432:5432"

  api:
    build:
      context: .
```

The exact image tag, secrets and credentials should be environment-specific.

Production should pin known versions rather than blindly using `latest`.

---


---

# 77. Automated Tests

Minimum test coverage:

## Unit tests

Test:

```text
slug generation
slug validation
port validation
target validation
command template rendering
required field validation
share permissions
note ownership rules
import schema validation
```

## Integration tests

Test:

```text
PostgreSQL migrations
create note
update note
delete note
search note
search inside note
share creation
share revocation
command generation
```

## API tests

Test:

```text
401
403
404
409
400
422
429
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

# 106. Deployment

Suggested deployment flow:

```text
Commit
  -> Build
  -> Unit Tests
  -> Integration Tests
  -> Security/Dependency Scan
  -> Build Container
  -> Apply DB Migration
  -> Deploy API
  -> Health Check
  -> Smoke Test
```

Database migrations should be backward-compatible with the currently running API during rolling deployments where practical.

---


---

# 107. Observability

Production should track:

```text
API request count
API latency
5xx rate
database latency
search latency
import/export failures
command generation failures
share-link access
```

Alert on:

```text
high 5xx
database unavailable
migration failure
unusual public-share traffic
repeated authentication failures
```

---


---

# 108. Performance Goals

Start with practical targets rather than premature optimization.

Suggested targets:

```text
normal note fetch: < 200 ms server processing
note search: < 300 ms for normal-sized datasets
command generation: < 100 ms for standard templates
database queries: indexed and explain-plan reviewed
```

These are engineering targets, not guarantees.

---


---

# 109. Important Non-Goals for the First Version

Do not make the initial version unnecessarily complicated.

Avoid initially:

```text
microservices
event sourcing
Kafka
Kubernetes
distributed command execution
AI-generated shell commands
remote shell execution
complex collaborative editing
```

Build a reliable modular monolith first.

---


---

# 110. Suggested Implementation Order

## Phase 1 — Foundation

Implement:

```text
ASP.NET Core API
PostgreSQL
EF Core
authentication
users
notes
categories
tags
CRUD
```

## Phase 2 — Rich Notes

Implement:

```text
JSON block content
code blocks
links
copyable code
revisions
archive/favorite/pin
```

## Phase 3 — Search

Implement:

```text
full-text search
filters
inside-note search
highlighting
```

## Phase 4 — Command Generator

Implement:

```text
generator definition
fields
validation
target parser
port selector
preset support
C# rendering engine
generate endpoint
```

## Phase 5 — Sharing

Implement:

```text
short slugs
share records
public read-only access
revoke
expiration
optional password
```

## Phase 6 — Import/Export

Implement:

```text
JSON export
JSON import
Markdown export
validation
background jobs for large files
```

## Phase 7 — Hardening

Implement:

```text
rate limiting
audit logging
backups
observability
security testing
concurrency
performance testing
```

---


---

# 111. Recommended Project Structure

```text
src/
  App.Api/
    Controllers/
      AuthController.cs
      NotesController.cs
      CategoriesController.cs
      TagsController.cs
      SearchController.cs
      SharesController.cs
      CommandGeneratorsController.cs
      ImportsController.cs
      ExportsController.cs
    Middleware/
    Filters/
    Extensions/

  App.Application/
    Notes/
      Commands/
      Queries/
      Validators/
    Categories/
    Tags/
    Search/
    Sharing/
    ImportExport/
    CommandGenerators/
    Common/

  App.Domain/
    Entities/
      User.cs
      Note.cs
      Category.cs
      Tag.cs
      NoteRevision.cs
      ShareLink.cs
      CommandGenerator.cs
    ValueObjects/
    Enums/
    Exceptions/

  App.Infrastructure/
    Persistence/
      AppDbContext.cs
      Configurations/
      Migrations/
    Search/
    Security/
    ImportExport/
    Sharing/
    Services/

  App.Contracts/
    Notes/
    Search/
    Sharing/
    CommandGenerators/
    Common/

tests/
  App.UnitTests/
  App.IntegrationTests/
  App.ApiTests/
```

---


---

# 117. Acceptance Criteria

The backend implementation can be considered functionally complete for the first major release when all of the following work:

### Notes

```text
Create note
Read note
Update note
Delete note
Restore note
Category
Tags
Rich blocks
Code blocks
Links
Revision history
Favorite
Pin
Archive
```

### Search

```text
Search notes
Filter notes
Search inside note
Search code
Search generator descriptions
```

### Import/Export

```text
Export JSON
Import JSON
Validate import
Preserve generator definitions
```

### Sharing

```text
Create share link
Custom short slug
Public read-only access
Revoke
Expiration
Optional password
```

### Command Generator

```text
Dynamic fields
Target validation
Port selector
Presets
Safe template rendering
Generated command response
No command execution
```

### Infrastructure

```text
PostgreSQL
EF Core migrations
Authentication
Authorization
Rate limiting
Audit logging
Health checks
Backups
Tests
```

---


---

# 118. Final Recommended Architecture Decision

For this application, the strongest first implementation is:

```text
.NET 10+ / ASP.NET Core
        +
EF Core
        +
PostgreSQL
        +
Modular Monolith
        +
JSONB note documents
        +
PostgreSQL Full-Text Search
        +
Short opaque/custom share slugs
        +
C# deterministic command-template engine
        +
MAUI HTTPS API client
        +
Optional MAUI SQLite cache
```

The command generator should remain a **pure generation service**. It should never become an implicit remote command-execution service.

The backend should own authentication, authorization, validation, search, revisions, sharing, imports/exports and generator validation. MAUI should focus on presentation, local UX/cache and sending strongly typed requests to the API.

This structure leaves room to add more tools, richer note blocks, saved presets, collaboration, synchronization and other features without rewriting the core architecture.
