# Document API - Testing Guide

## Application Status
✅ **Application is running on: http://localhost:5072**
✅ **Swagger UI available at: http://localhost:5072/swagger**

## Database Setup
- Database: `DocumentApiDb` (SQL Server LocalDB)
- Roles seeded: `user` and `admin`
- Ready for testing

## Testing Workflow

### 1. Register a User (POST /auth/register)

**Endpoint:** `POST http://localhost:5072/auth/register`

**Request Body:**
```json
{
  "email": "test@example.com",
  "password": "Password123!",
  "displayName": "Test User"
}
```

**Expected Response (201 Created):**
```json
{
  "userId": "guid-here",
  "email": "test@example.com",
  "displayName": "Test User"
}
```

### 2. Login (POST /auth/login)

**Endpoint:** `POST http://localhost:5072/auth/login`

**Request Body:**
```json
{
  "email": "test@example.com",
  "password": "Password123!"
}
```

**Expected Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

**Important:** Copy the `accessToken` value for use in subsequent requests.

### 3. Upload a Document (POST /documents)

**Endpoint:** `POST http://localhost:5072/documents`

**Headers:**
```
Authorization: Bearer {your-access-token}
Content-Type: multipart/form-data
```

**Form Data:**
- `file`: Select a PDF or DOCX file (max 20MB)

**Expected Response (201 Created):**
```json
{
  "documentId": "guid-here",
  "filename": "example.pdf",
  "contentType": "application/pdf",
  "sizeBytes": 12345,
  "uploadedAt": "2025-11-02T18:35:00Z",
  "ownerUserId": "your-user-id"
}
```

### 4. Get Document Metadata (GET /documents/{id})

**Endpoint:** `GET http://localhost:5072/documents/{documentId}`

**Headers:**
```
Authorization: Bearer {your-access-token}
```

**Expected Response (200 OK):**
```json
{
  "documentId": "guid-here",
  "filename": "example.pdf",
  "contentType": "application/pdf",
  "sizeBytes": 12345,
  "uploadedAt": "2025-11-02T18:35:00Z",
  "ownerUserId": "your-user-id"
}
```

### 5. Download Document (GET /documents/{id}?download=true)

**Endpoint:** `GET http://localhost:5072/documents/{documentId}?download=true`

**Headers:**
```
Authorization: Bearer {your-access-token}
```

**Expected Response:** File download with appropriate content-type header

### 6. Send Document via Email (POST /documents/{id}/send)

**Endpoint:** `POST http://localhost:5072/documents/{documentId}/send`

**Headers:**
```
Authorization: Bearer {your-access-token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "to": "recipient@example.com",
  "subject": "Document for Review",
  "message": "Please review the attached document."
}
```

**Expected Response (200 OK):**
```json
{
  "status": "sent",
  "recipient": "recipient@example.com",
  "providerMessageId": "guid-here"
}
```

**Note:** For email testing, you can use MailHog (localhost:1025) or configure a real SMTP server in appsettings.json.

## Authorization Testing

### Test 1: Access Without Token
Try accessing any protected endpoint without the `Authorization` header.

**Expected:** 401 Unauthorized

### Test 2: Access Another User's Document
1. Register and login as User A
2. Upload a document as User A
3. Register and login as User B
4. Try to access User A's document using User B's token

**Expected:** 403 Forbidden

### Test 3: Admin Access
To test admin access, you need to manually add the admin role to a user:

**SQL Query:**
```sql
-- Get the user ID
SELECT Id, Email FROM Users WHERE Email = 'test@example.com';

-- Get the admin role ID
SELECT Id, Name FROM Roles WHERE Name = 'admin';

-- Add admin role to user
INSERT INTO UserRoles (UserId, RoleId)
VALUES ('user-guid-here', admin-role-id-here);
```

After adding admin role:
1. Login again to get a new token with admin role
2. Try accessing documents from other users

**Expected:** Admin can access any document (200 OK)

## File Validation Testing

### Test Invalid File Type
Upload a file that is not PDF or DOCX (e.g., .txt, .jpg)

**Expected:** 415 Unsupported Media Type

### Test File Too Large
Upload a file larger than 20MB

**Expected:** 413 Payload Too Large

## Email Configuration

### Using MailHog (Recommended for Testing)
1. Download and run MailHog: https://github.com/mailhog/MailHog
2. MailHog will run on localhost:1025 (SMTP) and localhost:8025 (Web UI)
3. Current appsettings.json is already configured for MailHog
4. View sent emails at: http://localhost:8025

### Using Real SMTP Server
Update `appsettings.json`:
```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "Username": "your-email@gmail.com",
  "Password": "your-app-password",
  "FromEmail": "your-email@gmail.com",
  "FromName": "Document API"
}
```

## Swagger UI Testing

Navigate to: http://localhost:5072/swagger

1. Click "Authorize" button at the top
2. Enter: `Bearer {your-access-token}`
3. Click "Authorize" and "Close"
4. Now you can test all endpoints directly from Swagger UI

## Database Verification

You can verify data in the database using SQL Server Management Studio or Visual Studio:

**Connection String:**
```
Server=(localdb)\mssqllocaldb;Database=DocumentApiDb;Trusted_Connection=true;
```

**Useful Queries:**
```sql
-- View all users
SELECT * FROM Users;

-- View all roles
SELECT * FROM Roles;

-- View user roles
SELECT u.Email, r.Name as Role
FROM Users u
JOIN UserRoles ur ON u.Id = ur.UserId
JOIN Roles r ON ur.RoleId = r.Id;

-- View all documents
SELECT * FROM Documents;

-- View email logs
SELECT * FROM EmailLogs;
```

## File Storage Verification

Uploaded files are stored in: `./storage/yyyy/MM/dd/{documentId}/{filename}`

Example: `./storage/2025/11/02/abc123.../example.pdf`

## Troubleshooting

### Issue: 401 Unauthorized
- Ensure you're including the Authorization header
- Check that the token hasn't expired (60 minutes)
- Verify the token format: `Bearer {token}`

### Issue: 403 Forbidden
- User is trying to access another user's document
- Only admins can access all documents

### Issue: Email not sending
- Check SMTP configuration in appsettings.json
- If using MailHog, ensure it's running
- Check EmailLogs table for error messages

### Issue: File upload fails
- Check file size (max 20MB)
- Verify file type (PDF or DOCX only)
- Ensure proper Content-Type header

## Success Criteria

✅ All endpoints return expected status codes
✅ JWT authentication works correctly
✅ Users can only access their own documents
✅ Admins can access all documents
✅ File upload and download work correctly
✅ Email sending works (check MailHog or email inbox)
✅ Database records are created correctly
✅ Files are stored in the correct directory structure

