# MAUI Client API Integration Guide (v1)

This document is the official API contract for integrating the existing `.NET MAUI` application with the new `NotesAndFileBackend.Api`.
It defines all authentication, note, command generation, import/export, and sharing endpoints expected by the frontend.

## Base URL
When running locally: `http://localhost:5001/api/v1` or `https://localhost:5000/api/v1`
When running in production (VPS): `https://api.yourdomain.com/api/v1`

## API Authentication & Security Boundary
- **Access Token:** Most endpoints require a short-lived JWT Access Token. Provide it in the header:
  `Authorization: Bearer <your_access_token>`
- **Idempotency Key:** State-mutating operations like Sharing, Import, and Export expect an `Idempotency-Key` header (usually a UUID) to prevent duplicate executions across network retries.
- **Client Validation:** The MAUI app is responsible for fast UX feedback validation. However, the API serves as the absolute security boundary and will perform comprehensive domain validation before executing any operation.

## Error Contract (RFC 7807)
The API strictly uses `application/problem+json` for validation and domain errors.
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["Title cannot be longer than 300 characters."],
    "Summary": ["Summary must be at least 10 characters long."]
  }
}
```

---

## 1. Auth / Tokens (`/auth`)
*The integration should support JWT access tokens and Secure Storage of refresh tokens in the MAUI client.*

### Sign In
- **Method:** `POST /auth/sign-in`
- **Request Body:**
  ```json
  { "email": "user@example.com", "password": "your_password" }
  ```
- **Response (200 OK):**
  ```json
  { "accessToken": "...", "refreshToken": "...", "userId": "..." }
  ```

---

## 2. Notes & Search CRUD (`/notes`)
*The Note List contract is deliberately lightweight. Full Note JSONB content is only downloaded when retrieving a specific note.*

### List Notes
- **Method:** `GET /notes`
- **Query Params:** `includeArchived (bool)`, `page (int)`, `pageSize (int, max 100)`
- **Response (200 OK):** Array of lightweight `NoteListItemDto`:
  ```json
  [
    {
      "id": "uuid",
      "title": "Nmap Full Scan",
      "summary": "Short description...",
      "category": "Networking",
      "tags": ["nmap", "scanning"],
      "toolName": "nmap",
      "isFavorite": true,
      "isPinned": false,
      "isArchived": false,
      "updatedAt": "2026-08-29T..."
    }
  ]
  ```

### Get Full Note
- **Method:** `GET /notes/{id}`
- **Response (200 OK):**
  ```json
  {
    "id": "uuid",
    "title": "Nmap Full Scan",
    "summary": "...",
    "category": "Networking",
    "tags": ["nmap"],
    "toolName": "nmap",
    "contentJsonb": "{ \"version\": 2, \"blocks\": [...] }",
    "isPinned": false,
    "isFavorite": true,
    "isArchived": false,
    "version": 2,
    "visibility": "PRIVATE",
    "createdAt": "...",
    "updatedAt": "...",
    "publicShares": [ { "id": "...", "tokenHash": "...", "expiresAt": "..." } ]
  }
  ```

### Create Note
- **Method:** `POST /notes`
- **Request Body:**
  ```json
  {
    "title": "My Note",
    "summary": "...",
    "categoryId": "uuid",
    "tags": ["tag1", "tag2"],
    "toolName": "custom",
    "content": { "version": 2, "blocks": [] },
    "visibility": "PRIVATE",
    "isPinned": false,
    "isFavorite": false
  }
  ```

### Update Note (Optimistic Concurrency)
- **Method:** `PUT /notes/{id}`
- **Description:** Updates the note. The `version` field must match the server's current version, otherwise `409 Conflict` is returned. You can also specify `"visibility": "PRIVATE" | "PUBLIC"`.

### Duplicate Note
- **Method:** `POST /notes/{id}/duplicate`
- **Description:** Creates a deep copy of the note and tags. Appends `(Copy)` to the title.

### Search Notes (PostgreSQL Full-Text Search)
- **Method:** `GET /notes/search`
- **Query Params:** `q (string)`, `categoryId (uuid)`, `tag (string)`, `tool (string)`, `page (int)`, `pageSize (int)`
- **Response (200 OK):**
  ```json
  {
    "items": [ /* NoteListItemDto */ ],
    "page": 1,
    "pageSize": 20,
    "total": 45
  }
  ```

### Search Inside Note (Block Search)
- **Method:** `GET /notes/{id}/search?q=query`
- **Response (200 OK):** Returns matched JSON blocks and snippets.

---

## 2.5 Note Attachments (`/note-attachments`)
*Attachments specifically embedded inside a note's blocks.*

### Upload Note Attachment
- **Method:** `POST /note-attachments`
- **Headers:** `Content-Type: multipart/form-data`
- **Description:** Upload an attachment for a note. Max 10MB for downloadable files, configurable limit (default 50MB) for display (images/videos).
- **Request Body:**
  - `file`: The file payload
  - `attachmentType`: `"Display"` or `"Downloadable"`
  - `displayName`: Optional custom name
- **Response (200 OK):**
  ```json
  {
    "id": "uuid",
    "attachmentType": "Display",
    "displayName": "screenshot.png",
    "mimeType": "image/png",
    "sizeBytes": 102450
  }
  ```

### Preview Display Attachment
- **Method:** `GET /note-attachments/{id}/preview`
- **Description:** Returns the raw byte stream of an image/video for inline display.

### Download Attachment
- **Method:** `GET /note-attachments/{id}/download`
- **Description:** Returns the file with a `Content-Disposition: attachment` header forcing a safe download.


---

## 3. Command Generators (`/command-generators`)

### Get Command Generator Schema
- **Method:** `GET /command-generators/{id}`
- **Description:** Provides metadata for the MAUI app to render the generator dynamically without hardcoding fields.
- **Response (200 OK):**
  ```json
  {
    "id": "uuid",
    "name": "Nmap Scan",
    "toolName": "nmap",
    "fields": [
      {
        "key": "target",
        "label": "Target IP/Hostname",
        "type": "target",
        "required": true
      }
    ],
    "language": "javascript",
    "script": "return `nmap ${inputs.target}`;",
    "template": "nmap {target}"
  }
  ```

*Note: `language` will be `"csharp_template"` for legacy generators using the `template` field, and `"javascript"` for new Jint generators using the `script` field.*

### Generate Command
- **Method:** `POST /command-generators/{id}/generate`
- **Description:** Evaluates the user's input against the deterministic C# engine. The server **never** executes the generated command.
- **Request Body:**
  ```json
  {
    "values": {
      "target": "192.168.1.1"
    }
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "success": true,
    "command": "nmap 192.168.1.1",
    "displayCommand": "nmap 192.168.1.1",
    "warnings": []
  }
  ```

### Test Command Generator (Drafting)
- **Method:** `POST /command-generators/{id}/test`
- **Description:** Allows evaluating a drafted script via the Jint sandbox without saving it to the database. Useful for a live preview editor.
- **Request Body:**
  ```json
  {
    "script": "return `nmap ${inputs.target}`;",
    "language": "javascript",
    "values": {
      "target": "192.168.1.1"
    }
  }
  ```
- **Response (200 OK):** Same response format as `/generate` (includes `success`, `command`, `errors`).

---

## 4. Sharing & Import / Export (`/notes`)

### Create Share Link
- **Method:** `POST /notes/{id}/share`
- **Headers:** `Idempotency-Key: <uuid>`
- **Request Body:**
  ```json
  {
    "alias": "my-nmap-cheatsheet",
    "expiresInHours": 24,
    "password": "optional_password",
    "allowIndexing": false,
    "maxViews": 100
  }
  ```
- **Response (200 OK):** `token` and full `publicUrl` to `/s/{token}`.

### Revoke Share
- **Method:** `DELETE /notes/{id}/share/{shareId}`

### Export Data
- **Method:** `POST /notes/export`
- **Headers:** `Idempotency-Key: <uuid>`
- **Request Body:**
  ```json
  {
    "format": "json",
    "includeAttachments": true
  }
  ```
- **Response (200 OK):** Returns raw file stream (`application/zip` or `application/json`).

### Import Data
- **Method:** `POST /notes/import`
- **Headers:** `Idempotency-Key: <uuid>`
- **Request Body:** Requires `multipart/form-data` with key `file`.
- **Response (200 OK):** `ImportResultDto` indicating success, conflicts, or errors.

---

## 5. Public Access (`/s` / `/public`)
*No Authentication Required. Protected by `StrictPolicy` Rate Limiting.*

### View Shared Note
- **Method:** `GET /public/Notes/{token}`
- **Query Params:** `pwd` (if password-protected)
- **Content Negotiation:**
  - If the client sends `Accept: text/html`, the backend will dynamically generate a beautiful HTML page rendering the note blocks, handling password prompts, and returning `410 Gone` if the token expired.
  - If the client (like a MAUI app importer) sends `Accept: application/json`, it returns the raw API JSON representation.

---

## 6. Files Management (`/files`)

### Upload File
- **Method:** `POST /files/upload`
- **Description:** Uploads a file (up to 20GB). Enforces user quotas.
- **Request Body:** `multipart/form-data` containing the key `file`.
- **Response (200 OK):**
  ```json
  {
    "id": "uuid",
    "originalFilename": "report.pdf",
    "mimeType": "application/pdf",
    "extension": ".pdf",
    "byteSize": 1048576,
    "status": "ACTIVE",
    "storageBackend": "LOCAL",
    "retentionExpiresAt": null,
    "createdAt": "2026-08-30T...",
    "updatedAt": "2026-08-30T...",
    "publicShares": []
  }
  ```

### List Files
- **Method:** `GET /files`
- **Query Params:** `search (string)`, `sortBy (date|size)`, `sortOrder (asc|desc)`
- **Response (200 OK):** Array of File DTOs (same format as upload response).

### Get File Metadata
- **Method:** `GET /files/{id}`
- **Response (200 OK):** Single File DTO.

### Download File
- **Method:** `GET /files/{id}/download`
- **Response (200 OK):** Raw file stream (e.g. `application/pdf`).

### Delete File
- **Method:** `DELETE /files/{id}`
- **Response (204 No Content)**
