# AI Agent Implementation Instructions

This file is a focused implementation unit extracted from the larger backend/integration specification.

## How to use this file
- Treat the instructions in this file as authoritative for the sections it contains.
- Preserve the architecture and security constraints from the original specification.
- Do not silently remove requirements.
- When another part is referenced, use the master index to locate it rather than duplicating or redefining the requirement.
- Implement production-quality code and tests appropriate to the scope of this file.
- Do not introduce architecture that conflicts with the modular-monolith direction.

# Part 2 — PostgreSQL & Persistence

## Source sections included

`5`, `6`, `7`, `8`, `9`, `10`, `11`, `12`, `13`, `50`, `51`, `52`, `53`, `54`, `55`, `71`, `85`, `86`, `87`, `88`, `93`, `94`

---

# 5. PostgreSQL Database Design

Use PostgreSQL with EF Core migrations.

Recommended database tables:

```text
users
categories
tags
notes
note_tags
note_revisions
note_links
note_command_generators
command_generator_fields
command_generator_options
share_links
imports
exports
audit_logs
refresh_tokens
```

Optional later:

```text
note_favorites
note_views
note_attachments
tool_catalog
tool_versions
saved_command_presets
```

---


---

# 6. `users`

Recommended fields:

```sql
id                uuid primary key
email             varchar(320) not null unique
normalized_email  varchar(320) not null unique
display_name      varchar(120)
password_hash     text
is_active         boolean not null default true
is_admin          boolean not null default false
created_at        timestamptz not null
updated_at        timestamptz not null
last_login_at     timestamptz null
row_version       bigint not null default 0
```

Use UUID/UUIDv7 for identifiers.

Never store plaintext passwords.

---


---

# 7. `categories`

```sql
id             uuid primary key
user_id        uuid not null references users(id)
name           varchar(120) not null
slug           varchar(140) not null
description    varchar(500)
sort_order     integer not null default 0
created_at     timestamptz not null
updated_at     timestamptz not null
```

Unique constraint:

```text
(user_id, slug)
```

Example:

```text
Networking
Web Security
Nmap
Linux
Enumeration
Cheatsheets
```

---


---

# 8. `tags`

```sql
id             uuid primary key
user_id        uuid not null references users(id)
name           varchar(80) not null
normalized     varchar(80) not null
created_at     timestamptz not null
```

Unique constraint:

```text
(user_id, normalized)
```

---


---

# 9. `notes`

Recommended schema:

```sql
id                  uuid primary key
user_id             uuid not null references users(id)
category_id         uuid null references categories(id)

title               varchar(300) not null
slug                varchar(340) not null
summary             varchar(1000)
tool_name           varchar(120) null

content_jsonb       jsonb not null
search_text         text not null

is_pinned           boolean not null default false
is_favorite         boolean not null default false
is_archived         boolean not null default false
is_deleted          boolean not null default false

visibility          varchar(30) not null default 'private'

version             integer not null default 1

created_at          timestamptz not null
updated_at          timestamptz not null
deleted_at          timestamptz null

created_by          uuid not null references users(id)
updated_by          uuid not null references users(id)
```

Recommended visibility values:

```text
private
unlisted
public
```

Do not expose a note merely because the user knows its numeric/UUID identifier.

---


---

# 10. Slugs

A note should have two identifiers:

1. Internal database ID.
2. Human-friendly slug.

Example:

```text
database id:
0198f....

public URL:
https://your-domain.example/n/nmap-full-scan-a7k3
```

Do not use the database UUID as the public URL.

Slug rules:

- Lowercase.
- ASCII-safe.
- Normalize whitespace.
- Replace unsupported characters.
- Enforce maximum length.
- Ensure uniqueness.
- Add a short random suffix if necessary.

Example:

```text
nmap-full-scan-a7k3
```

---


---

# 11. `note_tags`

Many-to-many table:

```sql
note_id uuid not null references notes(id) on delete cascade
tag_id  uuid not null references tags(id) on delete cascade

primary key (note_id, tag_id)
```

---


---

# 12. Note Revision History

Recommended table:

```sql
id             uuid primary key
note_id        uuid not null references notes(id)
version        integer not null
title          varchar(300) not null
content_jsonb  jsonb not null
summary        varchar(1000)
edited_by      uuid not null references users(id)
created_at     timestamptz not null
```

Keep revision history configurable.

Recommended default:

```text
Keep latest 50 revisions per note.
```

Revision history is valuable for command templates because a template change can otherwise unexpectedly alter the behavior of a note.

---


---

# 13. Links Inside Notes

For links extracted from note content:

```sql
id          uuid primary key
note_id     uuid not null references notes(id) on delete cascade
block_id    varchar(100)
url         text not null
title       varchar(300)
created_at  timestamptz not null
```

Validate URLs.

Only allow:

```text
https
http
```

unless the application explicitly requires additional URI schemes.

Reject dangerous schemes such as:

```text
javascript:
data:
file:
```

---


---

# 50. PostgreSQL Indexing

At minimum:

```text
users(normalized_email)
categories(user_id, slug)
tags(user_id, normalized)
notes(user_id, updated_at)
notes(user_id, is_deleted)
notes(user_id, category_id)
notes(user_id, is_archived)
notes(user_id, slug)
share_links(slug)
note_tags(tag_id)
GIN(search_text)
```

Add indexes based on real query plans, not blindly.

---


---

# 51. PostgreSQL Data Integrity

Use:

- foreign keys
- unique constraints
- check constraints where useful
- transactions
- `NOT NULL` where appropriate
- sensible default values

Examples:

```sql
CHECK (char_length(title) BETWEEN 1 AND 300)
```

For enum-like values, prefer application enums plus database validation strategy.

---


---

# 52. EF Core Configuration

Use explicit Fluent API mappings.

Avoid relying entirely on conventions for a production schema.

Example:

```csharp
builder.Entity<Note>(entity =>
{
    entity.HasKey(x => x.Id);

    entity.Property(x => x.Title)
        .HasMaxLength(300)
        .IsRequired();

    entity.Property(x => x.ContentJson)
        .HasColumnType("jsonb")
        .IsRequired();

    entity.HasIndex(x => new
    {
        x.UserId,
        x.UpdatedAt
    });
});
```

Use PostgreSQL-specific configuration intentionally.

---


---

# 53. Database Migrations

Every schema change must be an EF Core migration.

Development:

```bash
dotnet ef migrations add AddCommandGenerators
dotnet ef database update
```

Production:

```text
Build migration
Review SQL
Apply through deployment pipeline
```

Do not silently recreate production databases.

---


---

# 54. Transactions

Use transactions for multi-table operations.

Example note save:

```text
Begin transaction
  update note
  update tags
  update extracted search text
  create revision
Commit
```

If any required operation fails, rollback all changes.

---


---

# 55. Optimistic Concurrency

Recommended:

```text
row version / xmin / explicit integer version
```

For a simple application, an integer `version` is easy to reason about.

Example:

```text
Client reads version 7.
Client sends update for version 7.
Server sees current version 8.
Server returns 409.
```

This prevents one MAUI device from silently overwriting another change.

---


---

# 71. Secrets

Do not place secrets in PostgreSQL note content unless the product later adds an explicit encrypted-secret feature.

A security notes application can easily become a target for sensitive data leakage.

Consider a future dedicated encrypted-secret store with:

```text
envelope encryption
key rotation
access auditing
never-return-after-create semantics where appropriate
```

Do not treat ordinary note text as a secure vault.

---


---

# 85. Date/Time Handling

Store timestamps in PostgreSQL as:

```text
timestamptz
```

Represent them as UTC from the backend.

MAUI should convert to local time for display.

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


---

# 94. Import/Export Compatibility

Never assume the current schema is the only schema.

Import pipeline:

```text
read version
-> migrate imported document
-> validate
-> normalize
-> persist current representation
```

Example:

```text
v1 import -> v2 internal model
```

This prevents old exports from becoming unusable after application upgrades.

---
