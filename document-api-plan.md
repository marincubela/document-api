
# Document API — Simplified Implementation Plan (College Project)

**Goal:** Build a small, working REST API that uploads, stores, retrieves, and emails documents.  
**Scope:** Minimal features to meet requirements; light security; no heavy logging, no advanced testing, minimal future-proofing.

> If you'd like, I can add: (a) a simple file virus scan, or (b) one-click Docker compose for API + DB + MailHog. Just say the word.

---

## 1) Tech Stack (Simple & Windows‑friendly)

- **Language/Framework:** C#/.NET 8 — ASP.NET Core Web API (Controllers)
- **Database:** SQL Server (LocalDB is fine) via **EF Core** (migrations)
- **Storage:** Local file system folder (e.g., `./storage`)
- **Email:** SMTP using System.Net.Mail (or MailKit). For dev, use **MailHog** (optional)
- **Auth:** JWT (HS256, single symmetric secret in appsettings)
- **Docs:** Swagger (Swashbuckle)

Why this stack? It’s straightforward on Windows, easy to scaffold, and meets all requirements without extra complexity.

---

## 2) Database Schema (Minimal)

### `users`
- `id` (GUID, PK)
- `email` (nvarchar(256), unique)
- `password_hash` (nvarchar(255))
- `display_name` (nvarchar(128), nullable)
- `created_at` (datetime2 UTC, default now)
- `is_active` (bit, default 1)

### `roles`
- `id` (int, identity, PK)
- `name` (nvarchar(32), unique) — `user`, `admin`

### `user_roles`
- `user_id` (GUID, FK → users.id)
- `role_id` (int, FK → roles.id)
- **PK**: (`user_id`, `role_id`)

### `documents`
- `id` (GUID, PK)
- `owner_user_id` (GUID, FK → users.id)
- `original_filename` (nvarchar(255))
- `content_type` (nvarchar(127))
- `size_bytes` (bigint)
- `storage_key` (nvarchar(512)) — relative path like `yyyy/MM/dd/{docId}/{safeName}`
- `uploaded_at` (datetime2 UTC, default now)

### `email_logs` (very basic)
- `id` (GUID, PK)
- `document_id` (GUID, FK → documents.id)
- `sender_user_id` (GUID, FK → users.id)
- `recipient_email` (nvarchar(256))
- `status` (nvarchar(32)) — `sent` or `failed`
- `provider_message_id` (nvarchar(256), nullable)
- `error_message` (nvarchar(512), nullable)
- `created_at` (datetime2 UTC, default now)

---

## 3) Endpoints (Exact Requirements, Kept Simple)

### 3.1 `POST /documents` — Upload
- **Auth:** (Phase 1 optional), Phase 2 protected
- **Consumes:** `multipart/form-data` (field: `file`)
- **Allow types:** PDF (`application/pdf`), DOCX (`application/vnd.openxmlformats-officedocument.wordprocessingml.document`)
- **Max size:** 20 MB (config)
- **On success:** Save file to local storage, insert metadata
- **201 Created** body:
```json
{
  "document_id": "uuid",
  "filename": "original.docx",
  "content_type": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
  "size_bytes": 12345,
  "uploaded_at": "2025-11-02T12:34:56Z"
}
```
- **Errors:** `400` (validation), `413` (too large), `415` (bad type), `500`

### 3.2 `GET /documents/{document_id}` — Metadata or Download
- **Auth:** (Phase 1 optional), Phase 2 protected + ownership/admin
- **Query:** `download=true|false` (default false)
- **200 OK (metadata)**:
```json
{
  "document_id": "uuid",
  "filename": "file.pdf",
  "content_type": "application/pdf",
  "size_bytes": 654321,
  "uploaded_at": "2025-11-02T12:34:56Z",
  "owner_user_id": "uuid"
}
```
- **200 OK (download):** binary stream with correct `Content-Type` and `Content-Disposition`
- **Errors:** `404` (not found), `403` (forbidden in Phase 2)

### 3.3 `POST /documents/{document_id}/send` — Email Attachment
- **Auth:** (Phase 1 optional), Phase 2 protected + ownership/admin
- **Body:**
```json
{
  "to": "recipient@example.com",
  "subject": "optional",
  "message": "optional"
}
```
- **Result:** Attempt SMTP send; log to `email_logs`
- **200 OK** (sent) or **202 Accepted** (if you queue later; for now use 200)
```json
{
  "status": "sent",
  "recipient": "recipient@example.com",
  "provider_message_id": "abc123"
}
```
- **Errors:** `400` (bad email), `404` (no such doc), `500`

### 3.4 Auth (Phase 2)
#### `POST /auth/register` (optional)
```json
{ "email": "user@example.com", "password": "Str0ng#Pass", "display_name": "User" }
```
- **201 Created** (returns user id/email)

#### `POST /auth/login`
```json
{ "email": "user@example.com", "password": "Str0ng#Pass" }
```
- **200 OK**:
```json
{ "access_token": "jwt", "expires_in": 3600, "token_type": "Bearer" }
```

**RBAC rules (simple):**
- Roles: `user`, `admin`
- `POST /documents`: both roles allowed
- `GET /documents/{id}`: `user` → own docs only; `admin` → any
- `POST /documents/{id}/send`: same rule as above

---

## 4) File Storage (Local Only for Project)

- Root folder: configurable (e.g., `STORAGE_ROOT=./storage`)
- Path: `yyyy/MM/dd/{documentId}/{safeFileName}`
- Save file via atomic write (write temp then move), ensure directory exists
- Sanitize filename; detect MIME via basic header check to avoid obvious spoofing

---

## 5) Email Integration (SMTP)

- App settings: `Smtp:Host`, `Port`, `EnableSsl`, `User`, `Pass`, `From`
- Use System.Net.Mail (or MailKit) to send:
  1. Create `MailMessage` with `To`, `Subject`, `Body`
  2. Attach file stream from storage
  3. `SmtpClient.Send` (async preferred)
- On success/failure, insert into `email_logs`

---

## 6) JWT (Simple)

- **HS256** with one secret from `appsettings.json` or environment variable
- Claims: `sub` (user id), `email`, `roles` (array), `exp`
- Lifetime: 60 minutes is fine
- No refresh tokens (keep it simple)
- Validate via middleware; read `roles` claim for RBAC checks

---

## 7) Minimal Security Notes

- Hash passwords with ASP.NET Identity (PBKDF2) or BCrypt
- Only accept PDF/DOCX; check MIME + extension
- Limit upload size in Kestrel and controller
- Authorization: check owner (`owner_user_id == userId`) or `admin` role
- Use HTTPS in dev if possible (or at least don’t send credentials over plain HTTP)
- Do not log secrets or full tokens

---

## 8) Step-by-Step Tasks

### Phase 1 — Core
1. Scaffold Web API project; add Swagger.
2. Add EF Core, DbContext, and entities; run first migration.
3. Create `IFileStorage` interface + simple Local implementation.
4. Implement `POST /documents` (validate file ⇒ save ⇒ insert row ⇒ return metadata).
5. Implement `GET /documents/{id}` (metadata or stream on `download=true`).
6. Create SMTP sender and implement `POST /documents/{id}/send`.
7. Seed a demo user (manual SQL or small seeding code) for quick testing.

### Phase 2 — Auth + RBAC
1. Add `/auth/register` (optional) and `/auth/login`.
2. Issue JWT (HS256) with `roles` claim.
3. Protect document endpoints with `[Authorize]`.
4. Add ownership check for `GET` and `SEND`; admins bypass.
5. Add role seeding: `user`, `admin` (simple console init or migration seed).

---

## 9) Quick Manual Testing (No Test Framework Needed)

- **Upload:**
  - curl: `curl -F "file=@test.pdf" http://localhost:5000/documents`
- **Get metadata:**
  - `GET /documents/{id}`
- **Download:**
  - `GET /documents/{id}?download=true`
- **Send email:**
  - `POST /documents/{id}/send` with JSON body and valid SMTP settings
- **Auth (Phase 2):**
  - `POST /auth/login` → use `Authorization: Bearer <token>` for protected endpoints

---

## 10) Minimal Deployment

- Run locally: `dotnet run`
- Change `appsettings.json` for DB connection, STORAGE_ROOT, and SMTP
- Optional: small Dockerfile if needed later

---

## 11) Folder Layout (Suggested)

```
/src
  /Api
    Controllers/
    Dtos/
    Auth/
    Storage/
    Email/
    Program.cs
    appsettings.json
  /Infrastructure
    Data/
    Entities/
    Migrations/
/storage (created at runtime)
```

---

## 12) Done Criteria

- Can upload PDF/DOCX and get a `document_id`
- Can fetch metadata and download the file
- Can send the file by email via SMTP
- After Phase 2: Login returns JWT; RBAC enforced (owner/admin)

---
