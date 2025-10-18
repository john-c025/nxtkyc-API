# KYC API Documentation

## Public Endpoints (Client-facing)

### 1. Revoke Access Token

**Endpoint:** `POST /api/v1/public/kyc/tokens/revoke`

**Description:** Allows clients to revoke their own access tokens (self-serve "I didn't request this")

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "token": "string (required)",
  "account_code": "string (required)"
}
```

**Sample Response:**
```json
{
  "success": true,
  "message": "Token revoked",
  "data": {
    "revoked": true
  }
}
```

**Error Response:**
```json
{
  "success": false,
  "message": "Token already invalid or not found",
  "data": {
    "revoked": false
  }
}
```

---

### 2. Token Introspection

**Endpoint:** `GET /api/v1/public/kyc/tokens/introspect`

**Description:** Check token validity and get expiry information

**Query Parameters:**
- `token` (string, required): The access token to introspect
- `account_code` (string, required): The account code associated with the token

**Sample Request:**
```
GET /api/v1/public/kyc/tokens/introspect?token=abc123&account_code=ACC001
```

**Sample Response:**
```json
{
  "success": true,
  "message": "Token introspection",
  "data": {
    "is_valid": true,
    "expires_at": "2024-01-15T10:30:00Z",
    "reason": null
  }
}
```

**Invalid Token Response:**
```json
{
  "success": true,
  "message": "Token invalid",
  "data": {
    "is_valid": false
  }
}
```

---

### 3. Send KYC Link Email

**Endpoint:** `POST /api/v1/public/kyc/tokens/send-email`

**Description:** Send KYC link to client's email (server-initiated)

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "email": "client@example.com",
  "account_code": "ACC001",
  "hours_valid": 24,
  "subject": "Complete your KYC verification",
  "body": "Please click the link to complete your KYC: {link}"
}
```

**Parameters:**
- `email` (string, required): Client's email address
- `account_code` (string, required): Account code
- `hours_valid` (integer, optional): Token validity in hours (default: 24)
- `subject` (string, optional): Email subject line
- `body` (string, optional): Email body template (use {link} placeholder)

**Sample Response:**
```json
{
  "success": true,
  "message": "KYC link sent"
}
```

---

### 4. Resend KYC Link Email

**Endpoint:** `POST /api/v1/public/kyc/tokens/resend`

**Description:** Resend KYC link with cooldown protection

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "email": "client@example.com",
  "account_code": "ACC001",
  "hours_valid": 24,
  "cooldown_minutes": 5,
  "subject": "Complete your KYC verification",
  "body": "Please click the link to complete your KYC: {link}"
}
```

**Parameters:**
- `email` (string, required): Client's email address
- `account_code` (string, required): Account code
- `hours_valid` (integer, optional): Token validity in hours (default: 24)
- `cooldown_minutes` (integer, optional): Cooldown period in minutes (default: 5)
- `subject` (string, optional): Email subject line
- `body` (string, optional): Email body template

**Sample Response:**
```json
{
  "success": true,
  "message": "KYC link resent"
}
```

**Cooldown Response:**
```json
{
  "success": true,
  "message": "Please wait 3 minute(s) before resending"
}
```

---

### 5. Public Timeline

**Endpoint:** `GET /api/v1/public/kyc/requests/{kycRequestId}/timeline`

**Description:** Get public timeline/events for a KYC request

**Path Parameters:**
- `kycRequestId` (string, required): The KYC request ID

**Sample Request:**
```
GET /api/v1/public/kyc/requests/KYC-2024-001/timeline
```

**Sample Response:**
```json
{
  "success": true,
  "message": "Timeline retrieved",
  "data": [
    {
      "action_type": 1,
      "action_timestamp": "2024-01-15T10:30:00Z",
      "old_status": null,
      "new_status": 1
    },
    {
      "action_type": 2,
      "action_timestamp": "2024-01-15T11:00:00Z",
      "old_status": 1,
      "new_status": 2
    }
  ]
}
```

---

### 6. Create Upload Session

**Endpoint:** `POST /api/v1/public/kyc/upload/create-session`

**Description:** Create upload session for file uploads (placeholder for pre-signed URLs)

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "account_code": "ACC001",
  "file_types": ["pdf", "jpg", "png"]
}
```

**Sample Response:**
```json
{
  "success": true,
  "message": "Upload session created",
  "data": {
    "upload_session_id": "550e8400-e29b-41d4-a716-446655440000",
    "max_file_size_mb": 10,
    "allowed_mime": [
      "application/pdf",
      "image/jpeg",
      "image/png",
      "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
      "application/msword"
    ],
    "urls": []
  }
}
```

---

### 7. Complete Upload Session

**Endpoint:** `POST /api/v1/public/kyc/upload/complete-session`

**Description:** Complete upload session (placeholder for verification)

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "upload_session_id": "550e8400-e29b-41d4-a716-446655440000",
  "files": [
    {
      "file_name": "document.pdf",
      "file_size": 1024000,
      "file_type": "pdf"
    }
  ]
}
```

**Sample Response:**
```json
{
  "success": true,
  "message": "Upload session completed",
  "data": {
    "verified": true
  }
}
```

---

## Admin Endpoints (Internal)

### 8. Download Media File

**Endpoint:** `GET /api/v1/kyc/files/{fileId}/download`

**Description:** Secure download of media files

**Path Parameters:**
- `fileId` (long, required): The media file ID

**Headers:**
```
Authorization: Bearer <jwt_token>
```

**Sample Request:**
```
GET /api/v1/kyc/files/123/download
```

**Response:** Binary file content with appropriate Content-Type header

**Error Response:**
```json
{
  "success": false,
  "message": "File not found"
}
```

---

### 9. Request Reupload

**Endpoint:** `POST /api/v1/kyc/files/{fileId}/request-reupload`

**Description:** Request client to reupload a document

**Path Parameters:**
- `fileId` (long, required): The media file ID

**Headers:**
```
Authorization: Bearer <jwt_token>
Content-Type: application/json
```

**Request Body:**
```json
{
  "reason": "Document quality is too low, please upload a clearer version"
}
```

**Sample Response:**
```json
{
  "success": true,
  "message": "Reupload requested"
}
```

---

### 10. Send Decision Email

**Endpoint:** `POST /api/v1/kyc/requests/{kycRequestId}/send-decision-email`

**Description:** Send decision email to client (approve/reject/initial)

**Path Parameters:**
- `kycRequestId` (string, required): The KYC request ID

**Headers:**
```
Authorization: Bearer <jwt_token>
Content-Type: application/json
```

**Request Body:**
```json
{
  "decision": "approve",
  "decision_reason": "All documents verified successfully",
  "subject": "Your KYC request has been approved",
  "body": "Congratulations! Your KYC verification has been completed successfully."
}
```

**Parameters:**
- `decision` (string, optional): Decision type (initial, approve, reject)
- `decision_reason` (string, optional): Reason for the decision
- `subject` (string, optional): Email subject line
- `body` (string, optional): Email body content

**Sample Response:**
```json
{
  "success": true,
  "message": "Decision email sent"
}
```

---

## Error Responses

All endpoints may return the following error responses:

### 400 Bad Request
```json
{
  "success": false,
  "message": "Invalid request parameters"
}
```

### 401 Unauthorized
```json
{
  "success": false,
  "message": "User not authenticated"
}
```

### 404 Not Found
```json
{
  "success": false,
  "message": "Resource not found"
}
```

### 500 Internal Server Error
```json
{
  "success": false,
  "message": "Internal server error"
}
```

---

## Configuration

### Environment Variables
- `PublicKycBaseUrl`: Base URL for KYC client pages (used in email links)
- `Jwt:Key`: JWT signing key for authentication
- `Jwt:Issuer`: JWT issuer
- `Jwt:Audience`: JWT audience

### Email Configuration
- SMTP settings configured in `EmailService`
- Email templates stored in `Templates/` directory
- HTML templates with placeholder support

---

## Rate Limiting

- Resend email endpoint has built-in cooldown (default: 5 minutes)
- Token generation is rate-limited per account
- File uploads have size limits (10MB default)

---

## Security Notes

- All tokens are hashed before storage
- Tokens are single-use after consumption
- File downloads require authentication
- Audit trails are maintained for all actions
- Email sending includes validation and error handling
