# Document API - Quick Reference

## 🌐 Base URL
```
http://localhost:5072
```

## 📖 Swagger UI
```
http://localhost:5072/swagger
```

## 🔐 Authentication Endpoints

### Register User
```http
POST /auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!",
  "displayName": "John Doe"
}
```

**Response:** 201 Created
```json
{
  "userId": "guid",
  "email": "user@example.com",
  "displayName": "John Doe"
}
```

### Login
```http
POST /auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!"
}
```

**Response:** 200 OK
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

## 📄 Document Endpoints (Protected)

### Upload Document
```http
POST /documents
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: [PDF or DOCX file, max 20MB]
```

**Response:** 201 Created
```json
{
  "documentId": "guid",
  "filename": "document.pdf",
  "contentType": "application/pdf",
  "sizeBytes": 12345,
  "uploadedAt": "2025-11-02T18:35:00Z",
  "ownerUserId": "guid"
}
```

### Get Document Metadata
```http
GET /documents/{id}
Authorization: Bearer {token}
```

**Response:** 200 OK
```json
{
  "documentId": "guid",
  "filename": "document.pdf",
  "contentType": "application/pdf",
  "sizeBytes": 12345,
  "uploadedAt": "2025-11-02T18:35:00Z",
  "ownerUserId": "guid"
}
```

### Download Document
```http
GET /documents/{id}?download=true
Authorization: Bearer {token}
```

**Response:** 200 OK (File stream)

### Send Document via Email
```http
POST /documents/{id}/send
Authorization: Bearer {token}
Content-Type: application/json

{
  "to": "recipient@example.com",
  "subject": "Document for Review",
  "message": "Please review the attached document."
}
```

**Response:** 200 OK
```json
{
  "status": "sent",
  "recipient": "recipient@example.com",
  "providerMessageId": "guid"
}
```

## 📊 HTTP Status Codes

| Code | Meaning | When |
|------|---------|------|
| 200 | OK | Successful GET/POST request |
| 201 | Created | Resource created successfully |
| 400 | Bad Request | Invalid input data |
| 401 | Unauthorized | Missing or invalid token |
| 403 | Forbidden | No permission to access resource |
| 404 | Not Found | Resource doesn't exist |
| 413 | Payload Too Large | File exceeds 20MB |
| 415 | Unsupported Media Type | Invalid file type |
| 500 | Internal Server Error | Server error |

## 🔑 Authorization Rules

| Role | Can Upload | Can View Own Docs | Can View All Docs | Can Send Email |
|------|-----------|-------------------|-------------------|----------------|
| user | ✅ | ✅ | ❌ | ✅ (own docs) |
| admin | ✅ | ✅ | ✅ | ✅ (all docs) |

## 📝 File Validation

- **Allowed Types:** PDF (.pdf), DOCX (.docx)
- **Max Size:** 20 MB
- **Content-Type:** 
  - `application/pdf`
  - `application/vnd.openxmlformats-officedocument.wordprocessingml.document`

## 🔐 JWT Token

- **Algorithm:** HS256
- **Expiration:** 60 minutes
- **Claims:**
  - `sub`: User ID (Guid)
  - `email`: User email
  - `role`: User roles (user/admin)
  - `exp`: Expiration timestamp

**Usage:**
```
Authorization: Bearer {your-token-here}
```

## 🗄️ Database

- **Server:** (localdb)\mssqllocaldb
- **Database:** DocumentApiDb
- **Tables:** Users, Roles, UserRoles, Documents, EmailLogs

## 📁 File Storage

**Path Structure:**
```
./storage/yyyy/MM/dd/{documentId}/{filename}
```

**Example:**
```
./storage/2025/11/02/abc123-def456.../document.pdf
```

## 📧 Email Configuration

**Default (MailHog):**
- Host: localhost
- Port: 1025
- SSL: Disabled
- Web UI: http://localhost:8025

## 🧪 Quick Test Sequence

1. **Register:**
   ```bash
   POST /auth/register
   ```

2. **Login:**
   ```bash
   POST /auth/login
   # Copy the accessToken
   ```

3. **Upload:**
   ```bash
   POST /documents
   Authorization: Bearer {token}
   # Copy the documentId
   ```

4. **Get Metadata:**
   ```bash
   GET /documents/{documentId}
   Authorization: Bearer {token}
   ```

5. **Download:**
   ```bash
   GET /documents/{documentId}?download=true
   Authorization: Bearer {token}
   ```

6. **Send Email:**
   ```bash
   POST /documents/{documentId}/send
   Authorization: Bearer {token}
   ```

## 🛠️ cURL Examples

### Register
```bash
curl -X POST http://localhost:5072/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password123!","displayName":"Test User"}'
```

### Login
```bash
curl -X POST http://localhost:5072/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password123!"}'
```

### Upload Document
```bash
curl -X POST http://localhost:5072/documents \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@document.pdf"
```

### Get Document
```bash
curl -X GET http://localhost:5072/documents/{id} \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Download Document
```bash
curl -X GET "http://localhost:5072/documents/{id}?download=true" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -o downloaded.pdf
```

### Send Email
```bash
curl -X POST http://localhost:5072/documents/{id}/send \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"to":"recipient@example.com","subject":"Document","message":"Please review"}'
```

## 🎯 Common Errors

### 401 Unauthorized
- **Cause:** Missing or invalid token
- **Fix:** Include valid Bearer token in Authorization header

### 403 Forbidden
- **Cause:** Trying to access another user's document
- **Fix:** Only access your own documents (or use admin account)

### 413 Payload Too Large
- **Cause:** File exceeds 20MB
- **Fix:** Use a smaller file

### 415 Unsupported Media Type
- **Cause:** File is not PDF or DOCX
- **Fix:** Upload only PDF or DOCX files

## 📞 Support

- **Swagger UI:** http://localhost:5072/swagger
- **Testing Guide:** See TESTING-GUIDE.md
- **Implementation Details:** See IMPLEMENTATION-SUMMARY.md

