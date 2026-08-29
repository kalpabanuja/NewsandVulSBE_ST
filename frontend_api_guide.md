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
    "contentJsonb": "{ \"blocks\": [...] }",
    "isPinned": false,
    "isFavorite": true,
    "isArchived": false,
    "version": 2,
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
    "content": { "blocks": [] },
    "isPinned": false,
    "isFavorite": false
  }
  ```

### Update Note (Optimistic Concurrency)
- **Method:** `PUT /notes/{id}`
- **Description:** Updates the note. The `version` field must match the server's current version, otherwise `409 Conflict` is returned.

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
    "template": "nmap {target}"
  }
  ```

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
- **Method:** `GET /public/notes/{token}`
- **Query Params:** `pwd` (if password-protected)
- **Response:** Returns the read-only Note representation for unauthenticated rendering.
