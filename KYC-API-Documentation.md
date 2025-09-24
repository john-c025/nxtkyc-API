# KYC API Documentation

## Overview
The KYC (Know Your Customer) API provides a comprehensive system for managing client verification requests through a secure token-based workflow. The system supports both internal administrative operations and public client-facing endpoints.

## System Architecture

### Core Components
- **Internal API** (`/api/KYC/*`) - Administrative functions
- **Public API** (`/api/public/kyc/*`) - Client-facing endpoints
- **Token-Based Security** - Secure access tokens for client requests
- **File Upload Support** - Document management for KYC verification
- **Audit Trail** - Complete tracking of all actions and status changes

## ID Generation Patterns

The system uses specific patterns for generating unique identifiers:

- **KYC Request ID**: `KYC123456789012` (KYC + 12 random digits)
- **Account Code**: `BWC1234567890` (company_code + 10 random digits)
- **Account ID**: `987654321098765` (15 random digits)
- **System User Key**: `A7B3K9M2P5` (10 random alphanumeric characters)
- **User ID**: `9182736450` (10 random digits)
- **Token ID**: Auto-incrementing integer from `kyc_access_tokens.autoid`

## API Endpoints

### Internal API Endpoints (`/api/KYC/*`)

#### Company Management
```http
GET    /api/KYC/companies                     # Get all companies
GET    /api/KYC/companies/{companyId}         # Get company by ID
POST   /api/KYC/companies                     # Create new company
```

**Sample Responses:**

**GET /api/KYC/companies**
```json
{
  "success": true,
  "message": "Companies retrieved successfully",
  "data": [
    {
      "company_id": 1,
      "company_name": "BLOOMS WELLNESS",
      "company_code": "BWC",
      "company_type": "networking",
      "is_active": true,
      "created_at": "2025-01-20T10:00:00Z",
      "updated_at": "2025-01-20T10:00:00Z",
      "created_by": "admin",
      "updated_by": "admin"
    }
  ]
}
```

**GET /api/KYC/companies/{companyId}**
```json
{
  "success": true,
  "message": "Company retrieved successfully",
  "data": {
    "company_id": 1,
    "company_name": "BLOOMS WELLNESS",
    "company_code": "BWC",
    "company_type": "networking",
    "is_active": true,
    "created_at": "2025-01-20T10:00:00Z",
    "updated_at": "2025-01-20T10:00:00Z",
    "created_by": "admin",
    "updated_by": "admin"
  }
}
```

**POST /api/KYC/companies**
```json
{
  "success": true,
  "message": "Company created successfully",
  "data": {
    "company_id": 2
  }
}
```

#### Client Account Management
```http
GET    /api/KYC/clients                       # Get client accounts (with filtering)
GET    /api/KYC/clients/{accountCode}         # Get client by account code
POST   /api/KYC/clients                       # Create new client account (full details)
POST   /api/KYC/clients/create               # Create/Update client account (upsert)
PUT    /api/KYC/clients/{accountId}          # Update existing client account
```

**Sample Responses:**

**GET /api/KYC/clients**
```json
{
  "success": true,
  "message": "Client accounts retrieved successfully",
  "data": [
    {
      "autoid": 123,
      "company_id": 1,
      "account_code": "BWC1234567890",
      "account_origin_number": "ACC001",
      "account_id": "987654321098765",
      "fname": "John",
      "mname": "Middle",
      "sname": "Doe",
      "account_status": 1,
      "current_privilege_level": 2,
      "account_metadata": "{\"source\":\"web\",\"campaign\":\"kyc2025\"}",
      "is_active": true,
      "created_at": "2025-01-20T10:00:00Z",
      "updated_at": "2025-01-23T14:30:00Z",
      "created_by": "admin",
      "updated_by": "system",
      "company_name": "BLOOMS WELLNESS",
      "full_name": "John Middle Doe"
    }
  ]
}
```

**GET /api/KYC/clients/{accountCode}**
```json
{
  "success": true,
  "message": "Client account retrieved successfully",
  "data": {
    "autoid": 123,
    "company_id": 1,
    "account_code": "BWC1234567890",
    "account_origin_number": "ACC001",
    "account_id": "987654321098765",
    "fname": "John",
    "mname": "Middle",
    "sname": "Doe",
    "account_status": 1,
    "current_privilege_level": 2,
    "account_metadata": "{\"source\":\"web\",\"campaign\":\"kyc2025\"}",
    "is_active": true,
    "created_at": "2025-01-20T10:00:00Z",
    "updated_at": "2025-01-23T14:30:00Z",
    "created_by": "admin",
    "updated_by": "system",
    "company_name": "BLOOMS WELLNESS",
    "full_name": "John Middle Doe"
  }
}
```

**POST /api/KYC/clients**
```json
{
  "success": true,
  "message": "Client account created successfully",
  "data": {
    "client_id": 124
  }
}
```

**POST /api/KYC/clients/create**
```json
{
  "success": true,
  "message": "Client account created successfully",
  "data": {
    "client_id": 125,
    "account_code": "BWC9876543210",
    "account_id": "123456789012345",
    "is_new_account": true
  }
}
```

**PUT /api/KYC/clients/{accountId}**
```json
{
  "success": true,
  "message": "Client account updated successfully"
}
```

#### Access Token Management
```http
POST   /api/KYC/tokens/generate              # Generate access token for KYC request
POST   /api/KYC/tokens/validate              # Validate access token for client pages
```

**Sample Responses:**

**POST /api/KYC/tokens/generate**
```json
{
  "success": true,
  "message": "Access token generated successfully",
  "data": {
    "token": "dGVzdF90b2tlbl9leGFtcGxlX2Jhc2U2NF9lbmNvZGVk",
    "expires_in_hours": 24,
    "account_code": "BWC1234567890"
  }
}
```

**POST /api/KYC/tokens/validate**
```json
{
  "success": true,
  "message": "Token is valid",
  "data": {
    "is_valid": true,
    "account_code": "BWC1234567890",
    "current_privilege_level": 1,
    "company_name": "BLOOMS WELLNESS",
    "company_code": "BWC",
    "expires_at": "2025-01-24T10:30:00Z"
  }
}
```

#### KYC Request Management
```http
GET    /api/KYC/requests                     # Get KYC requests (with filtering)
GET    /api/KYC/requests/{kycRequestId}      # Get detailed KYC request
POST   /api/KYC/requests                     # Create KYC request (internal)
POST   /api/KYC/requests/process            # Process KYC request (approve/reject/archive/escalate)
```

**Sample Responses:**

**GET /api/KYC/requests**
```json
{
  "success": true,
  "message": "KYC requests retrieved successfully",
  "data": [
    {
      "autoid": 456,
      "kyc_request_id": "KYC123456789012",
      "company_id": 1,
      "client_account_id": 123,
      "token_id": 789,
      "request_type": "Level Upgrade",
      "request_status": 1,
      "priority_level": 2,
      "request_description": "Requesting privilege level upgrade",
      "current_level": 0,
      "level_to_upgrade_to": 2,
      "has_files": true,
      "is_one_time_only": true,
      "submitted_at": "2025-01-23T10:30:00Z",
      "completed_at": null,
      "archived_at": null,
      "created_at": "2025-01-23T10:30:00Z",
      "updated_at": "2025-01-23T10:30:00Z",
      "created_by": "SYSTEM_PUBLIC",
      "updated_by": "SYSTEM_PUBLIC",
      "client_full_name": "John Middle Doe",
      "company_name": "BLOOMS WELLNESS",
      "request_status_name": "Pending",
      "priority_level_name": "Medium"
    }
  ]
}
```

**GET /api/KYC/requests/{kycRequestId}**
```json
{
  "success": true,
  "message": "KYC request retrieved successfully",
  "data": {
    "autoid": 456,
    "kyc_request_id": "KYC123456789012",
    "company_id": 1,
    "client_account_id": 123,
    "token_id": 789,
    "request_type": "Level Upgrade",
    "request_status": 1,
    "priority_level": 2,
    "request_description": "Requesting privilege level upgrade",
    "current_level": 0,
    "level_to_upgrade_to": 2,
    "has_files": true,
    "is_one_time_only": true,
    "submitted_at": "2025-01-23T10:30:00Z",
    "completed_at": null,
    "archived_at": null,
    "created_at": "2025-01-23T10:30:00Z",
    "updated_at": "2025-01-23T10:30:00Z",
    "created_by": "SYSTEM_PUBLIC",
    "updated_by": "SYSTEM_PUBLIC",
    "client_full_name": "John Middle Doe",
    "company_name": "BLOOMS WELLNESS",
    "request_status_name": "Pending",
    "priority_level_name": "Medium",
    "attached_files": [
      {
        "autoid": 1,
        "kyc_request_id": "KYC123456789012",
        "file_name": "KYC123456789012_uuid.pdf",
        "file_original_name": "passport.pdf",
        "file_type": 1,
        "file_extension": ".pdf",
        "file_size": 2048576,
        "file_path": "/Uploads/KYC/Public/KYC123456789012_uuid.pdf",
        "file_url": "/uploads/kyc/public/KYC123456789012_uuid.pdf",
        "mime_type": "application/pdf",
        "file_category": 1,
        "file_description": "Government issued passport",
        "is_verified": false,
        "uploaded_at": "2025-01-23T10:30:00Z",
        "uploaded_by": "CLIENT_PUBLIC",
        "verified_at": null,
        "verified_by": null
      }
    ],
    "approval_actions": [],
    "audit_trail": [
      {
        "autoid": 1,
        "kyc_request_id": "KYC123456789012",
        "action_type": 1,
        "action_by": "SYSTEM_PUBLIC",
        "action_timestamp": "2025-01-23T10:30:00Z",
        "old_status": null,
        "new_status": 1,
        "action_details": "KYC request created",
        "action_type_name": "Created"
      }
    ]
  }
}
```

**POST /api/KYC/requests**
```json
{
  "success": true,
  "message": "KYC request created successfully",
  "data": {
    "kyc_request_id": "KYC987654321098"
  }
}
```

**POST /api/KYC/requests/process**
```json
{
  "success": true,
  "message": "KYC request approved successfully"
}
```

#### File Management
```http
GET    /api/KYC/requests/{kycRequestId}/files    # Get media files for KYC request
POST   /api/KYC/requests/{kycRequestId}/files    # Upload files for KYC request
```

**Sample Responses:**

**GET /api/KYC/requests/{kycRequestId}/files**
```json
{
  "success": true,
  "message": "KYC media files retrieved successfully",
  "data": [
    {
      "autoid": 1,
      "kyc_request_id": "KYC123456789012",
      "file_name": "KYC123456789012_uuid.pdf",
      "file_original_name": "passport.pdf",
      "file_type": 1,
      "file_extension": ".pdf",
      "file_size": 2048576,
      "file_path": "/Uploads/KYC/Public/KYC123456789012_uuid.pdf",
      "file_url": "/uploads/kyc/public/KYC123456789012_uuid.pdf",
      "mime_type": "application/pdf",
      "file_category": 1,
      "file_description": "Government issued passport",
      "is_verified": false,
      "uploaded_at": "2025-01-23T10:30:00Z",
      "uploaded_by": "CLIENT_PUBLIC",
      "verified_at": null,
      "verified_by": null
    }
  ]
}
```

**POST /api/KYC/requests/{kycRequestId}/files**
```json
{
  "success": true,
  "message": "Files uploaded successfully",
  "data": [
    {
      "file_id": 1,
      "file_name": "KYC123456789012_uuid.pdf",
      "original_name": "passport.pdf"
    }
  ]
}
```

#### Dashboard & Analytics
```http
GET    /api/KYC/dashboard/summary               # Get dashboard summary statistics
GET    /api/KYC/dashboard/company-statistics    # Get company statistics
```

**Sample Responses:**

**GET /api/KYC/dashboard/summary**
```json
{
  "success": true,
  "message": "Dashboard summary retrieved successfully",
  "data": {
    "total_requests": 150,
    "pending_requests": 25,
    "in_review_requests": 15,
    "approved_requests": 85,
    "rejected_requests": 20,
    "archived_requests": 5,
    "high_priority_requests": 10,
    "urgent_priority_requests": 3,
    "approval_rate": 80.95,
    "rejection_rate": 19.05,
    "average_processing_hours": 48.5
  }
}
```

**GET /api/KYC/dashboard/company-statistics**
```json
{
  "success": true,
  "message": "Company statistics retrieved successfully",
  "data": [
    {
      "company_id": 1,
      "company_name": "BLOOMS WELLNESS",
      "total_clients": 50,
      "active_clients": 45,
      "total_requests": 120,
      "pending_requests": 20,
      "approved_requests": 85,
      "rejected_requests": 15,
      "approval_rate": 85.00,
      "average_processing_hours": 36.2
    }
  ]
}
```

#### Privilege Management
```http
GET    /api/KYC/privileges                     # Get KYC privilege levels for dropdowns
```

**Sample Response:**

**GET /api/KYC/privileges**
```json
{
  "success": true,
  "message": "KYC privileges retrieved successfully",
  "data": [
    {
      "autoid": 1,
      "company_id": 1,
      "privilege_level": 0,
      "privilege_name": "Basic",
      "privilege_description": "Basic account access with limited features",
      "privileges_json": "{\"features\":[\"view_profile\",\"basic_transactions\"]}",
      "is_active": true,
      "created_at": "2025-01-20T10:00:00Z",
      "updated_at": "2025-01-20T10:00:00Z",
      "created_by": "admin",
      "updated_by": "admin",
      "company_name": "BLOOMS WELLNESS"
    },
    {
      "autoid": 2,
      "company_id": 1,
      "privilege_level": 1,
      "privilege_name": "Bronze",
      "privilege_description": "Enhanced account access with additional features",
      "privileges_json": "{\"features\":[\"view_profile\",\"basic_transactions\",\"advanced_reports\"]}",
      "is_active": true,
      "created_at": "2025-01-20T10:00:00Z",
      "updated_at": "2025-01-20T10:00:00Z",
      "created_by": "admin",
      "updated_by": "admin",
      "company_name": "BLOOMS WELLNESS"
    }
  ]
}
```

#### File Category Management
```http
GET    /api/KYC/files/categories               # Get file categories for file management
```

**Sample Response:**

**GET /api/KYC/files/categories**
```json
{
  "success": true,
  "message": "File categories retrieved successfully",
  "data": [
    {
      "id": 1,
      "name": "ID Documents",
      "description": "Government-issued identification documents"
    },
    {
      "id": 2,
      "name": "Address Proof",
      "description": "Utility bills, bank statements, lease agreements"
    },
    {
      "id": 3,
      "name": "Financial Documents",
      "description": "Income verification, bank statements, tax returns"
    },
    {
      "id": 4,
      "name": "Authorization Documents",
      "description": "Signatures, authorization forms, power of attorney"
    },
    {
      "id": 99,
      "name": "General/Other",
      "description": "Miscellaneous documents"
    }
  ]
}
```

#### Reference Data
```http
GET    /api/KYC/reference/statuses             # Get status reference data
```

**Sample Response:**

**GET /api/KYC/reference/statuses**
```json
{
  "success": true,
  "message": "Status reference data retrieved successfully",
  "data": {
    "request_statuses": [
      { "value": 1, "name": "Pending" },
      { "value": 2, "name": "In Review" },
      { "value": 3, "name": "Approved" },
      { "value": 4, "name": "Rejected" },
      { "value": 5, "name": "Archived" }
    ],
    "priority_levels": [
      { "value": 1, "name": "Low" },
      { "value": 2, "name": "Medium" },
      { "value": 3, "name": "High" },
      { "value": 4, "name": "Urgent" }
    ],
    "action_types": [
      { "value": 1, "name": "Approve" },
      { "value": 2, "name": "Reject" },
      { "value": 3, "name": "Archive" },
      { "value": 4, "name": "Escalate" }
    ]
  }
}
```

### Public API Endpoints (`/api/public/kyc/*`)

#### Client KYC Submission
```http
POST   /api/public/kyc/submit                  # Submit KYC request with access token
GET    /api/public/kyc/status/{kycRequestId}   # Check KYC request status
```

**Sample Responses:**

**POST /api/public/kyc/submit**
```json
{
  "success": true,
  "message": "KYC request submitted successfully",
  "data": {
    "kyc_request_id": "KYC123456789012",
    "status": "Submitted",
    "uploaded_files_count": 3,
    "uploaded_files": [
      {
        "file_id": 1,
        "file_name": "KYC123456789012_uuid.pdf",
        "original_name": "passport.pdf"
      },
      {
        "file_id": 2,
        "file_name": "KYC123456789012_uuid2.jpg",
        "original_name": "utility_bill.jpg"
      }
    ]
  }
}
```

**GET /api/public/kyc/status/{kycRequestId}**
```json
{
  "success": true,
  "message": "KYC request status retrieved successfully",
  "data": {
    "kyc_request_id": "KYC123456789012",
    "request_type": "Level Upgrade",
    "request_status": 1,
    "request_status_name": "Pending",
    "priority_level": 2,
    "priority_level_name": "Medium",
    "submitted_at": "2025-01-23T10:30:00Z",
    "completed_at": null,
    "has_files": true,
    "current_level": 0,
    "level_to_upgrade_to": 2,
    "latest_update": "2025-01-23T10:30:00Z",
    "estimated_processing_time": "3-5 business days"
  }
}
```

#### Information Endpoints
```http
GET    /api/public/kyc/privilege-levels/{companyId}  # Get available privilege levels
GET    /api/public/kyc/requirements                  # Get submission requirements
```

**Sample Responses:**

**GET /api/public/kyc/privilege-levels/{companyId}**
```json
{
  "success": true,
  "message": "Available privilege levels retrieved successfully",
  "data": [
    {
      "level": 0,
      "name": "Basic",
      "description": "Basic account access"
    },
    {
      "level": 1,
      "name": "Bronze",
      "description": "Enhanced account access"
    },
    {
      "level": 2,
      "name": "Silver",
      "description": "Premium account access"
    },
    {
      "level": 3,
      "name": "Gold",
      "description": "VIP account access"
    },
    {
      "level": 4,
      "name": "Platinum",
      "description": "Premium VIP account access"
    }
  ]
}
```

**GET /api/public/kyc/requirements**
```json
{
  "success": true,
  "message": "Submission requirements retrieved successfully",
  "data": {
    "file_requirements": {
      "max_file_size_mb": 10,
      "allowed_extensions": [".pdf", ".jpg", ".jpeg", ".png", ".docx", ".doc"],
      "max_files_per_request": 5,
      "recommended_files": [
        "Valid government-issued ID",
        "Proof of address",
        "Income verification documents",
        "Bank statements (if applicable)"
      ]
    },
    "request_types": [
      "Level Upgrade",
      "Account Verification",
      "Document Update",
      "Special Access Request"
    ],
    "processing_times": {
      "low_priority": "5-7 business days",
      "medium_priority": "3-5 business days",
      "high_priority": "1-3 business days",
      "urgent_priority": "Within 24 hours"
    }
  }
}
```

## Complete KYC Workflow

### Phase 1: Setup & Preparation

#### 1.1 Company Setup
```http
POST /api/KYC/companies
{
  "company_name": "BLOOMS WELLNESS",
  "company_code": "BWC",
  "company_type": "networking"
}
```

#### 1.2 System User Setup
- Create system users in `sys_users` table
- Set up user credentials in `sys_user_credentials`
- Assign company access permissions in `sys_user_company_access`

### Phase 2: Client Account Creation/Update

#### 2.1 Upsert Client Account (Recommended)
```http
POST /api/KYC/clients/create
{
  "account_origin_number": "ACC001",
  "company_id": 1,
  "current_privilege_level": 0
}
```

**Upsert Logic:**
- **If account exists**: Only updates `current_privilege_level` if different
- **If new account**: Creates account with generated `account_code` and `account_id`

**Response:**
```json
{
  "success": true,
  "message": "Client account created successfully",
  "data": {
    "client_id": 123,
    "account_code": "BWC1234567890",
    "account_id": "987654321098765",
    "is_new_account": true
  }
}
```

### Phase 3: Token Generation & Link Creation

#### 3.1 Generate Access Token
```http
POST /api/KYC/tokens/generate
{
  "account_code": "BWC1234567890",
  "hours_valid": 24
}
```

**Response:**
```json
{
  "success": true,
  "message": "Access token generated successfully",
  "data": {
    "token": "base64-encoded-secure-token",
    "expires_in_hours": 24,
    "account_code": "BWC1234567890"
  }
}
```

#### 3.2 Create KYC Link
The system generates a secure link similar to password reset flows:

```
https://your-frontend.com/kyc?token={access_token}&account={account_code}
```

**Link Flow:**
1. Client clicks the link
2. Frontend validates token and account code
3. Frontend calls the upsert endpoint to update client privilege level
4. Frontend displays KYC form with captcha protection

### Phase 4: Client KYC Submission

#### 4.1 Client Submits KYC Request
```http
POST /api/public/kyc/submit
Content-Type: multipart/form-data

{
  "access_token": "base64-encoded-secure-token",
  "account_code": "BWC1234567890",
  "request_type": "Level Upgrade",
  "priority_level": 2,
  "request_description": "Requesting privilege level upgrade",
  "level_to_upgrade_to": 2,
  "files": [file1, file2, file3],
  "file_description": "ID documents and proof of address"
}
```

**Submission Process:**
1. **Token Validation**: Validates and consumes the access token
2. **Account Lookup**: Retrieves client account details
3. **KYC Request Creation**: Creates new KYC request with generated ID
4. **File Processing**: Uploads and processes attached files
5. **Audit Logging**: Records the submission in audit trail

**Response:**
```json
{
  "success": true,
  "message": "KYC request submitted successfully",
  "data": {
    "kyc_request_id": "KYC123456789012",
    "status": "Submitted",
    "uploaded_files_count": 3,
    "uploaded_files": [
      {
        "file_id": 1,
        "file_name": "KYC123456789012_uuid.pdf",
        "original_name": "passport.pdf"
      }
    ]
  }
}
```

### Phase 5: Status Tracking

#### 5.1 Client Checks Status
```http
GET /api/public/kyc/status/KYC123456789012
```

**Response:**
```json
{
  "success": true,
  "message": "KYC request status retrieved successfully",
  "data": {
    "kyc_request_id": "KYC123456789012",
    "request_type": "Level Upgrade",
    "request_status": 1,
    "request_status_name": "Pending",
    "priority_level": 2,
    "priority_level_name": "Medium",
    "submitted_at": "2025-01-23T10:30:00Z",
    "has_files": true,
    "current_level": 0,
    "level_to_upgrade_to": 2,
    "estimated_processing_time": "3-5 business days"
  }
}
```

### Phase 6: Internal Review & Processing

#### 6.1 Admin Reviews Request
```http
GET /api/KYC/requests/KYC123456789012
```

**Detailed Response:**
```json
{
  "success": true,
  "data": {
    "kyc_request_id": "KYC123456789012",
    "client_full_name": "John Middle Doe",
    "company_name": "BLOOMS WELLNESS",
    "request_status": 1,
    "attached_files": [
      {
        "file_id": 1,
        "file_name": "KYC123456789012_uuid.pdf",
        "file_original_name": "passport.pdf",
        "file_size": 2048576,
        "uploaded_at": "2025-01-23T10:30:00Z"
      }
    ],
    "audit_trail": [
      {
        "action_type": 1,
        "action_type_name": "Created",
        "action_by": "SYSTEM_PUBLIC",
        "action_timestamp": "2025-01-23T10:30:00Z"
      }
    ]
  }
}
```

#### 6.2 Process KYC Request
```http
POST /api/KYC/requests/process
{
  "kyc_request_id": "KYC123456789012",
  "action_type": 1,
  "remarks": "All documents verified and approved",
  "approver_user_id": "1234567890"
}
```

**Action Types:**
- `1` = Approve
- `2` = Reject  
- `3` = Archive
- `4` = Escalate

**Processing Effects:**
- **Approve**: Updates client privilege level, sets completion timestamp
- **Reject**: Sets rejection status, allows for resubmission
- **Archive**: Moves to archived state (final)
- **Escalate**: Changes status to "In Review" for higher-level review

## File Upload Specifications

### Supported File Types
- **Documents**: `.pdf`, `.docx`, `.doc`
- **Images**: `.jpg`, `.jpeg`, `.png`
- **Spreadsheets**: `.xlsx`, `.xls`

### File Limits
- **Maximum Size**: 10MB per file
- **Maximum Files**: 5 files per request
- **Storage Path**: `/Uploads/KYC/` or `/Uploads/KYC/Public/`

### File Categories
1. **ID Documents** - Government-issued identification
2. **Address Proof** - Utility bills, bank statements
3. **Financial Documents** - Income verification, bank statements
4. **Authorization Documents** - Signatures, authorization forms
5. **General/Other** - Miscellaneous documents

## Status Codes & Transitions

### Request Status Codes
- `1` = Pending
- `2` = In Review  
- `3` = Approved
- `4` = Rejected
- `5` = Archived

### Valid Status Transitions
- **Pending (1)** → In Review (2), Rejected (4), Archived (5)
- **In Review (2)** → Approved (3), Rejected (4), Archived (5)
- **Approved (3)** → Archived (5)
- **Rejected (4)** → In Review (2), Archived (5)
- **Archived (5)** → No transitions (final state)

### Priority Levels
- `1` = Low (5-7 business days)
- `2` = Medium (3-5 business days)
- `3` = High (1-3 business days)
- `4` = Urgent (Within 24 hours)

## Security Features

### Token-Based Access
- **Secure Generation**: 32-byte random tokens with Base64 encoding
- **Hash Storage**: SHA256 hashed tokens stored in database
- **Expiration**: Configurable expiration time (default 24 hours)
- **Single Use**: Tokens are consumed upon successful submission

### Audit Trail
- **Complete Tracking**: Every action logged with timestamp and user
- **Status Changes**: Old and new status tracking
- **File Operations**: Upload and verification tracking
- **User Actions**: All approver actions with remarks

### Data Validation
- **Input Validation**: Required fields and data type validation
- **File Validation**: Extension and size validation
- **Business Rules**: Privilege level upgrade validation

## Error Handling

### Common Error Responses
```json
{
  "success": false,
  "message": "Error description"
}
```

### Error Scenarios
- **Invalid Token**: Token expired, used, or invalid
- **Invalid Account**: Account code not found or inactive
- **File Errors**: Invalid file type, size exceeded, upload failed
- **Business Rule Violations**: Invalid status transitions, privilege level rules
- **Authentication**: User not authenticated for internal endpoints

## Integration Examples

### Frontend Integration Flow

#### 1. Client Receives Link
```javascript
// URL: https://frontend.com/kyc?token=abc123&account=BWC1234567890
const urlParams = new URLSearchParams(window.location.search);
const token = urlParams.get('token');
const accountCode = urlParams.get('account');
```

#### 2. Update Client Privilege Level
```javascript
const response = await fetch('/api/KYC/clients/create', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    account_origin_number: 'ACC001',
    company_id: 1,
    current_privilege_level: 1
  })
});
```

#### 3. Submit KYC Request
```javascript
const formData = new FormData();
formData.append('access_token', token);
formData.append('account_code', accountCode);
formData.append('request_type', 'Level Upgrade');
formData.append('level_to_upgrade_to', '2');
formData.append('files', fileInput.files[0]);

const response = await fetch('/api/public/kyc/submit', {
  method: 'POST',
  body: formData
});
```

### Backend Integration

#### 1. Generate Token for Client
```csharp
var tokenDto = new GenerateAccessTokenDto
{
    account_code = "BWC1234567890",
    hours_valid = 24
};

var token = await _kycRepository.GenerateAccessTokenAsync(tokenDto);
var link = $"https://frontend.com/kyc?token={token}&account={tokenDto.account_code}";

// Send link via email/SMS to client
```

#### 2. Process Approval
```csharp
var processDto = new ProcessKYCRequestDto
{
    kyc_request_id = "KYC123456789012",
    action_type = 1, // Approve
    remarks = "Documents verified successfully",
    approver_user_id = currentUserId
};

await _kycRepository.ProcessKYCRequestAsync(processDto);
```

## Database Schema Summary

### Key Tables
- `client_companies` - Company master data
- `client_accounts` - Client account information
- `kyc_requests` - KYC request records
- `kyc_access_tokens` - Secure access tokens
- `kyc_media_files` - Uploaded documents
- `kyc_approval_actions` - Approval/rejection actions
- `kyc_audit_trail` - Complete audit log
- `sys_users` - System user accounts
- `sys_user_credentials` - User authentication
- `sys_user_company_access` - User permissions

This documentation provides a complete guide for implementing and using the KYC API system with proper security, audit trails, and user experience considerations.
