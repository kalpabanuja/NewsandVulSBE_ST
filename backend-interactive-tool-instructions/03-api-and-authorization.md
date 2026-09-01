# Part 03 — API and Owner Authorization

## Resource

Use a note-scoped API such as:

```text
/notes/{noteId}/interactive-tools
/notes/{noteId}/interactive-tools/{toolId}
```

Adapt naming only if the existing API convention requires it.

## Create

```http
POST /notes/{noteId}/interactive-tools
```

Example:

```json
{
  "name": "HTTP Header Tester",
  "description": "Interactive HTTP header reference tool",
  "html": "<div>...</div>",
  "css": ".tool { ... }",
  "javascript": "..."
}
```

Do not accept authoritative ownership fields.

## List

```http
GET /notes/{noteId}/interactive-tools
```

Prefer a lightweight response:

```json
[
  {
    "id": "uuid",
    "name": "HTTP Header Tester",
    "description": "...",
    "assetVersion": 3,
    "isEnabled": true
  }
]
```

Do not return full source for lists unless necessary.

## Details

```http
GET /notes/{noteId}/interactive-tools/{toolId}
```

Return the source assets only when the requesting context needs them.

## Update

```http
PUT /notes/{noteId}/interactive-tools/{toolId}
```

Only the owner may change:

```text
name
description
HTML
CSS
JavaScript
enabled state
```

Use the existing optimistic concurrency approach.

## Delete

```http
DELETE /notes/{noteId}/interactive-tools/{toolId}
```

Only the owner may delete.

Prefer soft-delete if consistent with the existing note system.

## Authorization

Every management operation must perform:

```text
authenticated user
        ↓
load tool
        ↓
load owning note
        ↓
determine note creator
        ↓
compare authenticated user ID
        ↓
allow / deny
```

Do not rely on the frontend hiding buttons.

## Permissions

Separate:

```text
CanViewInteractiveTool
CanManageInteractiveTool
```

Public/read-only viewers may be able to use/view a tool without being able to modify it.

## DTO boundary

Use explicit DTOs:

```text
InteractiveToolListDto
InteractiveToolDetailsDto
CreateInteractiveToolRequest
UpdateInteractiveToolRequest
InteractiveToolValidationDto
```

Never expose EF entities directly.

Never expose:

```text
internal storage paths
security internals
token hashes
database internals
```

## Errors

Use the existing RFC 7807/problem-details contract.

Map fields to:

```text
name
description
html
css
javascript
```

where applicable.
