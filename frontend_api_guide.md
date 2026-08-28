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
- **Response:** `200 OK` returns an array of `StoredFile` objects.

### Get File Metadata
- **Method:** `GET /files/{id}`
- **Description:** Get metadata for a specific file.
- **Response:** `200 OK` returns a `StoredFile` object.

### Download File (Presigned URL)
- **Method:** `GET /files/{id}/download`
- **Description:** Generates a temporary, secure URL directly to the cloud storage bucket so the frontend can download the raw file without proxying binary data through the API.
- **Response:** `200 OK`
  ```json
  {
    "downloadUrl": "http://minio:9000/bucket/file.jpg?X-Amz-Signature=..."
  }
  ```

### Delete File
- **Method:** `DELETE /files/{id}`
- **Description:** Soft-deletes a file and queues it for permanent storage deletion.
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

### Delete Document
- **Method:** `DELETE /documents/{id}`
- **Description:** Soft-deletes a document.
- **Response:** `204 No Content`

---

## 4. Admin Dashboard (`/admin`)

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
