# AI Agent Implementation Instructions

This file is a focused implementation unit extracted from the larger backend/integration specification.

## How to use this file
- Treat the instructions in this file as authoritative for the sections it contains.
- Preserve the architecture and security constraints from the original specification.
- Do not silently remove requirements.
- When another part is referenced, use the master index to locate it rather than duplicating or redefining the requirement.
- Implement production-quality code and tests appropriate to the scope of this file.
- Do not introduce architecture that conflicts with the modular-monolith direction.

# Part 3 — Notes, CRUD & Search

## Source sections included

`3`, `14`, `15`, `16`, `17`, `65`, `86`, `87`, `88`, `89`, `90`, `91`, `92`, `93`

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

# 14. Full-Text Search

The requirements need two search levels:

### Note-level search

Example:

```text
scan all
```

Should return:

```text
Nmap Full Scan
Network Enumeration
Nmap Port Scanning
```

### Inside-note search

The client should be able to query matching content within an individual note.

PostgreSQL is a good fit.

Recommended index:

```sql
CREATE INDEX ix_notes_search
ON notes
USING GIN (to_tsvector('simple', search_text));
```

Optionally use a generated `tsvector` column.

Suggested searchable fields:

```text
title
summary
tool_name
tags
content text
code blocks
command generator descriptions
```

Code blocks should be searchable because users explicitly want to find commands such as a full scan command.

---


---

# 15. Search API

Example:

```http
GET /api/v1/notes/search?q=scan%20all
```

Parameters:

```text
q
categoryId
tag
tool
favorite
pinned
archived
page
pageSize
sort
```

Response:

```json
{
  "items": [
    {
      "id": "...",
      "title": "Nmap Full Scan",
      "summary": "...",
      "category": "Nmap",
      "matchedBlocks": [
        {
          "blockId": "...",
          "snippet": "...scan all..."
        }
      ]
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 3
}
```

The API should return enough information for MAUI to display search results without downloading every complete note.

---


---

# 16. Inside-Note Search

Endpoint:

```http
GET /api/v1/notes/{noteId}/search?q=ports
```

The response should identify:

```text
block ID
block type
match position where practical
highlight/snippet
```

The MAUI client can then jump directly to the matching block.

---


---

# 17. CRUD APIs

## Create

```http
POST /api/v1/notes
```

Request:

```json
{
  "title": "Nmap Full Scan",
  "summary": "Useful full-port scanning reference",
  "categoryId": "...",
  "tags": ["nmap", "scanning"],
  "toolName": "nmap",
  "content": {
    "version": 1,
    "blocks": []
  }
}
```

Response:

```text
201 Created
```

Return:

```json
{
  "id": "...",
  "slug": "nmap-full-scan-a7k3",
  "version": 1,
  "createdAt": "..."
}
```

---

## Read

```http
GET /api/v1/notes/{id}
```

For normal authenticated use.

Optional:

```http
GET /api/v1/notes/by-slug/{slug}
```

for share/public lookup where appropriate.

---

## Update

```http
PUT /api/v1/notes/{id}
```

Use optimistic concurrency.

Client supplies:

```text
version
```

or preferably:

```text
ETag / If-Match
```

If the note was modified by somebody else:

```text
409 Conflict
```

or:

```text
412 Precondition Failed
```

---

## Delete

```http
DELETE /api/v1/notes/{id}
```

Prefer soft deletion by default.

Permanent deletion should be separate:

```http
DELETE /api/v1/notes/{id}/permanent
```

and restricted by policy.

---


---

# 65. Note List Response Optimization

The note list should not return full `content_jsonb` for every note.

List response:

```json
{
  "id": "...",
  "title": "Nmap Full Scan",
  "summary": "...",
  "category": "Nmap",
  "tags": ["nmap", "scanning"],
  "toolName": "nmap",
  "isFavorite": true,
  "isPinned": false,
  "updatedAt": "..."
}
```

Only fetch full content when the user opens the note.

---


---

# 86. Soft Delete Rules

When a note is soft-deleted:

```text
is_deleted = true
deleted_at = now()
```

Normal search/list queries must exclude deleted notes.

Deleted notes should not accidentally remain available through public shares.

When a note is deleted:

```text
revoke active share links
```

unless the product explicitly supports independent share lifetime.

---


---

# 87. Archive Rules

Archive should not mean delete.

Archived notes:

```text
still exist
still searchable if the user chooses
not shown in normal active lists
```

The API should allow:

```text
includeArchived=true
```

only when requested.

---


---

# 88. Note Duplication

Recommended endpoint:

```http
POST /api/v1/notes/{id}/duplicate
```

The duplicate should:

```text
get new ID
get new slug
copy current content
copy category
copy tags
reset share links
create version 1
```

This is useful for making variants of command references.

---


---

# 89. Categories and Tags

Categories are hierarchical/grouping-oriented.

Tags are cross-cutting metadata.

Do not force tags and categories to be the same database concept.

Example:

```text
Category:
Nmap

Tags:
recon
tcp
port-scanning
cheatsheet
```

---


---

# 90. Search Ranking

Prefer results approximately in this order:

```text
exact title match
title prefix match
title token match
tag match
tool-name match
content match
code match
```

PostgreSQL full-text search can handle the base ranking; specialized fuzzy matching can be added later.

---


---

# 91. Fuzzy Search

Optional enhancement:

PostgreSQL `pg_trgm` can improve typo tolerance.

Example:

```text
nmp
```

can still find:

```text
nmap
```

Use fuzzy matching selectively because it is more expensive than ordinary indexed search.

---


---

# 92. Search Highlighting

Return snippets rather than entire matching documents.

For example:

```text
... use Nmap to perform a <match>full scan</match> ...
```

The MAUI client can render the highlighted match.

---


---

# 93. Note Content Versioning

Store:

```text
document schema version
note revision
```

These are different concepts.

Example:

```text
document schema version = 1
note revision = 14
```

This allows the document format to evolve independently from note edits.

---
