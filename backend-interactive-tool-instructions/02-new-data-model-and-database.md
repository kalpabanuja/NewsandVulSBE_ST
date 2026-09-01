# Part 02 — Custom Interactive Tool Data Model

## Core table

Create a new table such as:

```sql
custom_interactive_tools
```

Recommended fields:

```text
id                  uuid primary key
note_id             uuid not null
owner_user_id       uuid not null

name                varchar(200) not null
description         text null

html_source         text not null
css_source          text not null
javascript_source   text not null

content_hash        varchar(128) not null
schema_version      integer not null
asset_version       integer not null

is_enabled          boolean not null default true
is_deleted          boolean not null default false

validation_status   varchar(40) not null
security_status     varchar(40) not null

created_at          timestamptz not null
updated_at          timestamptz not null
deleted_at          timestamptz null

created_by          uuid not null
updated_by          uuid not null
```

## Relationships

```text
custom_interactive_tools.note_id
    → notes.id

custom_interactive_tools.owner_user_id
    → users.id
```

Also use foreign keys for `created_by` and `updated_by`.

## Ownership

The owner is determined server-side:

```text
authenticated user
        ↓
note ownership check
        ↓
owner_user_id = authenticated user
```

Never trust:

```json
{ "ownerUserId": "another-user" }
```

## Identifier

Generate the ID server-side using UUID/UUIDv7.

Never use note position, name, or predictable numeric IDs as the identity.

## Name

Required:

```text
1–200 characters
trimmed
not whitespace-only
```

Optionally enforce unique normalized names per note.

## Description

Optional and size-limited. Suggested configurable maximum: 2 KB.

## Source assets

Store:

```text
html_source
css_source
javascript_source
```

as source text.

Do not store large source assets repeatedly inside note JSON. The note block should reference the tool ID:

```json
{
  "id": "block-123",
  "type": "interactiveTool",
  "toolId": "0198..."
}
```

## Versioning

Keep both:

```text
schema_version
asset_version
```

Increment `asset_version` whenever HTML, CSS, or JavaScript changes.

## Content hash

Calculate a deterministic SHA-256 hash over the canonical tool data/assets and store it as `content_hash`.

Use it for integrity/caching/change detection, not as the primary ID.

## Validation/security state

Recommended:

```text
validation_status:
Pending | Valid | Invalid | Rejected

security_status:
Unreviewed | Approved | Rejected
```

## Indexes

At minimum:

```text
note_id
owner_user_id
(note_id, is_deleted)
(note_id, name)
content_hash
```

Use EF Core Fluent API and PostgreSQL constraints.

## Secure persistence

Do not log HTML/CSS/JavaScript source.

Do not expose source in analytics or ordinary exceptions.

Use the backend's normal secure database/storage controls; if application-level encryption is required, isolate it behind a storage/encryption service.
