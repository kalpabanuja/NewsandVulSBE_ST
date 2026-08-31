# Backend Note System Rework — Implementation Plan

## Background

After thoroughly inspecting the existing codebase, I can confirm what already works and what needs to be added. The backend already has:
- ✅ Note CRUD with optimistic concurrency (`Version` field)
- ✅ Revision history (`NoteRevision` entity, saved on every update)
- ✅ PostgreSQL full-text search on `SearchText` column with GIN index
- ✅ Public note sharing with tokens, expiry, password, max-views, revocation
- ✅ Browser share rendering (HTML via Content Negotiation — done by us recently)
- ✅ HTML password prompt for browser share links (done by us recently)
- ✅ Soft delete, archive, pin, favorite, duplicate, restore
- ✅ Tag resolution and categories
- ✅ Command generators (C# template-based — but **not** Jint/JavaScript)
- ✅ Note attachments (basic `NoteAttachment` entity exists)
- ✅ Audit events for note.created, note.updated, note.deleted, note.restored, note.shared
- ✅ Rate limiting via `GlobalPolicy` / `StrictPolicy`

## What Needs to Be Done

### Phase 1 — Content & Validation (Highest Priority)

**Problem:** The existing system accepts any arbitrary JSON in `ContentJsonb` with no block-type validation. The instructions require validated block types and URL scheme validation.

#### [MODIFY] `src/NotesAndFileBackend.Application/Services/NoteContentValidator.cs` [NEW]
- Create a content block validator that parses the `ContentJsonb` JSON.
- Validates the envelope `{ "version": 2, "blocks": [...] }`.
- Validates each block type against the allowed set: `heading`, `paragraph`, `bulletList`, `numberedList`, `checkList`, `divider`, `link`, `displayAttachment`, `downloadAttachment`, `code`, `commandGenerator`.
- Validates heading `level` is 1–5.
- Validates list `style` for bullets (`disc`, `circle`, `square`).
- Validates divider `style` (`singleLine`, `dots`, `breakLines`, `space`).
- Validates link `url` scheme (allow only `http`, `https`; reject `javascript:`, `data:`, `file:`, `vbscript:`).
- Validates code block `ui.backgroundColor` format (`#RRGGBB` or `#RGBA`).
- Returns structured validation errors in the format `{ field, code, message }`.

#### [MODIFY] `src/NotesAndFileBackend.Api/Controllers/NotesController.cs`
- Plug `NoteContentValidator` into `CreateNote` and `UpdateNote` to validate `ContentJsonb` before saving.
- Return `400` with structured errors when validation fails.
- Add `Visibility` field to `CreateNoteRequest` and `UpdateNoteRequest` (`"PRIVATE"` or `"PUBLIC"`).
- Emit `note.visibility_changed` audit event when visibility changes during update.

#### [MODIFY] `src/NotesAndFileBackend.Api/DTOs/NoteDTOs.cs`
- Add `Visibility` to `CreateNoteRequest` and `UpdateNoteRequest`.
- Add `Visibility` to the `GetNote` response.

---

### Phase 2 — Attachment API (Note-specific)

**Problem:** `NoteAttachment` entity exists but has no upload/download API. There's no distinction between display vs. downloadable attachments. Size limits are missing.

#### [NEW] `src/NotesAndFileBackend.Api/Controllers/NoteAttachmentsController.cs`
- `POST /api/v1/note-attachments` — Upload attachment (multipart). Validate size:
  - Downloadable: max 10 MiB.
  - Display (image/video): max configurable `AppConfig:MaxDisplayAttachmentBytes` (default 50 MiB).
  - Detect and validate MIME type server-side (not trusting the client).
  - Save to S3/MinIO via existing `IStorageService`.
- `GET /api/v1/note-attachments/{id}/preview` — Stream bytes for display attachments (no download headers).
- `GET /api/v1/note-attachments/{id}/download` — Downloadable attachments only, with `Content-Disposition: attachment`.
- `DELETE /api/v1/note-attachments/{id}` — Owner only.

#### [MODIFY] `src/NotesAndFileBackend.Domain/Entities/NoteAttachment.cs`
- Add `AttachmentType` (`Display` | `Downloadable`).
- Add `DisplayName` (user-supplied name for downloadable files).
- Add `Width`, `Height`, `DurationSeconds` (optional, for display attachments).
- Add `ThumbnailStorageKey` (optional).
- Add `OwnerUserId` (for authorization without going through Note).

#### Migration — Add new columns to `NoteAttachments` table.

---

### Phase 3 — Improved Browser Share View

**Problem:** The existing HTML share page is a basic fallback showing raw `contentJsonb`. We need to render it block-by-block according to section 25.

#### [MODIFY] `src/NotesAndFileBackend.Api/Controllers/PublicController.cs`
- Update `GetSharedNote` to render each block type in HTML:
  - `heading` → `<h1>`–`<h5>`
  - `paragraph` → `<p>` with preserved formatting
  - `bulletList` → `<ul>` with disc/circle/square style
  - `numberedList` → `<ol>`
  - `checkList` → `<ul>` with checkbox icons, non-editable
  - `divider` → `<hr>` styled per type
  - `link` → `<a href="..." rel="noopener noreferrer" target="_blank">` (sanitized URL)
  - `code` → `<pre><code>` with a Copy button (using configurable background color)
  - `displayAttachment` → `<img>` or `<video>`, no download control
  - `downloadAttachment` → file card with download button
  - `commandGenerator` → read-only description only (no admin controls)
- Return `410 Gone` with an HTML "expired" page for expired/revoked links (currently returns generic `404`).
- Ensure `noindex` is set by default.

---

### Phase 4 — Jint JavaScript Command Generators (Section 14)

**Problem:** The current generator uses a C# template renderer (`CommandTemplateRenderer`). The instructions require JavaScript execution via Jint.

> [!IMPORTANT]
> This is the most significant architectural change. The existing template-based generator still works and will be preserved. The Jint engine will be added as an **additional** execution backend, with the `Language` field on the generator deciding which path to use (`"csharp_template"` vs `"javascript"`).

#### [NEW] `src/NotesAndFileBackend.Application/Services/JintCommandGeneratorService.cs`
- Add the `Jint` NuGet package.
- Create a sandboxed Jint execution service:
  - No access to .NET objects, DI, DB, filesystem, processes.
  - Bounded timeout (configurable, e.g. 2s).
  - Fresh isolated engine per request.
  - Exposes only the `inputs` object to the script.
- Returns `{ success, output, warnings, errors }`.

#### [MODIFY] `src/NotesAndFileBackend.Api/Controllers/CommandGeneratorsController.cs`
- Add `POST /api/v1/command-generators/{id}/test` endpoint.
- Accepts a draft script and input values, runs via Jint sandbox, returns result without persisting.
- Route existing generators through the correct engine based on `Language` field.

#### [MODIFY] `src/NotesAndFileBackend.Domain/Entities/NoteCommandGenerator.cs`
- Add `Language` property (`"csharp_template"` | `"javascript"`), default `"csharp_template"` to keep backwards compatibility.
- Add `Script` property for Jint JavaScript source (nullable, only used if `Language = "javascript"`).

#### Migration — Add `Language` and `Script` columns to `note_command_generators`.

---

### Phase 5 — Remaining Audit Events & Tests

#### [MODIFY] `src/NotesAndFileBackend.Api/Controllers/NotesController.cs`
- Emit `note.share_accessed` audit event in `PublicController.GetSharedNote` when a share is successfully accessed.
- Confirm `note.visibility_changed` is emitted.

#### Tests
- Add xUnit integration tests covering: block type validation (all 11 types + invalid), URL scheme rejection, link validation, attachment size limits, optimistic concurrency (`409 Conflict`), share expiry/revocation returning `410`.

---

## Open Questions

> [!IMPORTANT]
> **Jint**: Do you want to migrate the existing template-based generators to JavaScript/Jint, or add Jint as an opt-in new feature while keeping C# templates for existing generators? I recommend **keeping templates for existing generators and adding Jint as a new optional language** to avoid breaking anything.

> [!NOTE]
> **Attachment storage**: Display attachment previews will be served through the same MinIO/S3 bucket as other files (via `IStorageService`). Is there any separate storage requirement?

## Verification Plan

- `dotnet build` to confirm no compilation errors.
- Verify block validation rejects bad input with structured errors via curl.
- Verify the share page renders HTML blocks correctly via browser.
- Verify `410 Gone` for expired/revoked share links.
- Run `dotnet test` for new unit tests.
- Commit and push to GitHub for VPS deployment.
