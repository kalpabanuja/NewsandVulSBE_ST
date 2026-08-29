# Part 7 — Existing MAUI Application Integration Contract

## Purpose

This document defines how the backend/API integrates with the **already-existing .NET MAUI application**.

## CRITICAL — DO NOT CREATE A NEW MAUI APPLICATION

The MAUI application already exists.

**Do not create a new MAUI project.**
**Do not initialize a second MAUI client.**
**Do not recreate the frontend from scratch.**
**Do not replace the existing MAUI project structure simply to match a recommended architecture.**
**Do not create duplicate pages, ViewModels, services, API clients, models, or components when suitable existing implementations already exist.**

Before making MAUI-related changes:

1. Inspect the existing solution and MAUI project.
2. Identify the existing API client, authentication/session handling, services, DTOs, pages, ViewModels, navigation, local storage, and reusable components.
3. Reuse and extend the existing implementations wherever practical.
4. Create a new class/component/page only when the required capability genuinely does not already exist.
5. Preserve working functionality and existing UX unless a requirement explicitly calls for a change.
6. Treat the detailed frontend behavior and UI instructions as an integration target, not as a reason to rebuild the application.

The separate file:

```text
docs/ai-instructions/frontend-user-side-maui-detailed-instructions.md
```

defines the frontend/user-experience requirements. This file defines the **backend/API contract and integration behavior** needed by that existing frontend.

---

## Integration Boundary

The required architecture is:

```text
Existing MAUI Application
        |
        | HTTPS / JSON
        v
ASP.NET Core API
        |
        v
PostgreSQL
```

Never connect MAUI directly to PostgreSQL.

PostgreSQL remains the server authority.

MAUI local storage, if present, is only a cache/offline workspace.

---

## 1. MAUI/API Source-of-Truth Contract

The existing MAUI application should treat the backend API as the source of truth for server-owned data and business rules.

Integrate the API into the application's **existing** service/API-client architecture.

Do not assume that the project needs a new layer named exactly:

```text
Views
ViewModels
Services
ApiClient
DTOs
Local Cache
```

Those are responsibilities, not mandatory new folders/files. Map them onto the existing application's architecture.

For existing command-generator functionality, integrate with:

```text
POST /api/v1/command-generators/{id}/generate
```

without creating a second command-generation architecture.

---

## 2. Security Validation Boundary

MAUI may perform client-side validation for fast user feedback.

The backend must validate again.

```text
Existing MAUI validation
        ↓
Fast UX feedback
        ↓
ASP.NET Core validation
        ↓
Security boundary
```

Never change the backend to trust a value merely because the MAUI application already validated it.

---

## 3. API Authentication

The integration should support:

```text
JWT access token
Refresh token rotation
HTTPS only
```

Access tokens should be short-lived.

Refresh tokens should:

```text
be stored securely by the existing MAUI authentication/session implementation
rotate after use
be revocable
```

Do not store tokens in plaintext preferences or ordinary logs.

Use the MAUI platform secure-storage mechanism already used by the existing application. Do not replace an existing working secure-storage abstraction solely to match this document.

---

## 4. Authorization

Every authenticated note operation must be evaluated using the authenticated server identity.

The client must not be trusted to choose ownership.

Bad:

```json
{
  "userId": "someone-else"
}
```

Correct:

```text
Authenticated request
        ↓
Current server-side user identity
        ↓
Authorization / ownership check
        ↓
Database query
```

Public/shared notes must use the dedicated controlled public-access path rather than bypassing normal authorization.

---

## 5. Note List Response Contract

The API should return lightweight note-list DTOs.

Do not require the existing MAUI application to download complete `content_jsonb` documents for every item in a note list.

Example:

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

Full note content should be fetched when the existing application opens a note.

---

## 6. Existing MAUI Offline/Local Cache Integration

If the existing application already has offline/local storage, integrate the backend sync contract into that implementation.

The preferred local technology is SQLite, but **do not create a second SQLite subsystem if the application already has one**.

Rules:

```text
PostgreSQL = server authority
Existing MAUI local database/cache = local workspace
```

Never allow:

```text
MAUI → PostgreSQL
```

The API remains the network boundary.

---

## 7. Synchronization Contract

When offline synchronization is implemented, local records should be able to track:

```text
serverId
localId
version
lastSyncedAt
syncState
```

Possible states:

```text
Synced
CreatedLocally
ModifiedLocally
DeletedLocally
Conflict
```

The exact storage model may be adapted to the existing application's local database architecture.

Conflicts must not silently overwrite important edits with last-write-wins behavior.

---

## 8. API Documentation Contract

Every backend endpoint used by the existing MAUI application should document:

```text
purpose
authorization
request body
query parameters
response
validation rules
possible errors
pagination
examples
```

OpenAPI/Swagger is the primary contract.

The existing MAUI application should be able to integrate from the documented contract without guessing backend behavior.

---

## 9. DTO Boundary

Do not expose database/domain entities directly to the existing MAUI application.

Use explicit API contracts such as:

```text
NoteListItemDto
NoteDetailsDto
CreateNoteRequest
UpdateNoteRequest
SearchNotesResponse
CommandGeneratorDto
GenerateCommandRequest
GenerateCommandResponse
ShareLinkDto
ImportResultDto
```

If the existing MAUI application already has equivalent DTOs/models, reuse or adapt them rather than creating duplicate classes with competing meanings.

---

## 10. Command Generator API Contract

The existing MAUI command-generator UI should receive enough metadata to render the generator dynamically.

Example:

```json
{
  "id": "...",
  "name": "Nmap Scan",
  "toolName": "nmap",
  "fields": [
    {
      "key": "target",
      "label": "Target",
      "type": "target",
      "required": true
    },
    {
      "key": "ports",
      "label": "Ports",
      "type": "portSelector",
      "required": false,
      "presets": [
        "common",
        "all"
      ]
    }
  ]
}
```

Do not require the existing frontend to have a separate hard-coded page for every generator.

The API contract must remain compatible with the frontend instruction file.

---

## 11. Generated Command Response

The command-generation endpoint:

```http
POST /api/v1/command-generators/{id}/generate
```

returns generated text only.

Example:

```json
{
  "command": "nmap ...",
  "displayCommand": "nmap ...",
  "warnings": []
}
```

The server must never execute the generated command.

The existing MAUI application should present the generated result to the user and may provide actions such as:

```text
Copy
Edit inputs
Regenerate
Save as preset
Share/copy
```

Any UI implementation belongs in the frontend instruction file.

---

## 12. Error Contract

The existing MAUI application should consume the backend's consistent problem-details/error contract.

Field-level validation errors should identify the related field where possible.

Example:

```json
{
  "errors": [
    {
      "field": "target",
      "code": "invalid_target",
      "message": "Enter a valid target."
    }
  ]
}
```

The frontend should map these errors to its existing validation/UI mechanisms rather than introducing a second incompatible error model.

---

## 13. API Evolution

Keep the API versioned under:

```text
/api/v1/
```

The existing MAUI application should tolerate additive response fields where practical.

Breaking API contract changes require a new version rather than silently breaking the existing client.

---

## 14. Integration Rule for Existing Code

When implementing any MAUI-facing backend feature:

```text
Inspect existing app
        ↓
Find existing integration point
        ↓
Extend existing implementation
        ↓
Add only missing functionality
        ↓
Preserve compatibility
```

Do not follow this pattern:

```text
Read requirements
        ↓
Create a brand-new MAUI project
        ↓
Create a second set of pages/services/ViewModels
        ↓
Ignore existing application
```

That is explicitly prohibited.

---

## 15. Scope Boundary

This document does **not** instruct the agent to build the MAUI user interface.

UI behavior, page design, navigation UX, reusable controls, responsive layouts, accessibility, command-generator presentation, offline UX, and frontend-specific implementation belong in:

```text
frontend-user-side-maui-detailed-instructions.md
```

The job of this file is to ensure that the backend/API can be integrated cleanly into the **existing** MAUI application.

---

## Integration Completion Criteria

This integration work is complete when:

```text
Existing MAUI app can authenticate against the API
Existing MAUI app can consume note APIs
Existing MAUI app can consume search APIs
Existing MAUI app can consume sharing APIs
Existing MAUI app can consume import/export APIs
Existing MAUI app can consume command-generator APIs
Backend validation remains authoritative
No direct MAUI → PostgreSQL connection exists
No duplicate MAUI application was created
No existing working frontend functionality was unnecessarily replaced
```
