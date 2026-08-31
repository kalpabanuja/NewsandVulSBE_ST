# Backend Service Implementation Instructions

## 1. Purpose

Build a backend service that adds two related capabilities to the application:

1. **File Share Service**
   - Upload files from an authenticated application/device.
   - Store and track files with ownership, storage usage, expiration, and sharing state.
   - Allow an owner to share a file with all application users/devices.
   - Generate an externally shareable URL when the owner enables external sharing.
   - Allow recipients to preview/open/download files when supported.
   - Keep deletion and management permissions restricted to the file owner.

2. **Shared Notes / Scripts Service**
   - Store rich, web-page-like documents for topics such as Nmap, Metasploit, configuration procedures, setup notes, commands, and other operational documentation.
   - Support headings, paragraphs, formatted text, code blocks/cards, and file attachments.
   - Make documents available to authenticated application devices.
   - Allow owners/authors to create, edit, share, expire public links, and delete documents as appropriate.
   - Allow public visitors to access a document through a temporary share URL without receiving authenticated application access.

The source requirements explicitly call for a separate file-sharing data area and another area for notes/scripts, per-device uploading, owner-only management, device synchronization, temporary sharing, external links, and storage limits. fileciteturn0file0L3-L6

---

# Mandatory Authentication and Account Access

Authentication is a **hard requirement** for accessing the application. Users must sign up or sign in before accessing the main application, Files, Notes & Scripts, device synchronization, or account-specific APIs.

## Authentication flow

```text
Application launch → Check session → Valid? → Main Application
                                  ↓ No
                           Sign In / Sign Up
                                  ↓
                           Authenticated session
                                  ↓
                           Main Application
```

Recommended endpoints:
```http
POST /api/v1/auth/sign-up
POST /api/v1/auth/sign-in
POST /api/v1/auth/sign-out
POST /api/v1/auth/refresh
GET  /api/v1/auth/me
```

User records should include `id`, `email/username`, `display_name`, `password_hash`, `status`, `email_verified_at`, `last_login_at`, `created_at`, and `updated_at`. Passwords must never be stored in plaintext.

The backend must validate the authenticated session/token on every protected request, support session expiry/refresh according to policy, revoke sessions on sign-out, support multiple devices per account, and derive ownership from the authenticated identity rather than client-supplied user IDs.

Protected API areas include:
```text
/api/v1/files/*
/api/v1/documents/*
/api/v1/storage/*
/api/v1/devices/*
/api/v1/sync/*
/api/v1/activity/*
```

Only explicit public-share routes such as `/public/file/{token}` and `/public/document/{token}` may be unauthenticated, and those tokens must grant access only to their specific shared resource.

Unauthenticated access to protected APIs returns `401 Unauthorized` with an error such as `AUTHENTICATION_REQUIRED`.

---

# 2. Core Design Principles

Implement the service with these principles:

- **Authenticated-by-default** for all application APIs.
- **Owner-controlled management** for files and documents.
- **Read access can be broader than management access.**
- **Temporary sharing must never delete the original resource.**
- **Deleting a resource must immediately reclaim storage where applicable.**
- **Storage accounting must be server-authoritative.**
- **Expiration must be enforced server-side, not only by the UI.**
- **Public share tokens must be random, non-guessable, and revocable.**
- **All mutating operations should be audited.**
- **Device synchronization should use stable resource IDs rather than device-local IDs.**
- **Do not expose filesystem paths, internal database IDs, service credentials, or storage-provider credentials through public APIs.**

---

# 3. Recommended Backend Components

Use the following logical components. They may be implemented in one service initially, but the boundaries should remain clear.

```text
Backend Service
├── Authentication / Authorization
├── File Share Service
│   ├── Upload
│   ├── Download / Preview
│   ├── Storage Quotas
│   ├── Ownership
│   ├── Device Visibility
│   └── Share Links
├── Notes / Scripts Service
│   ├── Documents
│   ├── Rich Content
│   ├── Code Blocks
│   ├── Copy Cards
│   ├── Attachments
│   └── Share Links
├── Public Share Service
├── Storage Service
├── Background Jobs
├── Audit / Activity Logging
└── API / Validation Layer
```

Use object/blob storage for file contents. The relational database should store metadata rather than large binary file content wherever possible.

---

# 4. Authentication Model

Every authenticated application request must resolve to:

```text
user_id
device_id
session_id / access token
```

The backend must distinguish:

- **User** — the account/person using the service.
- **Device** — an installation or registered client (phone, laptop, tablet, etc.).
- **Resource owner** — user who uploaded/created the resource.
- **Public visitor** — unauthenticated person using a temporary public link.

A device must never be treated as the permanent owner identity. Ownership should belong to the authenticated user.

Recommended fields:

```text
users
- id
- display_name
- email / username
- status
- created_at
- updated_at

devices
- id
- user_id
- device_name
- platform
- app_version
- last_seen_at
- created_at
```

---

# 5. File Share Service

## 5.1 File Lifecycle

Recommended lifecycle:

```text
CREATED/PENDING
    ↓
UPLOADING
    ↓
ACTIVE
    ↓
OPTIONALLY SHARED
    ↓
EXPIRED SHARE / ACTIVE RESOURCE
    ↓
DELETED
```

Important distinction:

- **File expiration** means the file itself becomes unavailable/deleted according to the owner-selected retention policy.
- **Share-link expiration** means only the external/public link stops working. The original file remains available to authorized application users.

The second behavior is explicitly required for shared notes/documents and should also be used consistently for external file links.

---

# 6. File Metadata Schema

Create a table similar to:

```text
files
- id
- owner_user_id
- owner_device_id
- original_filename
- stored_filename / object_key
- mime_type
- extension
- byte_size
- checksum
- status
- retention_expires_at
- created_at
- updated_at
- deleted_at
```

Recommended additional metadata:

```text
files
- storage_backend
- storage_region
- upload_session_id
- virus_scan_status
- preview_status
```

Do not trust client-provided file size. The server must calculate the actual uploaded size.

---

# 7. File Upload Requirements

The API must support uploading a file from any registered device belonging to the user.

Recommended flow:

```text
1. Client asks backend to start upload.
2. Backend verifies authentication and quota.
3. Backend creates upload session.
4. Client uploads content.
5. Backend/object storage confirms received bytes.
6. Backend validates MIME/type/size.
7. Optional malware scan runs.
8. Backend finalizes metadata.
9. File becomes ACTIVE.
10. Client receives resource metadata.
```

For large files, prefer resumable/multipart uploads instead of sending the entire file through a single HTTP request.

The source requirement caps overall file storage at **30 GB**, with the UI showing remaining space and reclaiming space after deletion. fileciteturn0file0L3-L3

---

# 8. Storage Quota

Default quota:

```text
Maximum file storage = 30 GiB or 30 GB
```

Choose one unit convention and use it consistently throughout the backend and UI.

Recommended quota endpoint:

```http
GET /api/v1/storage/quota
```

Example response:

```json
{
  "limitBytes": 32212254720,
  "usedBytes": 10737418240,
  "availableBytes": 21474836480,
  "fileCount": 42,
  "maxFileCount": 500
}
```

Rules:

- Reject an upload if the resulting total would exceed the quota.
- Reject an upload that exceeds any per-file limit.
- Update usage atomically when an upload is finalized.
- Decrease usage immediately after successful deletion.
- Never calculate quota solely from client-side values.
- Provide a reconciliation/background job that can recompute usage from authoritative file records.

The original requirement indicates a maximum of roughly **500 files** and a total storage capacity of **30 GB**. fileciteturn0file0L3-L3

---

# 9. File Count Limit

Enforce:

```text
MAX_FILES = 500
```

The backend must reject the 501st active file even if storage remains.

Return a useful error:

```json
{
  "code": "FILE_COUNT_LIMIT_REACHED",
  "message": "Maximum number of stored files reached."
}
```

---

# 10. File Sharing With Application Users

The owner should have a simple sharing state:

```text
private
shared_with_all_users
```

When:

```text
shared_with_all_users = true
```

all authenticated application devices/users that are permitted by the product's global-access policy can discover/read the file.

Important:

- "Share with everyone" means **application users**, not the entire public internet.
- Public internet access should require a separate public-share operation/token.
- A user who can read a file must not automatically gain delete/edit/expiration-management permission.

Suggested table:

```text
file_access
- file_id
- access_type
- target_user_id nullable
- created_at
```

The initial product can use `ALL_AUTHENTICATED_USERS` without per-user ACLs, while keeping the database/API model extensible for future individual sharing.

---

# 11. Owner Permissions

For every file, the owner can:

- View
- Download
- Rename (optional but recommended)
- Set/modify file retention
- Enable/disable application-wide sharing
- Create/revoke public share link
- Delete

Other application users can:

- View if permitted
- Download if permitted
- Never delete another user's file
- Never change another user's expiration
- Never change another user's sharing state

Server-side authorization must enforce this regardless of frontend controls.

---

# 12. File Expiration

Support two separate timestamps:

```text
retention_expires_at
public_share_expires_at
```

Examples:

```text
File retention:
- never
- 1 hour
- 24 hours
- 7 days
- 30 days
- custom date/time

Public link:
- 1 hour
- 24 hours
- 7 days
- custom date/time
- never, only if product policy allows
```

When a **public share expires**:

- The public token stops working.
- The original file remains intact.
- Authenticated users still retain whatever access they previously had.

When **file retention expires**:

- The file becomes unavailable according to policy.
- Its public links become invalid.
- Storage usage is reclaimed after physical deletion is complete.

The requirement explicitly distinguishes expiring sharing from deleting the underlying document/resource. fileciteturn0file0L6-L6

---

# 13. Public Share Links

Do not expose sequential IDs.

Use a cryptographically secure token:

```text
https://share.example.com/f/<random-token>
```

Store:

```text
public_file_shares
- id
- file_id
- token_hash
- created_by_user_id
- expires_at
- revoked_at
- created_at
- last_accessed_at
- access_count
```

Store the token hash where practical rather than the raw token.

Recommended endpoints:

```http
POST   /api/v1/files/{fileId}/public-share
GET    /api/v1/files/{fileId}/public-share
PATCH  /api/v1/files/{fileId}/public-share
DELETE /api/v1/files/{fileId}/public-share

GET    /public/file/{token}
```

The client should normally receive the share URL immediately after the owner enables sharing. There should not be a confusing two-step "generate link" workflow when link generation is part of the share action.

---

# 14. File Preview / Download

For supported formats, return metadata enabling the application to open or preview the content.

Examples:

```text
PDF → browser/PDF viewer
Image → image preview
Text → text viewer
Office document → download or supported viewer
Archive → download
Unknown binary → download
```

For large files, use streaming or signed object-storage URLs.

Add:

- `Content-Type`
- `Content-Disposition`
- range request support where appropriate
- access checks before issuing download URLs

---

# 15. Notes / Scripts Service

Treat each note as a structured document rather than one giant plaintext string.

Recommended conceptual structure:

```text
Document
├── Metadata
├── Blocks
│   ├── Heading
│   ├── Paragraph
│   ├── Code
│   ├── Copy Card
│   ├── Quote / Callout
│   ├── List
│   ├── Attachment
│   └── Divider
└── Sharing Metadata
```

This directly supports the requested web-page-like editor with headings, formatted text, embedded small files, code, and copyable cards. fileciteturn0file0L6-L6

---

# 16. Notes Database Schema

Recommended:

```text
documents
- id
- owner_user_id
- owner_device_id
- title
- slug
- description
- status
- visibility
- revision
- created_at
- updated_at
- deleted_at
```

Then:

```text
document_blocks
- id
- document_id
- block_type
- position
- content_json
- created_at
- updated_at
```

Alternative:

Use a single JSON document body if the chosen editor already produces a stable structured JSON format. In that case, still keep the document metadata in normal columns.

---

# 17. Supported Block Types

Minimum:

```text
heading
paragraph
bold / italic / inline formatting
bullet_list
numbered_list
code_block
copy_card
attachment
divider
```

Recommended later:

```text
table
image
callout
quote
link
collapsible_section
```

Example code block:

```json
{
  "type": "code_block",
  "language": "bash",
  "code": "nmap -sV 192.168.1.0/24"
}
```

Example copy card:

```json
{
  "type": "copy_card",
  "title": "Scan HTTP services",
  "content": "nmap -p 80,443 -sV 192.168.1.0/24"
}
```

---

# 18. Attachments Inside Notes

Attachments in a document should reference file/blob objects rather than duplicating the entire storage system.

Recommended:

```text
document_attachments
- id
- document_id
- block_id
- object_key
- filename
- mime_type
- byte_size
- checksum
- created_at
```

Set a separate attachment-size limit suitable for the application.

The general file-share quota does not have to be identical to the document-editor attachment quota; make this explicit in configuration.

---

# 19. Document Sharing

Authenticated application users/devices should be able to access shared documents as product data.

For external access, create a temporary public document share:

```text
public_document_shares
- id
- document_id
- token_hash
- expires_at
- revoked_at
- created_by_user_id
- created_at
- last_accessed_at
```

Recommended endpoints:

```http
POST   /api/v1/documents/{id}/public-share
GET    /api/v1/documents/{id}/public-share
PATCH  /api/v1/documents/{id}/public-share
DELETE /api/v1/documents/{id}/public-share
GET    /public/document/{token}
```

When the share expires:

- Public URL stops working.
- Document is not deleted.
- Authenticated application users can still access the document.
- Owner can create a new share later.

This expiration behavior is explicitly part of the requested notes/scripts feature. fileciteturn0file0L6-L6

---

# 20. Document Permissions

Minimum roles:

```text
OWNER
VIEWER
PUBLIC_VIEWER
```

Owner can:

- Create
- Edit
- Delete
- Upload attachments
- Change title
- Change sharing
- Change share expiration
- Revoke public links

Viewer can:

- Read
- Open attachments
- Copy code/cards

Public viewer can:

- Read the published document only
- Open permitted attachments
- Cannot edit
- Cannot delete
- Cannot manage sharing

Design the permission model so future `EDITOR` or per-user sharing can be introduced without rewriting the database.

---

# 21. Versioning and Conflict Handling

Because the same user can use the application on a phone and laptop, document edits should use optimistic concurrency.

Keep:

```text
revision
updated_at
```

Client update:

```http
PUT /api/v1/documents/{id}
If-Match: <revision>
```

If an old revision attempts to overwrite a newer version:

```text
409 CONFLICT
```

Return enough data for the client to refresh and resolve the conflict.

For files, metadata updates should also use revision checking.

---

# 22. Device Synchronization

Provide endpoints such as:

```http
GET /api/v1/sync?cursor=<cursor>
```

Return changes:

```json
{
  "nextCursor": "...",
  "changes": [
    {
      "resourceType": "file",
      "resourceId": "...",
      "changeType": "created"
    }
  ]
}
```

Support:

```text
created
updated
deleted
sharing_changed
expired
```

Use cursor-based synchronization rather than repeatedly downloading the complete database.

Devices should cache metadata locally, but the backend remains authoritative.

---

# 23. API Structure

Suggested namespace:

```text
/api/v1/auth/*
/api/v1/devices/*
/api/v1/storage/*
/api/v1/files/*
/api/v1/documents/*
/api/v1/sync/*
/api/v1/activity/*
```

Public routes:

```text
/public/file/{token}
/public/document/{token}
```

Public endpoints must be isolated from authenticated management APIs.

---

# 24. Suggested File Endpoints

```http
GET    /api/v1/files
POST   /api/v1/files/upload
GET    /api/v1/files/{id}
PATCH  /api/v1/files/{id}
DELETE /api/v1/files/{id}

POST   /api/v1/files/{id}/public-share
GET    /api/v1/files/{id}/public-share
PATCH  /api/v1/files/{id}/public-share
DELETE /api/v1/files/{id}/public-share
```

Recommended query options:

```text
?owner=me
?shared=true
?search=
?sort=created_at
?order=desc
?limit=
?cursor=
```

---

# 25. Suggested Document Endpoints

```http
GET    /api/v1/documents
POST   /api/v1/documents
GET    /api/v1/documents/{id}
PUT    /api/v1/documents/{id}
PATCH  /api/v1/documents/{id}
DELETE /api/v1/documents/{id}

POST   /api/v1/documents/{id}/attachments
DELETE /api/v1/documents/{id}/attachments/{attachmentId}

POST   /api/v1/documents/{id}/public-share
GET    /api/v1/documents/{id}/public-share
PATCH  /api/v1/documents/{id}/public-share
DELETE /api/v1/documents/{id}/public-share
```

---

# 26. Validation Rules

Validate on the server:

### Files

- Maximum total quota: 30 GB/30 GiB according to configured convention.
- Maximum active file count: 500.
- Maximum individual file size: define a configurable value.
- Filename length and unsafe characters.
- MIME type versus detected file type.
- Empty/corrupt upload.
- Malicious filenames/path traversal attempts.
- Upload integrity/checksum.

### Documents

- Title required.
- Maximum title length.
- Maximum document size.
- Maximum number of blocks.
- Maximum attachment count/size.
- Valid JSON structure for structured content.
- Allowed code-language identifiers.
- Sanitize HTML/Markdown content if rendered to browsers.

---

# 27. Security Requirements

At minimum:

- TLS everywhere outside local development.
- Secure session/token storage.
- Rate-limit upload and public-share endpoints.
- Rate-limit public token access.
- Validate every file upload.
- Perform malware scanning if infrastructure supports it.
- Never trust client-supplied ownership or permission fields.
- Prevent IDOR/BOLA by checking resource ownership/authorization on every request.
- Sanitize rich content to prevent XSS.
- Escape/render code safely.
- Use secure random public tokens.
- Hash public tokens in the database when feasible.
- Support share revocation.
- Avoid leaking whether arbitrary private resource IDs exist.
- Log administrative/security-sensitive changes.

---

# 28. Audit Logging

Create:

```text
audit_events
- id
- user_id nullable
- device_id nullable
- event_type
- resource_type
- resource_id
- metadata_json
- ip_hash / approved privacy-safe network identifier
- created_at
```

Examples:

```text
file.uploaded
file.deleted
file.shared
file.unshared
file.public_link_created
file.public_link_revoked
file.expired
document.created
document.updated
document.deleted
document.public_link_created
document.public_link_revoked
```

Do not log file contents or authentication secrets.

---

# 29. Background Jobs

Use a job worker/scheduler for:

- Expiring files.
- Expiring public shares.
- Physical blob deletion.
- Storage reconciliation.
- Virus scanning.
- Document indexing/search indexing if added.
- Cleaning abandoned multipart uploads.
- Rebuilding previews/thumbnails if supported.

Expiration should be checked by the API at request time as a second safety measure; do not rely only on the scheduled job.

---

# 30. Search

Add server-side search for:

### Files

```text
filename
type
owner
created date
expiration
shared state
```

### Documents

```text
title
description
body text
block text
code card titles
```

Start with database search. Add full-text indexing only when scale requires it.

---

# 31. Storage Architecture

Recommended:

```text
Database
    ├── metadata
    ├── users
    ├── permissions
    ├── links
    └── audit data

Object Storage
    ├── files/
    ├── document-attachments/
    └── temporary-upload-parts/
```

Never store a 30 GB binary object directly in a normal relational table unless there is a very strong infrastructure reason.

---

# 32. Deletion Semantics

When an owner deletes a file:

1. Confirm ownership.
2. Mark database resource deleted/DELETING.
3. Revoke all public shares.
4. Prevent new downloads.
5. Delete physical blob.
6. Mark record deleted or purge it according to retention policy.
7. Recalculate/decrement storage usage.
8. Emit audit event.
9. Notify/sync other devices that the file is gone.

A race condition must not allow a concurrent upload/download to produce an incorrect quota state.

---

# 33. Observability

Expose:

```text
health endpoint
readiness endpoint
metrics
structured logs
request IDs
job status
storage usage metrics
upload failure metrics
public-link usage
```

Recommended metrics:

```text
files_total
file_bytes_used
file_upload_failures
file_downloads
public_share_requests
document_count
document_update_conflicts
expired_resources
```

---

# 34. Error Format

Use one consistent API error format:

```json
{
  "error": {
    "code": "STORAGE_QUOTA_EXCEEDED",
    "message": "The upload would exceed the available storage.",
    "details": {
      "availableBytes": 1048576,
      "requestedBytes": 5242880
    },
    "requestId": "..."
  }
}
```

Use correct HTTP statuses:

```text
400 invalid request
401 unauthenticated
403 forbidden
404 not found
409 conflict
413 payload too large
422 validation failure
429 rate limited
500 server error
503 service unavailable
```

---

# 35. Testing Requirements

Implement tests for:

### File service

- Upload succeeds.
- Upload is rejected at quota.
- Upload is rejected at file-count limit.
- Usage increases after upload.
- Usage decreases after deletion.
- Owner can delete.
- Non-owner cannot delete.
- Owner can change expiration.
- Non-owner cannot change expiration.
- Application-wide sharing works.
- Public link works before expiration.
- Public link fails after expiration.
- Revoked link fails immediately.
- Underlying resource survives share-link expiration.
- Device A uploads and Device B sees the resource.

### Document service

- Create document.
- Edit document.
- Render blocks correctly.
- Copy-card data survives round trip.
- Attachments upload/delete correctly.
- Owner permissions work.
- Public link works.
- Share expiration does not delete document.
- Concurrent update returns conflict.
- XSS payloads are sanitized.

---

# 36. Acceptance Criteria

The backend is complete when:

- A user can upload files from any registered device.
- Storage usage and remaining capacity are visible through an API.
- Total storage cannot exceed 30 GB/30 GiB.
- Active files cannot exceed 500.
- Users can share a file with application users.
- Only the owner can manage/delete the file.
- Public links can be created and copied through the application.
- Public links have expiration and revocation.
- Expired public links do not delete the original file.
- A phone and laptop logged into the same account see synchronized resources.
- Documents support rich structured content.
- Documents support code/copy cards.
- Documents support attachments.
- Documents can be temporarily shared externally.
- Public document links expire without deleting the document.
- Security and authorization are enforced server-side.
