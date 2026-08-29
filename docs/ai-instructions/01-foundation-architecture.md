# AI Agent Implementation Instructions

This file is a focused implementation unit extracted from the larger backend/integration specification.

## How to use this file
- Treat the instructions in this file as authoritative for the sections it contains.
- Preserve the architecture and security constraints from the original specification.
- Do not silently remove requirements.
- When another part is referenced, use the master index to locate it rather than duplicating or redefining the requirement.
- Implement production-quality code and tests appropriate to the scope of this file.
- Do not introduce architecture that conflicts with the modular-monolith direction.

# Part 1 — Foundation & Architecture

## Source sections included

`2`, `3`, `4`

---

# 2. Recommended High-Level Architecture

Use a modular ASP.NET Core architecture rather than placing all logic in controllers.

```text
MAUI Client
    |
    | HTTPS / JSON
    v
ASP.NET Core Web API
    |
    +----------------------------+
    |                            |
    v                            v
Application Layer           Authentication
    |                            |
    v                            v
Domain Layer                 Identity/JWT
    |
    +----------------+-------------------+------------------+
    |                |                   |                  |
    v                v                   v                  v
Notes Service    Search Service   Share Service     Command Generator
    |                |                   |                  |
    +----------------+---------+---------+------------------+
                             |
                             v
                         EF Core
                             |
                             v
                        PostgreSQL
```

Recommended backend projects:

```text
src/
  App.Api/
  App.Application/
  App.Domain/
  App.Infrastructure/
  App.Contracts/
tests/
  App.UnitTests/
  App.IntegrationTests/
```

Responsibilities:

### `App.Api`
HTTP endpoints, authentication middleware, exception handling, request/response mapping, API versioning, rate limiting.

### `App.Application`
Use cases, commands/queries, validation, business rules, authorization decisions, DTO orchestration.

### `App.Domain`
Entities, value objects, domain rules, enums, invariants. This layer must not depend on ASP.NET Core or EF Core.

### `App.Infrastructure`
EF Core, PostgreSQL, migrations, repositories/query implementations, full-text search implementation, storage adapters, hashing utilities, export/import processing.

### `App.Contracts`
Public request/response DTOs and API contracts shared conceptually with the MAUI client. Avoid sharing domain entities with MAUI.

---


---

# 3. Core Functional Requirements

The server must support:

## 3.1 Notes

A user can:

- Create a note.
- Update a note.
- Delete a note.
- Restore a deleted note if soft-delete is enabled.
- Archive/unarchive a note.
- Pin/unpin a note.
- Favorite/unfavorite a note.
- Assign a category.
- Add tags.
- Add a title, summary and rich content.
- Add code blocks.
- Add links.
- Add command-generator blocks.
- Search note titles and content.
- Filter by category, tags, tool, date, favorite, archived state and visibility.
- Import notes.
- Export notes.
- Duplicate a note.
- View note revision history.
- Share a note through a short public URL.

The initial requirements specifically call out add/edit/delete/share and search/filter operations.

---


---

# 4. Note Content Model

Do not store rich note content as arbitrary executable HTML.

Use a controlled structured document format.

Recommended representation:

```json
{
  "version": 1,
  "blocks": [
    {
      "id": "01J...",
      "type": "heading",
      "level": 2,
      "text": "Full Nmap Scan"
    },
    {
      "id": "01J...",
      "type": "paragraph",
      "text": "Example explanation..."
    },
    {
      "id": "01J...",
      "type": "code",
      "language": "bash",
      "code": "nmap -p- -T4 192.168.1.10",
      "copyEnabled": true
    },
    {
      "id": "01J...",
      "type": "link",
      "text": "Nmap documentation",
      "url": "https://nmap.org/docs.html"
    },
    {
      "id": "01J...",
      "type": "commandGenerator",
      "templateId": "..."
    }
  ]
}
```

Store the canonical document as JSONB in PostgreSQL.

Also maintain extracted searchable text in a dedicated column so PostgreSQL full-text search does not have to repeatedly parse the entire JSON document.

Recommended columns:

```text
notes.content_jsonb
notes.search_text
```

`content_jsonb` is the source of truth for rendering.

`search_text` is a denormalized searchable representation.

---
