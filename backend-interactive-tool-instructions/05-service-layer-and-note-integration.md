# Part 05 — Service Layer and Note Integration

## Domain

Use:

```text
Note
 └── InteractiveTools[]
```

The tool is part of the note's functionality.

## Services

Adapt to existing architecture; suitable responsibilities may include:

```text
InteractiveToolService
InteractiveToolValidator
InteractiveToolAuthorizationService
InteractiveToolRevisionService
```

Do not create duplicates if equivalent services already exist.

## Create flow

```text
authenticate
→ load note
→ verify user is note creator
→ validate metadata
→ validate HTML
→ validate CSS
→ validate JavaScript
→ calculate hash
→ create tool
→ revision/audit
→ transaction commit
```

## Update flow

```text
authenticate
→ load tool + note
→ verify owner
→ concurrency check
→ validate changed assets
→ recalculate hash
→ increment asset version
→ save revision
→ update tool
→ audit
→ commit
```

## Delete flow

```text
authenticate
→ load tool
→ verify owner
→ soft-delete
→ audit
→ commit
```

## Note reference

A note block should reference the stable tool ID rather than duplicating source:

```json
{
  "type": "interactiveTool",
  "toolId": "..."
}
```

## Missing/deleted tool

A missing tool must not crash the note.

Return an appropriate unavailable state.

## Duplicate note

If the current product supports deep-copying notes, interactive tools should normally be deep-copied with new IDs and ownership transferred to the duplicated note's owner, unless the domain explicitly supports shared references.

## Search

Searchable metadata may include:

```text
tool name
description
```

Do not automatically index raw JavaScript/HTML/CSS source into ordinary note search.

## Import/export

Imported tools must use the same validation pipeline as normal creation.

Never execute imported assets.
