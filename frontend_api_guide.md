# Frontend API Integration Guide

This guide details all available endpoints for integrating your frontend application with the `NotesAndFileBackend`.

## Base URL
When running locally: `http://localhost:5001/api/v1`
When running in production (VPS): `http://YOUR_VPS_IP:5001/api/v1` or `https://api.yourdomain.com/api/v1`

## Authentication

Most endpoints require a JWT Access Token. To authenticate, include the token in the `Authorization` header of your HTTP requests:

```http
Authorization: Bearer <your_access_token>
```

---

## 1. Auth Service (`/auth`)

Endpoints for user registration and authentication.

### Sign Up
- **Method:** `POST /auth/sign-up`
- **Description:** Register a new user account.
- **Request Body (JSON):**
  ```json
  {
    "email": "user@example.com",
    "password": "your_password",
    "displayName": "John Doe",
    "deviceName": "MyLaptop",
    "platform": "Web"
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "accessToken": "eyJhbGci...",
    "refreshToken": "base64_string",
    "userId": "uuid",
    "deviceId": "uuid"
  }
  ```

### Sign In
- **Method:** `POST /auth/sign-in`
- **Description:** Authenticate an existing user.
- **Request Body (JSON):**
  ```json
  {
    "email": "user@example.com",
    "password": "your_password",
    "deviceName": "MyLaptop",
    "platform": "Web"
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "accessToken": "eyJhbGci...",
    "refreshToken": "base64_string",
    "userId": "uuid",
    "deviceId": "uuid"
  }
  ```

---

## 2. File Storage Service (`/files`)

*(Requires Authentication)*

### Upload File
- **Method:** `POST /files/upload`
- **Description:** Uploads a file to cloud storage (MinIO) securely.
- **Request Body:** `multipart/form-data`
  - Key: `file` (The binary file you are uploading)
- **Response:** `200 OK` returns a `StoredFile` metadata object.

### List Files
- **Method:** `GET /files`
- **Description:** Retrieves all active files owned by the authenticated user.
- **Query Parameters (Optional):**
  - `search` (string): Filter files by name (e.g., `?search=vacation`).
  - `sortBy` (string): Field to sort by. Options: `date` (default) or `size`.
  - `sortOrder` (string): Sort direction. Options: `desc` (default, e.g., newest/largest first) or `asc`.
  - *Example:* `GET /files?search=report&sortBy=size&sortOrder=desc`
- **Response:** `200 OK` returns an array of `StoredFile` objects.

### Get File Metadata
- **Method:** `GET /files/{id}`
- **Description:** Get metadata for a specific file.
- **Response:** `200 OK` returns a `StoredFile` object.

### Download File
- **Method:** `GET /files/{id}/download`
- **Description:** Downloads the raw file securely. The API proxies the file directly from MinIO to the frontend, so the frontend receives the raw binary data.
- **Response:** `200 OK` (File Stream)

### Delete File
- **Method:** `DELETE /files/{id}`
- **Description:** Soft-deletes a file and queues it for permanent storage deletion.
- **Response:** `204 No Content`

### Generate Public Link
- **Method:** `POST /files/{id}/share`
- **Description:** Generates a public share link for a file.
- **Request Body (JSON):**
  ```json
  {
    "alias": "my-vacation-photo", 
    "expiresInHours": 24
  }
  ```
  *(Note: `alias` and `expiresInHours` are optional. If `alias` is omitted, it generates a secure random 32-character base62 string. If `alias` is provided, it generates `{alias}_{randomNumber}`)*
- **Response:** `200 OK`
  ```json
  {
    "id": "uuid",
    "token": "my-vacation-photo_8492",
    "publicUrl": "http://api.yourdomain.com/api/v1/public/files/my-vacation-photo_8492",
    "expiresAt": "2026-08-30T..."
  }
  ```

### Revoke Public Link
- **Method:** `DELETE /files/{id}/share/{shareId}`
- **Description:** Revokes a previously generated public share link.
- **Response:** `204 No Content`

---

## 3. Documents (Notes/Scripts) Service (`/documents`)

*(Requires Authentication)*

### Create Document
- **Method:** `POST /documents`
- **Description:** Create a new empty note/script document.
- **Request Body (JSON):**
  ```json
  {
    "title": "My First Note",
    "description": "Optional short summary"
  }
  ```
- **Response:** `201 Created` returns the newly created `Document` object.

### List Documents
- **Method:** `GET /documents`
- **Description:** Retrieve a lightweight list of all active documents (without heavy blocks/attachments).
- **Response:** `200 OK` returns an array of lightweight document objects.

### Get Full Document
- **Method:** `GET /documents/{id}`
- **Description:** Retrieve the full document, including all of its content blocks and attachments.
- **Response:** `200 OK` returns the full `Document` object.

### Update Document (Optimistic Concurrency)
- **Method:** `PUT /documents/{id}`
- **Description:** Update the title and description of a document. Uses optimistic locking to prevent overwriting someone else's changes.
- **Request Body (JSON):**
  ```json
  {
    "title": "Updated Title",
    "description": "Updated summary",
    "revision": 1 
  }
  ```
  *(Note: The `revision` number MUST match the `revision` number currently on the server, otherwise it returns `409 Conflict`)*
- **Response:** `200 OK` returns the updated document with an incremented revision number.

### Generate Public Link
- **Method:** `POST /documents/{id}/share`
- **Description:** Generates a public share link for a document.
- **Request Body (JSON):** Same as the Files share endpoint (`alias` and `expiresInHours` are optional).
- **Response:** `200 OK` returns share token and URL.

### Revoke Public Link
- **Method:** `DELETE /documents/{id}/share/{shareId}`
- **Description:** Revokes a previously generated public share link.
- **Response:** `204 No Content`

### Delete Document
- **Method:** `DELETE /documents/{id}`
- **Description:** Soft-deletes a document.
- **Response:** `204 No Content`

---

## 4. Public Access (`/public`)

*(Does NOT Require Authentication)*

### Access Shared File
- **Method:** `GET /public/files/{token}`
- **Description:** Accesses a shared file. The server will stream the raw binary file data directly back to the client. This means you can simply put this URL in an `<a href="">` or `<img src="">` tag and the browser will automatically render or download the file securely.

### Access Shared Document
- **Method:** `GET /public/documents/{token}`
- **Description:** Retrieves the full document payload in read-only mode for unauthenticated users.
- **Response:** `200 OK` returns the `Document` object. Returns `404 Not Found` if the token is invalid, expired, or revoked.

---

## 5. Admin Dashboard (`/admin`)

*(Requires Authentication + Must be the Admin User)*

### Get System Metrics
- **Method:** `GET /admin/metrics`
- **Description:** Retrieves real-time statistics for the dashboard.
- **Response:** `200 OK`
  ```json
  {
    "totalUsers": 1,
    "totalFiles": 15,
    "totalStorageUsed": 1048576,
    "totalDocuments": 5
  }
  ```
