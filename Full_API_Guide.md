## KYC API - Full Integration Guide

This guide is intended for frontend integration. It lists all endpoints, parameters, request/response samples, purposes, and suggested usage flows.

### Conventions
- Base URL examples use `/api/v1/...`.
- Auth-only endpoints require header: `Authorization: Bearer <jwt>`.
- API envelope (when applicable): `{ success, message, data }`.

### Multi-Company Support
- Admins can access all companies (universal access)
- Use `companyId` parameter to filter data by specific company
- No company-specific user restrictions
- Superadmins and admins have full access to all affiliate companies

---

## Auth

### Login
- Purpose: Obtain JWT for admin/internal endpoints
- Method/Path: `POST /api/v1/auth/login`
- Body:
```json
{ "username": "admin", "password": "******" }
```
- Response:
```json
{ "Message": "Login successful", "UserId": "USR001", "Token": "<jwt>", "Code": 200 }
```
- Usage: Store `Token` for subsequent admin requests.

---

## Public KYC (client-facing)

### Revoke Token
- Purpose: Client self-serve revoke ("I didn't request this")
- Method/Path: `POST /api/v1/public/kyc/tokens/revoke`
- Body:
```json
{ "token": "<token>", "account_code": "ACC001" }
```
- Response:
```json
{ "success": true, "message": "Token revoked", "data": { "revoked": true } }
```
- Usage: Provide button on client page to revoke.

### Introspect Token
- Purpose: Validate token and see expiry
- Method/Path: `GET /api/v1/public/kyc/tokens/introspect`
- Query: `token`, `account_code`
- Response:
```json
{ "success": true, "message": "Token introspection", "data": { "is_valid": true, "expires_at": "2025-01-01T10:00:00Z" } }
```
- Usage: Client page loads → check token validity before rendering.

### Send KYC Link Email
- Purpose: Client-initiated or admin-initiated KYC email (with email verification and rate limiting)
- Method/Path: `POST /api/v1/public/kyc/tokens/send-email`
- Body:
```json
{ "email": "client@x.com", "account_code": "ACC001", "hours_valid": 24, "subject": "Complete KYC", "body": "Click: {link}" }
```
- Response: `{ "success": true, "message": "KYC verification link sent to your email", "data": { "email_sent": true, "expires_in_hours": 24 } }`
- Rate Limit Response: `{ "success": true, "message": "Rate limit exceeded. Please wait X minute(s)...", "data": { "rate_limited": true, "wait_minutes": X } }`
- Usage: Clients can request their own KYC links. Rate limited to 3 requests per email per hour. Email must match the account's registered email (if stored).
- Rate Limiting: 3 requests per email per hour

### Resend KYC Link Email
- Purpose: Resend with cooldown
- Method/Path: `POST /api/v1/public/kyc/tokens/resend`
- Body (same as send): cooldown honored
- Response: success or cooldown wait message
- Usage: Admin UI "Resend" button; handle cooldown message.
- Cooldown: 5 minutes between resends (configurable)

### Public Timeline
- Purpose: Show client-visible timeline
- Method/Path: `GET /api/v1/public/kyc/requests/{kycRequestId}/timeline`
- Response:
```json
{ "success": true, "message": "Timeline retrieved", "data": [ { "action_type": 1, "action_timestamp": "...", "old_status": null, "new_status": 1 } ] }
```
- Usage: Client status page render.

### Create Upload Session (placeholder)
- Purpose: Prep for uploads (S3/GCS later)
- Method/Path: `POST /api/v1/public/kyc/upload/create-session`
- Response includes `upload_session_id`, constraints.

### Complete Upload Session (placeholder)
- Purpose: Confirm upload
- Method/Path: `POST /api/v1/public/kyc/upload/complete-session`
- Response: `{ verified: true }`

### Submit KYC Request
- Purpose: Create request + upload files
- Method/Path: `POST /api/v1/public/kyc/submit`
- Body: Multipart form
  - Fields: `access_token`, `account_code`, optional: `request_type`, `priority_level`, `request_description`, `level_to_upgrade_to`, `file_description`
  - Files: `files[]`
- Response includes `kyc_request_id` and uploaded files info
- Usage: Client submission form.

### Public Reference
- Purpose: Reference data for client UI
- Methods:
  - `GET /api/v1/public/kyc/privilege-levels/{companyId}`
  - `GET /api/v1/public/kyc/company-by-account?account_code=...`
  - `GET /api/v1/public/kyc/files/categories`
  - `GET /api/v1/public/kyc/check-account?account_code=...`
  - `GET /api/v1/public/kyc/requirements`

---

## Admin KYC

Requires: `Authorization: Bearer <jwt>`

### Generate Access Token
- Purpose: Issue KYC tokens for an account
- Method/Path: `POST /api/v1/kyc/tokens/generate`
- Body:
```json
{ "account_code": "ACC001", "hours_valid": 24 }
```
- Response: `{ "success": true, "message": "Access token generated successfully", "data": { "token": "...", "expires_in_hours": 24, "account_code": "ACC001" } }`

### Requests (CRUD-like)
- Create: `POST /api/v1/kyc/requests`
- List: `GET /api/v1/kyc/requests?companyId=&status=&priorityLevel=&page=&pageSize=`
- Detail: `GET /api/v1/kyc/requests/{kycRequestId}`
- Process: `POST /api/v1/kyc/requests/process`

### Files
- List for request: `GET /api/v1/kyc/requests/{kycRequestId}/files`
- Upload to request: `POST /api/v1/kyc/requests/{kycRequestId}/files`
- Download: `GET /api/v1/kyc/files/{fileId}/download`
- Request reupload: `POST /api/v1/kyc/files/{fileId}/request-reupload`

### Decision Email
- Purpose: Notify client on initial/approve/reject
- Method/Path: `POST /api/v1/kyc/requests/{kycRequestId}/send-decision-email`
- Body:
```json
{ "decision": "approve", "decision_reason": "All good", "subject": "Approved", "body": "Congrats" }
```

### Companies & Clients
- Companies:
  - List: `GET /api/v1/kyc/companies`
  - Get: `GET /api/v1/kyc/companies/{companyId}`
  - Create: `POST /api/v1/kyc/companies`
- Clients:
  - List: `GET /api/v1/kyc/clients`
  - Get by code: `GET /api/v1/kyc/clients/{accountCode}`
  - Create: `POST /api/v1/kyc/clients`
  - Update: `PUT /api/v1/kyc/clients/{accountId}`

### Privileges & Categories
- Privileges: `GET /api/v1/kyc/privileges?companyId=`
- File categories: `GET /api/v1/kyc/files/categories`

### Dashboard & Stats
- Summary: `GET /api/v1/kyc/dashboard/summary?companyId=&fromDate=&toDate=`
- Company statistics: `GET /api/v1/kyc/dashboard/company-statistics`

---

## Requests System (Global)

- Submit: `POST /api/v1/requests/submit-request` (query: `initiatorId`, `requestType`; body: JSON)
- Approve: `POST /api/v1/requests/admin/approve?refno=`
- Reject: `POST /api/v1/requests/admin/reject?refno=`
- List pending: `GET /api/v1/requests/admin/get-pending-requests`
- List all: `GET /api/v1/requests/admin/get-all-requests`

---

## Notifications (Global)

- Send reset password: `POST /api/v1/global/notifications/send-reset-password`
- Send update request: `POST /api/v1/global/notifications/send-update-request-notification`
- Send admin update request: `POST /api/v1/global/notifications/send-admin-update-request`
- Mark read: `POST /api/v1/global/notifications/update-read` (body: `{ userId, notifId }`)
- Load user notifs: `GET /api/v1/global/load-user-notifications?userId=`

---

## User Management (UAM)

- Get user details: `GET /api/v1/uam/management/user/details?userId=`
- Load system users: `GET /api/v1/uam/management/load-system-users?isactive=`
- Register user: `POST /api/v1/uam/management/register-user`
- Load positions: `GET /api/v1/uam/management/load-positions`
- Load branches: `GET /api/v1/uam/management/load-branches`
- Load companies: `GET /api/v1/uam/management/load-companies`
- Activate user: `PUT /api/v1/uam/management/activate-user?employeeid=`
- Deactivate user: `PUT /api/v1/uam/management/deactivate-user?employeeid=`

---

## How To Use (Frontend Flows)

### Flow A: Admin initiates KYC via email
1) Admin logs in → get JWT
2) Generate token: `POST /api/v1/kyc/tokens/generate` OR send directly: `POST /api/v1/public/kyc/tokens/send-email`
3) Optionally resend with `POST /api/v1/public/kyc/tokens/resend` (cooldown enforced)
4) Client clicks link → client page loads → call `GET /api/v1/public/kyc/tokens/introspect`

### Flow B: Client submits KYC
1) Client page receives `token` and `account` query params
2) Validate before render: `GET /api/v1/public/kyc/tokens/introspect`
3) Load requirements/reference endpoints
4) Submit form (multipart) to `POST /api/v1/public/kyc/submit`
5) Show public status with `GET /api/v1/public/kyc/requests/{id}/timeline` and `GET /api/v1/public/kyc/status/{id}`

### Flow C: Admin review workflow
1) List requests: `GET /api/v1/kyc/requests`
2) Open details: `GET /api/v1/kyc/requests/{id}`
3) View files: `GET /api/v1/kyc/requests/{id}/files`, download as needed
4) Request reupload: `POST /api/v1/kyc/files/{fileId}/request-reupload`
5) Approve/Reject: `POST /api/v1/kyc/requests/process`
6) Send decision email: `POST /api/v1/kyc/requests/{id}/send-decision-email`

---

## Notes & Best Practices
- Token security: tokens are hashed at rest; consumption invalidates them.
- Cooldowns: resend endpoint enforces minutes-based cooldown.
- Files: current implementation uses local storage; replace with S3/GCS + presigned URLs for production.
- Audits: public timeline exposes safe audit fields.
- Errors: handle standard 400/401/404/500.


