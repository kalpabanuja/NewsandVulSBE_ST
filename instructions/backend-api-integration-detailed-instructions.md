# Backend API — Existing Application Extension & Note System Rework

## 0. Purpose

This document is an implementation specification for extending and refining the **already-existing backend/API** of the application.

The backend already exists. The frontend already exists. This is **not a greenfield build**.

The implementation goal is to add and refine the requested Note features while preserving working functionality that already exists.

Technology boundary:

```text
Existing .NET MAUI Application
          |
          | HTTPS / JSON
          v
Existing ASP.NET Core Backend API
          |
          v
Existing PostgreSQL Database
```

The backend remains the authoritative source for:

- notes and note ownership
- note visibility/permissions
- note block validation
- attachment metadata and access control
- public share links
- authenticated application viewing
- revisions/concurrency
- search/indexing
- command-generator definitions and safe generation
- API error contracts

The MAUI application remains the user-facing client.

---

# 1. CRITICAL EXISTING-SYSTEM RULES

## 1.1 Do not rebuild the backend

Before changing anything:

1. Inspect the existing solution.
2. Locate the existing API project(s), application/domain services, EF Core context, migrations, repositories, controllers/endpoints, DTOs, validation, authentication, authorization, storage, and tests.
3. Identify the existing note/content model and existing sharing implementation.
4. Reuse suitable implementations.
5. Extend existing services/contracts rather than creating parallel implementations.
6. Preserve existing endpoints unless a compatibility-safe refinement is required.
7. Add migrations rather than destroying/recreating the database.

Do **not** create a second backend, second DbContext, second note service, or duplicate command-generator engine merely because a recommended architecture differs from the current one.

## 1.2 Do not break existing clients

The current MAUI application is an existing consumer of this API.

Prefer:

```text
additive response fields
backward-compatible endpoints
versioned contracts
migration of old content into the new canonical model
```

Avoid silently changing the meaning of existing JSON properties.

Breaking changes require a deliberate version transition.

## 1.3 API base

Existing production API host:

```text
https://newsandvulst.mrpent.us/
```

Versioned API base:

```text
https://newsandvulst.mrpent.us/api/v1
```

Keep environments configurable. Do not hard-code production URLs throughout the codebase.

---

# 2. IMPLEMENTATION ORDER

Implement in this order so the frontend can integrate progressively:

```text
1. Inspect current implementation
2. Define/migrate canonical note block model
3. Add validation + persistence changes
4. Add note visibility and authorization
5. Add attachment storage/access metadata
6. Add note CRUD/update/concurrency behavior
7. Add public share API + browser rendering
8. Add search extraction/indexing
9. Integrate/refine command generator
10. Add API contract documentation
11. Add unit/integration/security tests
12. Run end-to-end tests with the existing MAUI client
```

Do not start by deleting existing note code.

---

# 3. NOTE DOCUMENT MODEL

## 3.1 Canonical structured document

The note body must be represented as structured JSON, never as arbitrary executable HTML.

Canonical envelope:

```json
{
  "version": 2,
  "blocks": []
}
```

Every block has a stable unique `id`.

Block order is the order of the `blocks` array.

The backend validates the entire document before persistence.

## 3.2 Supported block types

The canonical block type set for this feature is:

```text
heading
paragraph
bulletList
numberedList
checkList
divider
link
displayAttachment
downloadAttachment
code
commandGenerator
```

Aliases from older versions should be normalized during read/import/migration where practical.

## 3.3 Suggested JSON shape

```json
{
  "version": 2,
  "blocks": [
    {
      "id": "block-1",
      "type": "heading",
      "level": 2,
      "text": "Installation"
    },
    {
      "id": "block-2",
      "type": "paragraph",
      "text": "Install the required package first.",
      "format": {
        "fontSize": 16,
        "bold": false,
        "italic": false,
        "underline": false
      }
    },
    {
      "id": "block-3",
      "type": "bulletList",
      "style": "disc",
      "items": [
        {
          "id": "item-1",
          "text": "First step",
          "checked": false
        }
      ]
    },
    {
      "id": "block-4",
      "type": "divider",
      "style": "single"
    },
    {
      "id": "block-5",
      "type": "code",
      "language": "bash",
      "code": "sudo apt install nmap",
      "ui": {
        "backgroundColor": "#1f2937"
      }
    }
  ]
}
```

The exact storage shape may be adapted to the existing model, but the semantics below are mandatory.

---

# 4. HEADING BLOCK

Supported levels:

```text
Heading 1
Heading 2
Heading 3
Heading 4
Heading 5
```

Backend rules:

- `level` must be an integer from 1 through 5.
- Text must be present and bounded by a configured maximum length.
- No arbitrary HTML is accepted.
- Styling is represented as data/semantics, not stored as executable markup.

---

# 5. PARAGRAPH BLOCK

A paragraph stores text and optional inline formatting.

The API must support the frontend behavior where pressing Enter inside a paragraph produces a new paragraph block/split paragraph rather than inserting a literal HTML `<br>` payload.

Canonical behavior:

```text
Paragraph A|Paragraph B
        Enter
        ↓
Paragraph A
Paragraph B
```

The split is a frontend editing behavior, but the backend must permit adjacent paragraph blocks and preserve them in order.

Do not merge adjacent paragraphs automatically unless an explicit normalization operation is requested.

---

# 6. UNIVERSAL INLINE FORMATTING

All text-bearing blocks that support text formatting must use a common formatting model.

Supported formatting must include at minimum:

```text
font size
bold
italic
underline
```

The model should remain extensible for:

```text
strikethrough
text alignment
text color
highlight
```

Do not implement separate incompatible formatting schemas for headings, paragraphs, list items, links, and code descriptions.

For rich text requiring multiple styles within one string, prefer a controlled span/run representation, for example:

```json
{
  "text": "Install Nmap",
  "runs": [
    {
      "text": "Install",
      "format": { "bold": true, "fontSize": 16 }
    },
    {
      "text": " Nmap",
      "format": { "italic": true, "fontSize": 16 }
    }
  ]
}
```

Never persist raw MAUI/XAML formatting markup.

---

# 7. LIST BLOCKS

## 7.1 Bullet List

The editor must be able to choose the bullet style.

Supported starting styles should include:

```text
disc
circle
square
```

The backend must validate the style against an allowlist.

Each list item has a stable ID.

## 7.2 Numbered List

Support ordered items.

Keep the data semantic; numbering should be rendered by the client/viewer.

## 7.3 Check List

Checklist items require:

```text
id
text
checked
```

`checked` is boolean.

Do not encode checklist state into text such as `[x] item`.

---

# 8. DIVIDER BLOCK

The divider block must be configurable.

The backend should support an allowlisted style model such as:

```text
singleLine
dots
breakLines
space
```

Additional styles may be introduced later without changing existing stored documents.

No raw HTML/CSS may be stored.

Example:

```json
{
  "id": "divider-1",
  "type": "divider",
  "style": "dots"
}
```

---

# 9. LINK BLOCK

A link block contains:

```text
id
text
url
optional title/description
```

Allowed URI schemes for normal links:

```text
https
http
```

Reject dangerous schemes including:

```text
javascript:
data:
file:
vbscript:
```

Normalize and validate URLs on the server.

The backend must never trust a client-side URL validation result.

---

# 10. ATTACHMENT MODEL

Attachment handling is split into two distinct user-facing block types.

```text
Display Attachment
Downloadable Attachment
```

Do not model them as the same thing with only a UI flag if doing so weakens access control or validation.

## 10.1 Common attachment metadata

A stored attachment should have at least:

```text
attachmentId
ownerUserId
originalFileName
contentType
sizeBytes
storageKey
createdAt
checksum/hash
```

Optional:

```text
width
height
durationSeconds
thumbnailStorageKey
```

For note references, store the attachment ID, not a permanent signed storage URL.

## 10.2 Security

Never expose raw filesystem/storage paths.

Attachment access must go through authorized API/storage endpoints.

Do not trust the filename or MIME type supplied by the client.

Validate:

```text
size
extension
detected MIME type
file signature where applicable
```

Do not execute uploaded files.

---

# 11. DOWNLOADABLE ATTACHMENTS

Maximum size:

```text
10 MiB per downloadable attachment
```

The backend is authoritative for the limit even if the frontend pre-checks it.

During attachment creation, the API must support a user-supplied display name.

Required metadata:

```text
stored filename
user display name
content type
size
attachment id
```

In note view mode, the viewer can display:

```text
[User supplied name]   [Download icon]
```

The download endpoint must require authorization unless the resource is being accessed through a valid public-share capability.

The backend should return a download response with a safe content-disposition filename.

Do not expose the actual storage key.

---

# 12. DISPLAY ATTACHMENTS

Supported display attachment types for this requirement:

```text
image
video
```

The note editor must be able to store display properties such as:

```text
widthMode
alignment
maxWidth
optional fixed width/height
caption
```

Recommended `widthMode` values:

```text
full
threeQuarter
half
quarter
custom
```

Recommended alignment:

```text
left
center
right
```

## 12.1 Maximum display attachment size

Introduce a configurable backend setting:

```text
MaxDisplayAttachmentBytes
```

A sensible initial default may be **50 MiB**, but the value must be configuration-driven and changeable without changing the document schema.

The backend must reject uploads over the configured limit.

## 12.2 View-only behavior

Display attachments are viewable inside note view/share view.

Normal note view must not show a download action for display attachments.

Do not issue unrestricted download URLs to the client.

Public share view must use the same rule: display-only attachments are rendered in place and are not exposed as download controls.

Streaming/preview endpoints may return bytes to the renderer without creating a general-purpose downloadable endpoint.

---

# 13. CODE BLOCK

A Code Block is a **displayable code container**, not a command executor.

The author workflow is:

```text
Edit mode
    ↓
Select Code Block
    ↓
Paste/type code
    ↓
Optionally select language
    ↓
Save
    ↓
Normal/View mode
    ↓
Render the code in a dedicated box
    ↓
Click the code box / Copy action
    ↓
Copy exact code to clipboard
    ↓
Show temporary “Copied” confirmation
```

The stored block must preserve the exact code content, including whitespace, line breaks and characters.

Required data:

```json
{
  "id": "code-1",
  "type": "code",
  "language": "csharp",
  "code": "Console.WriteLine(\"Hello\");",
  "ui": {
    "backgroundColor": "#202020"
  }
}
```

Optional metadata:

```text
language
ui.backgroundColor
caption/title
```

The requested code background color is a **UI presentation property only**. It must not affect execution, syntax interpretation or security decisions.

Validate color syntax. Prefer a safe allowlisted format such as `#RRGGBB`, `#RGBA`, or application theme keys.

The backend must never execute the contents of a Code Block.

---

# 14. COMMAND GENERATOR BLOCK — JAVASCRIPT + JINT

A Command Generator block is a **custom JavaScript generator definition** stored with the note and executed by the backend through the **Jint JavaScript engine for .NET**.

The implementation is explicitly:

```text
MAUI Edit Mode
    ↓
Generator definition + JavaScript source
    ↓
ASP.NET Core API
    ↓
Validate definition/source/input
    ↓
Jint isolated execution
    ↓
Generated command text
    ↓
MAUI View Mode
```

**Do not use C# as the user-authored command-generator language.** C# remains the backend implementation language, but the command-generator source authored by the note creator is JavaScript.

## 14.1 Generator responsibilities

The backend must:

1. Store the generator definition and JavaScript source separately from ordinary Code Block content.
2. Validate the generator schema before saving.
3. Validate the JavaScript source before activation.
4. Return generator metadata and input fields to the existing MAUI client.
5. Accept selected input values from the client.
6. Validate all submitted values again on the server.
7. Execute the JavaScript using Jint only inside the dedicated generator runtime.
8. Return deterministic generated text for the same generator version and inputs.
9. Return warnings and structured errors without leaking server internals.
10. Record the generator version/revision used for the result where revision history is enabled.
11. Never execute the generated operating-system command.

## 14.2 Generator data model

A generator block should contain concepts equivalent to:

```json
{
  "id": "generator-1",
  "type": "commandGenerator",
  "name": "Nmap Scan",
  "description": "Generate an Nmap scan command",
  "toolName": "nmap",
  "fields": [
    {
      "key": "target",
      "label": "Target",
      "type": "target",
      "required": true
    },
    {
      "key": "scanType",
      "label": "Scan Type",
      "type": "select",
      "required": true,
      "options": ["syn", "connect"]
    }
  ],
  "language": "javascript",
  "script": "return `nmap -sS ${inputs.target}`;",
  "scriptVersion": 1,
  "isEnabled": true
}
```

The exact JSON names may be adapted to the existing schema, but the semantic contract must remain equivalent.

## 14.3 Jint execution model

Use the Jint JavaScript engine in the .NET backend for generator execution.

Execution must be:

```text
stateless
isolated per invocation
short-lived
resource-limited
without access to ASP.NET application services
```

Do **not** expose arbitrary .NET objects, dependency-injection services, database contexts, configuration objects, request objects, file-system abstractions, process APIs or server secrets to the JavaScript runtime.

The JavaScript environment must not be able to:

```text
read server files
write server files
spawn processes
invoke shell commands
access PostgreSQL directly
read environment secrets
access authentication keys/tokens
make unrestricted network requests
load native code
modify global server state
persist state between requests
```

Use Jint configuration appropriate for resource limiting, including a bounded execution timeout and instruction/recursion/resource safeguards supported by the installed Jint version.

Each generation request must create a fresh isolated execution context or otherwise guarantee that state from one invocation cannot leak into another invocation.

Do not store mutable JavaScript globals as application-wide singleton state.

## 14.4 JavaScript input/output contract

Pass validated values into JavaScript through a deliberately small input object such as:

```javascript
inputs.target
inputs.scanType
inputs.ports
```

The preferred generator contract is a single deterministic entry point that returns text, for example:

```javascript
function generate(inputs) {
    return `nmap -sS ${inputs.target}`;
}

return generate(inputs);
```

The actual supported syntax/API may be simplified so the runtime is predictable. Do not require access to browser globals, Node.js globals, `require`, filesystem APIs, process APIs or network libraries.

The backend should pass only validated plain data structures and retrieve only the final serializable result.

## 14.5 Existing generation endpoint

Keep the existing contract where already implemented:

```http
POST /api/v1/command-generators/{id}/generate
```

Typical response:

```json
{
  "command": "nmap -sS 192.168.1.10",
  "displayCommand": "nmap -sS 192.168.1.10",
  "warnings": []
}
```

The endpoint generates text only. It must never execute that text through an operating-system shell.

## 14.6 Generator test/preview endpoint

Edit Mode requires a developer-facing **Test Generator** capability.

Provide a dedicated validation/test operation, preferably separate from production generation, such as:

```http
POST /api/v1/command-generators/{id}/test
```

or an equivalent existing API contract.

The test request should accept:

```text
generator draft/script
input field values
```

and return:

```json
{
  "success": true,
  "output": "nmap -sS 192.168.1.10",
  "warnings": [],
  "errors": []
}
```

For invalid scripts or invalid test inputs:

```json
{
  "success": false,
  "output": null,
  "warnings": [],
  "errors": [
    {
      "code": "generator_runtime_error",
      "message": "The generator could not produce a result."
    }
  ]
}
```

Do not expose raw stack traces, internal file paths, server configuration or Jint internals to normal users.

The test endpoint must use the **same security restrictions and Jint sandbox model** as production generation, but it may execute the draft definition without first persisting it.

## 14.7 Statelessness requirements

The command-generator runtime must be stateless:

```text
Request A
  → fresh Jint context
  → result A
  → context disposed

Request B
  → fresh Jint context
  → result B
  → context disposed
```

A generator must not rely on:

```text
previous invocation state
server process state
global JavaScript variables
filesystem files created by earlier runs
application singleton mutation
```

If a generator needs a value, that value must be supplied explicitly through validated input or persisted generator configuration.

## 14.8 Safety boundary

Never implement the generator as:

```text
JavaScript
  ↓
.NET reflection / arbitrary host object access
  ↓
Process.Start / shell
```

The correct architecture is:

```text
Validated note-owned JavaScript
        ↓
Restricted Jint runtime
        ↓
String result
        ↓
API response
```

The command generator is a **text-generation feature**, not a remote execution feature.

---

# 15. NOTE EDITING / BLOCK INSERTION CONTRACT

The frontend should expose one canonical block-add interaction, but the backend must accept every supported block type in a versioned request.

The client must not send arbitrary block type integers that the server silently interprets.

Prefer stable string enum values:

```text
heading
paragraph
bulletList
numberedList
checkList
divider
link
displayAttachment
downloadAttachment
code
commandGenerator
```

Unknown block types must fail validation with a clear error.

---

# 16. NOTE VISIBILITY

Add/maintain a note visibility state with at least:

```text
private
public
```

## 16.1 Private

Only authorized owner/application sharing rules can access the note.

## 16.2 Public

When the user toggles a note to `public`:

- any authenticated user of the application may view it
- no non-owner may edit it
- no non-owner may delete it
- no non-owner may change visibility
- only the creator/owner may create a public share link

A public note inside the application is **not automatically anonymous web access**.

Anonymous web access happens only through an active public share link.

---

# 17. OWNERSHIP / AUTHORIZATION

Never trust a client-supplied `userId` for note ownership.

Use:

```text
authenticated identity
        ↓
currentUserId
        ↓
resource ownership check
```

Required authorization matrix:

| Action | Owner | Authenticated non-owner | Anonymous with valid share link |
|---|---|---|---|
| View private note | yes | no | no |
| View public note | yes | yes | no unless link exists |
| Edit note | yes | no | no |
| Delete note | yes | no | no |
| Change visibility | yes | no | no |
| Create share link | yes | no | no |
| Copy an existing share URL | yes; client may expose it | yes if they can see the existing share record/link in the app | yes, by browser copy |
| Revoke share link | yes | no | no |
| Download downloadable attachment | according to note access | according to note access | yes when included in shared content and allowed by share policy |
| Download display attachment | no download action | no download action | no download action |

The server enforces all of this regardless of UI state.

---

# 18. NOTE CRUD API

Preserve existing paths when present. The canonical contract should be:

```http
POST   /api/v1/notes
GET    /api/v1/notes/{id}
PUT    /api/v1/notes/{id}
DELETE /api/v1/notes/{id}
```

Optional/supporting operations:

```http
POST   /api/v1/notes/{id}/restore
POST   /api/v1/notes/{id}/duplicate
POST   /api/v1/notes/{id}/favorite
DELETE /api/v1/notes/{id}/favorite
POST   /api/v1/notes/{id}/pin
DELETE /api/v1/notes/{id}/pin
POST   /api/v1/notes/{id}/archive
DELETE /api/v1/notes/{id}/archive
```

## 18.1 Update request

The update request must carry a concurrency token/version.

Example:

```json
{
  "version": 17,
  "title": "Nmap",
  "summary": "Scanning reference",
  "visibility": "public",
  "content": {
    "version": 2,
    "blocks": []
  }
}
```

If the note was changed after version 17, return:

```text
409 Conflict
```

Do not overwrite the newer document automatically.

---

# 19. REVISION HISTORY

Every successful note content update should be able to create a revision.

Recommended fields:

```text
revisionId
noteId
version
title
summary
visibility
contentJson
editedBy
createdAt
```

Keep a configurable revision retention policy.

At minimum, preserve a useful recent history and never let revision cleanup remove the current version.

---

# 20. SEARCH / INDEXING

Search must include:

```text
title
summary
tags
category
tool name
paragraph text
heading text
list text
link text
code
command-generator metadata
```

Generate normalized searchable text from the structured document whenever a note changes.

Do not force PostgreSQL to parse JSONB from scratch for every search query.

Keep:

```text
content_jsonb = rendering source of truth
search_text   = denormalized search representation
```

Use PostgreSQL full-text search as the primary implementation if it already exists.

---

# 21. ATTACHMENT API

Use existing file APIs where possible.

The note-specific layer should be capable of:

```http
POST /api/v1/note-attachments
GET  /api/v1/note-attachments/{id}/preview
GET  /api/v1/note-attachments/{id}/download
DELETE /api/v1/note-attachments/{id}
```

The exact routes can differ if an existing attachment service already exists.

Important distinction:

- `preview` is used for display attachments and must not become an unrestricted download operation.
- `download` is for downloadable attachments only.

Return typed metadata so the MAUI client does not guess file types.

---

# 22. PUBLIC SHARE LINKS

A share link is a capability token.

Only the note owner may create/revoke it.

Anyone holding an active share URL may view the shared note according to its share policy.

Use an opaque, unguessable slug/token.

Do not use sequential IDs.

Recommended record:

```text
shareLinkId
noteId
createdBy
slug/tokenHash
isActive
expiresAt
createdAt
lastAccessedAt
allowIndexing
```

Store a hash of a secret capability token when practical; do not store only a predictable slug as the secret.

---

# 23. SHARE API

Canonical authenticated operations:

```http
POST   /api/v1/notes/{id}/share
GET    /api/v1/notes/{id}/share
DELETE /api/v1/notes/{id}/share/{shareId}
```

Creation request may support:

```json
{
  "expiresAt": null,
  "customSlug": null
}
```

Only the owner may call creation/revocation.

Once the link exists:

- the owner may copy it
- other application users may see/copy an already-created link only if the product's existing sharing UI exposes the link to them
- non-owners cannot generate a new link
- non-owners cannot revoke a link

---

# 24. BROWSER SHARE VIEW — CRITICAL REWORK

The current failure mode where a browser shows raw JSON such as:

```text
{"blocks":[{"id":"...","type":0,...
```

must be eliminated.

A browser share URL must return a **human-readable rendered document**, not an API JSON dump.

## 24.1 Preferred implementation

Create a dedicated server-rendered share route, for example:

```http
GET /s/{shareToken}
```

or preserve an existing public-share route if already deployed.

The response must be a complete HTML document suitable for direct browser rendering.

## 24.2 Browser page requirements

The generated HTML should contain:

```text
page title
note title
summary/description when present
category/tags where appropriate
ordered note blocks
formatted text
headings H1-H5
bullet lists
numbered lists
checklists
links
visual dividers
images
videos
code blocks
command-generator display/results where policy permits
file download cards
share/metadata information only where appropriate
```

Do not expose:

```text
internal database IDs
owner private metadata
JWTs
storage keys
raw content_jsonb
private navigation
edit/delete controls
application account data
```

## 24.3 Safe HTML generation

Generate HTML from the structured document model.

Do not inject stored content directly into HTML without escaping/sanitizing.

Rules:

- HTML-encode plain text.
- Sanitize URLs.
- Use allowlisted schemes.
- Never render raw user HTML.
- Never execute scripts from note content.
- Add safe external-link attributes where appropriate.
- Default public pages to `noindex` unless indexing is explicitly enabled.

## 24.4 Styling

The page must have a polished documentation layout rather than default browser text.

Minimum visual structure:

```text
--------------------------------------------------
Note title
Short description
--------------------------------------------------

Content

[heading]
paragraph paragraph paragraph

[code block]                 [Copy]

[image/video]

[file name]                  [Download]
--------------------------------------------------
```

Use responsive CSS so the page works on desktop and mobile browsers.

---

# 25. SHARE VIEW BLOCK RENDERING RULES

## Heading

Render as semantic `h1` through `h5`.

## Paragraph

Render as paragraph text with preserved intended spacing.

## Bullet list

Render with the selected bullet style where practical.

## Numbered list

Render as an ordered list.

## Checklist

Render a non-editable checklist with checked/unchecked visual states.

## Divider

Render the selected divider style.

## Link

Render safe clickable links.

## Display attachment

Render inline image/video according to saved sizing/alignment properties.

Do not show download controls.

## Download attachment

Render as a file card:

```text
Filename
Size
[Download]
```

## Code block

Render code with preserved whitespace and a copy button.

The code UI background color may be used if it passed validation.

## Command generator

In browser share view, never expose authenticated editing controls or sensitive generator administration data.

A read-only generator can be displayed only when the share policy and security model explicitly allow it.

The default public behavior should be to render the generated content/description safely rather than expose administration controls.

---

# 26. PUBLIC SHARE EXPIRATION

Before serving a shared page:

```text
share exists?
  ↓ yes
active?
  ↓ yes
not expired?
  ↓ yes
render
```

Expired or revoked links must not reveal private note existence beyond a generic state.

Recommended response:

```text
410 Gone
```

with a friendly HTML page:

```text
This shared link has expired or is no longer available.
```

Do not return the private note JSON.

---

# 27. PRIVATE/PUBLIC API BEHAVIOR

Authenticated endpoint:

```http
GET /api/v1/notes/{id}
```

uses normal authentication + ownership/public authorization.

Browser public endpoint:

```http
GET /s/{shareToken}
```

uses only the share capability and share record rules.

Do not bypass application authorization by accepting a note ID on the anonymous route.

---

# 28. DTO CONTRACTS

Do not expose EF Core entities directly.

Recommended contracts:

```text
NoteListItemDto
NoteDetailsDto
CreateNoteRequest
UpdateNoteRequest
NoteContentDto
BlockDto
AttachmentDto
ShareLinkDto
CreateShareLinkRequest
CommandGeneratorDto
GenerateCommandRequest
GenerateCommandResponse
RevisionDto
SearchNotesResponse
```

DTO names should be adapted to existing equivalents rather than duplicated.

---

# 29. VALIDATION

Every request should be validated centrally.

Validate:

```text
required fields
maximum lengths
block count
block IDs
duplicate block IDs
supported block types
heading levels
list styles
divider styles
formatting ranges
URLs
attachment ownership
attachment type
attachment size
visibility values
share token/slug rules
command generator schema
optimistic-concurrency version
```

Return consistent validation errors:

```json
{
  "errors": [
    {
      "field": "content.blocks[3].level",
      "code": "invalid_heading_level",
      "message": "Heading level must be between 1 and 5."
    }
  ]
}
```

---

# 30. ERROR HANDLING

Do not leak stack traces to clients.

Use a consistent API error shape, preferably Problem Details-compatible with field-level errors.

Examples:

```text
400 invalid request
401 unauthenticated
403 forbidden
404 not found
409 concurrency conflict
410 share expired/revoked
413 attachment too large
415 unsupported media type
422 validation failure, if used by the existing API
429 rate limited
500 generic server failure
```

The exact status-code convention should match the existing application where practical.

---

# 31. SECURITY REQUIREMENTS

The following are mandatory:

- server-side ownership checks
- public-share token entropy
- share revocation
- share expiration
- rate limiting on anonymous share endpoints
- request size limits
- attachment validation
- path traversal protection
- safe filenames
- no direct filesystem path exposure
- no raw HTML execution from notes
- safe URL validation
- import validation
- command-generator template validation
- no command execution
- audit logging for sensitive share actions

Never trust:

```text
userId from request
attachment MIME type from request
file extension alone
client-side validation
visibility flags
share status supplied by client
```

---

# 32. AUDIT EVENTS

At minimum support audit events for:

```text
note.created
note.updated
note.deleted
note.restored
note.visibility_changed
note.share_created
note.share_revoked
note.share_accessed
attachment.uploaded
attachment.deleted
command_generator_generated
```

Do not log secrets, bearer tokens, passwords, or full private note contents unnecessarily.

---

# 33. RATE LIMITING

Apply rate limits to:

```text
anonymous share view
public attachment preview
share creation
command generation
search endpoints
authentication endpoints
```

Tune limits to existing application behavior and deployment capacity.

---

# 34. MIGRATION / BACKWARD COMPATIBILITY

If the current database stores integer block types, map the old integers into the new canonical string names through a one-time migration/normalization layer.

Do not require users to manually recreate existing notes.

Example normalization:

```text
old integer type
      ↓
legacy mapper
      ↓
canonical block type
```

Keep an import/migration test corpus containing existing notes.

---

# 35. FRONTEND INTEGRATION CONTRACT

The backend must provide a stable contract for the existing MAUI application.

The frontend needs enough data to:

- render the editor
- render the note viewer
- insert/reorder blocks
- edit block properties
- upload attachments
- preview display attachments
- download downloadable attachments
- render command generators
- switch visibility
- create/copy/revoke share links
- show browser-share status

The frontend must not infer critical authorization from DTO fields alone.

---

# 36. API DOCUMENTATION

Document every endpoint with:

```text
purpose
authorization
request body
query parameters
response body
validation
status codes
examples
concurrency rules
attachment limits
share rules
```

OpenAPI/Swagger should be updated alongside the code.

---

# 37. TEST PLAN — BACKEND

Every individual block type must have tests.

## 37.1 Heading tests

Test:

```text
H1
H2
H3
H4
H5
invalid H0
invalid H6
empty text
maximum length
```

## 37.2 Paragraph tests

Test:

```text
normal paragraph
empty/whitespace validation policy
multiple adjacent paragraphs
unicode
long content
inline formatting
```

## 37.3 List tests

Test individually:

```text
bullet list — disc
bullet list — circle
bullet list — square
numbered list
checklist checked
checklist unchecked
empty list
invalid style
```

## 37.4 Divider tests

Test every supported divider style and invalid style.

## 37.5 Link tests

Test:

```text
http
https
javascript: rejection
data: rejection
file: rejection
malformed URL
```

## 37.6 Attachment tests

Test separately:

```text
display image
 display video
display size limit
image MIME mismatch
video MIME mismatch
download attachment under 10 MiB
download attachment exactly at 10 MiB
download attachment above 10 MiB
```

## 37.7 Code tests

Test code preservation, language metadata, color validation, and no execution.

## 37.8 Command-generator tests

Test valid generation, invalid inputs, template validation, deterministic output, and explicit confirmation that no process/shell execution occurs.

---

# 38. SHARE VIEW TESTS

The browser share route must be integration-tested with an HTTP client/browser-capable test harness.

Test:

```text
valid active share → HTML document
expired share → 410/friendly expired page
revoked share → inaccessible
unknown token → inaccessible
private note without share → inaccessible
public note without share → inaccessible anonymously
public note with share → render
```

Assert that the HTML response contains rendered content and **does not contain the raw JSON document payload**.

Explicit regression assertion:

```text
response must not begin as or behave like:
{"blocks":[
```

---

# 39. FULL NOTE INTEGRATION TEST

Create one test note containing every supported block:

```text
Heading 1
Heading 2
Heading 3
Heading 4
Heading 5
Paragraph
Bullet List
Numbered List
Check List
Divider
Link
Display Image
Display Video
Downloadable Attachment
Code Block
Command Generator
```

Verify:

1. API accepts it.
2. PostgreSQL stores it.
3. API returns it.
4. Search finds its text/code.
5. MAUI viewer can render it.
6. Browser share view renders it.
7. Permission rules remain correct.
8. Revision history preserves it.

---

# 40. CONCURRENCY TESTS

Simulate two clients editing the same note:

```text
Client A reads version 10
Client B reads version 10
Client A saves → version 11
Client B saves version 10 → 409 Conflict
```

The second save must not silently overwrite the first.

---

# 41. PERFORMANCE

Do not return full note content when listing notes.

Note list endpoints should use lightweight DTOs.

Large attachments must stream rather than loading the entire file into memory unnecessarily.

Large note documents should use efficient JSON serialization and bounded request sizes.

Public share rendering should cache safe rendered resources when practical, but cached content must respect expiration/revocation.

---

# 42. OBSERVABILITY

Log structured metadata such as:

```text
request id
user id when authenticated
note id where safe
endpoint
status code
latency
attachment id where safe
share id where safe
```

Never log access tokens, passwords, or secret share tokens.

---

# 43. DEFINITION OF DONE

Backend work is complete only when all of the following are true:

```text
Existing backend reused rather than rebuilt
Existing database preserved through migrations
All requested block types are supported
Universal text formatting is supported
Download attachments enforce 10 MiB maximum
Display attachments have a configurable size ceiling
Display attachments are view-only in note/share view
Note visibility private/public works
Only owners can edit/delete/change visibility/create/revoke share links
Public application users can view public notes
Active share links render a real browser page
Browser share view no longer shows raw JSON
Share expiration/revocation work
Command generator remains non-executing
Search indexes new block content
Optimistic concurrency prevents silent overwrite
Every individual block has tests
All-block integration test passes in normal view and share view
API documentation is updated
Security tests pass
```

---

# 44. FINAL IMPLEMENTATION PRINCIPLE

The correct approach is:

```text
Inspect existing backend
        ↓
Identify existing note/API/storage systems
        ↓
Extend and migrate safely
        ↓
Add the new block model and permissions
        ↓
Add attachment/share/browser rendering support
        ↓
Refine existing code instead of duplicating it
        ↓
Test every block + complete notes + permissions + browser share
        ↓
Integrate with the existing MAUI application
```

Never interpret this document as permission to replace the existing application with a new architecture simply because a cleaner architecture is suggested.
