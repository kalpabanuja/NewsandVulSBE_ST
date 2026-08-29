# AI Agent Implementation Instructions

This file is a focused implementation unit extracted from the larger backend/integration specification.

## How to use this file
- Treat the instructions in this file as authoritative for the sections it contains.
- Preserve the architecture and security constraints from the original specification.
- Do not silently remove requirements.
- When another part is referenced, use the master index to locate it rather than duplicating or redefining the requirement.
- Implement production-quality code and tests appropriate to the scope of this file.
- Do not introduce architecture that conflicts with the modular-monolith direction.

# Part 5 — Sharing & Import/Export

## Source sections included

`18`, `19`, `20`, `21`, `22`, `23`, `24`, `100`, `101`, `115`

---

# 18. Import and Export

The requirements call for note import/export.

Supported formats should be:

```text
JSON
ZIP of JSON/Markdown
Markdown
```

Optional later:

```text
YAML
HTML
```

Recommended canonical interchange format:

```text
application/vnd.yourapp.notes+json
```

Example:

```json
{
  "format": "notes",
  "version": 1,
  "exportedAt": "...",
  "notes": []
}
```

Import must validate:

- schema version
- note size
- number of notes
- block count
- URL format
- command-generator definitions
- duplicate slugs
- malformed content

Never directly execute anything during import.

---


---

# 19. Import Processing

For small imports:

```text
MAUI -> API -> validate -> transaction -> PostgreSQL
```

For larger imports:

```text
MAUI -> upload -> import job -> background processor -> PostgreSQL
```

Create an `imports` table:

```sql
id            uuid primary key
user_id       uuid not null
file_name     varchar(255)
status        varchar(40)
total_items   integer
processed     integer
failed        integer
error_jsonb   jsonb
created_at    timestamptz
completed_at  timestamptz null
```

---


---

# 20. Export Processing

For normal exports:

```text
POST /api/v1/notes/export
```

Request:

```json
{
  "format": "json",
  "noteIds": [],
  "includeRevisions": false
}
```

If `noteIds` is empty, export all notes the caller is authorized to export.

For very large exports, create a background job.

---


---

# 21. Shareable Note Links

The application needs short, customizable share links.

Recommended public pattern:

```text
https://your-domain.example/s/nmap-full-scan-a7k3
```

or:

```text
https://your-domain.example/s/Nmap123
```

Do not expose private note IDs.

---


---

# 22. Share Link Database

Recommended:

```sql
id                  uuid primary key
note_id             uuid not null references notes(id)
created_by          uuid not null references users(id)

slug                varchar(100) not null unique

is_active           boolean not null default true
expires_at          timestamptz null

password_hash       text null

allow_indexing      boolean not null default false
max_views           integer null
view_count          integer not null default 0

created_at          timestamptz not null
last_accessed_at    timestamptz null
```

Optional protection:

```text
password-protected share
expiration
view limit
revoke
regenerate link
read-only
```

These features are strongly recommended.

---


---

# 23. Share URL API

Create:

```http
POST /api/v1/notes/{noteId}/share
```

Request:

```json
{
  "slug": "nmap-full-scan",
  "expiresAt": null,
  "password": null
}
```

The server must normalize and validate the slug.

Response:

```json
{
  "slug": "nmap-full-scan",
  "url": "https://your-domain.example/s/nmap-full-scan"
}
```

Public access:

```http
GET /api/v1/shared/{slug}
```

or a dedicated web route:

```text
/s/{slug}
```

The server must verify the share record before returning the note.

---


---

# 24. Share Slug Security

Never use predictable sequential share IDs.

Bad:

```text
/s/1001
/s/1002
/s/1003
```

Better:

```text
/s/nmap-full-scan-k8x4
```

A custom slug can be human-readable while still including entropy when appropriate.

Also implement:

- rate limiting
- inactive/revoked checks
- optional expiration
- optional password protection
- audit events for share creation/revocation
- no search-engine indexing by default

---


---

# 100. Public Share Rendering

A public share should expose only safe content.

Server should not leak:

```text
owner email
internal note ID
audit information
private metadata
revision history
private tags unless intentionally included
```

Return a dedicated `SharedNoteDto`.

---


---

# 101. Public Share Caching

Public note pages may be cached carefully, but cache invalidation is required.

On update/revoke:

```text
invalidate shared representation
```

Private notes should never be accidentally served from a public cache.

---


---

# 115. Critical Import Rule

Imported content is untrusted.

Treat:

```text
file
JSON
Markdown
links
generator templates
```

as hostile input until validated.

Do not execute any content from an imported file.

---
