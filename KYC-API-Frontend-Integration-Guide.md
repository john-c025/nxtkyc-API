# KYC API Frontend Integration Guide

## 🚀 **Recent Updates & Changes**

### **1. Token Validation Endpoint Moved**
- **OLD URL**: `POST /api/v1/KYC/tokens/validate` (had auth issues)
- **NEW URL**: `POST /api/v1/public/kyc/tokens/validate` (no auth required)

### **2. Enhanced Privilege Structure**
Privileges now return detailed JSON with:
- `services`: Array of available services
- `limits`: Object with various limits  
- `requirements`: Array of document requirements with quantities

---

## 📋 **API Endpoints Reference**

### **🔐 Authentication Required (Internal APIs)**
All internal APIs require JWT token in Authorization header:
```
Authorization: Bearer <your-jwt-token>
```

### **🌐 Public APIs (No Authentication)**
These endpoints are accessible without authentication for external clients:

- `POST /api/v1/public/kyc/tokens/validate`
- `GET /api/v1/public/kyc/privilege-levels/{companyId}`
- `POST /api/v1/public/kyc/submit`
- `GET /api/v1/public/kyc/status/{kycRequestId}`
- `GET /api/v1/public/kyc/requirements`
- `GET /api/v1/public/kyc/files/categories`
- `GET /api/v1/public/kyc/company-by-account` ⭐ **NEW**

---

## 🔑 **Authentication Flow**

### **1. Login (Internal Users)**
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "username": "your_username",
  "password": "your_password"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expires_at": "2025-01-20T10:00:00Z",
    "user": {
      "user_id": "1234567890",
      "fname": "John",
      "sname": "Doe",
      "email": "john@example.com"
    }
  }
}
```

### **2. Token Validation (Public)**
```http
POST /api/v1/public/kyc/tokens/validate
Content-Type: application/json

{
  "token": "mzNg99wzzixt2atLPmsyFpOH7SfkQ4qd",
  "account_code": "BWC9119454067"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Token is valid",
  "data": {
    "is_valid": true,
    "account_code": "BWC9119454067",
    "current_privilege_level": 0,
    "company_name": "BLOOMS WELLNESS",
    "company_code": "BWC",
    "expires_at": "2025-01-20T10:00:00Z"
  }
}
```

---

## 🏢 **Company & Client Management**

### **1. Get Companies (Internal)**
```http
GET /api/v1/KYC/companies
Authorization: Bearer <token>
```

**Response:**
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
      "created_at": "2025-01-19T10:00:00Z"
    }
  ]
}
```

### **2. Create/Update Client Account (Public)**
```http
POST /api/v1/KYC/clients/create
Content-Type: application/json

{
  "account_origin_number": "12345",
  "company_id": 1,
  "current_privilege_level": 0
}
```

**Response:**
```json
{
  "success": true,
  "message": "Client account created successfully",
  "data": {
    "client_id": 123,
    "account_code": "BWC9119454067",
    "account_id": "987654321098765",
    "is_new_account": true
  }
}
```

### **3. Get Company Information by Account Code (Public)**
```http
GET /api/v1/public/kyc/company-by-account?account_code=BWC9119454067
```

**Response:**
```json
{
  "success": true,
  "message": "Company information retrieved successfully",
  "data": {
    "company_id": 1,
    "company_name": "BLOOMS WELLNESS",
    "company_code": "BWC",
    "company_type": "networking",
    "is_active": true,
    "created_at": "2025-01-19T10:00:00Z",
    "account_info": {
      "account_code": "BWC9119454067",
      "current_privilege_level": 0,
      "account_status": 1
    }
  }
}
```

**Error Response (Account Not Found):**
```json
{
  "success": false,
  "message": "Account code not found",
  "data": null
}
```

---

## 🎯 **Privilege Management**

### **1. Get Privileges (Internal)**
```http
GET /api/v1/KYC/privileges?companyId=1
Authorization: Bearer <token>
```

### **2. Get Privilege Levels (Public)**
```http
GET /api/v1/public/kyc/privilege-levels/1
```

**Enhanced Response Structure:**
```json
{
  "success": true,
  "message": "Available privilege levels retrieved successfully",
  "data": [
    {
      "level": 1,
      "name": "Basic Access",
      "description": "Default level for new accounts with basic services access",
      "services": ["can_cashout", "can_transfer", "can_view_balance"],
      "limits": {
        "cashout_limit": 500000,
        "daily_transfer_limit": 1000000,
        "monthly_limit": 5000000
      },
      "requirements": [
        {
          "type": "valid_id",
          "name": "Valid Government ID",
          "qty": 1,
          "description": "Driver's license, passport, or national ID"
        },
        {
          "type": "address_proof",
          "name": "Proof of Address",
          "qty": 1,
          "description": "Utility bill or bank statement"
        }
      ],
      "is_active": true
    }
  ]
}
```

---

## 📄 **KYC Request Flow**

### **1. Generate Access Token (Internal)**
```http
POST /api/v1/KYC/tokens/generate
Authorization: Bearer <token>
Content-Type: application/json

{
  "account_code": "BWC9119454067",
  "hours_valid": 24
}
```

**Response:**
```json
{
  "success": true,
  "message": "Access token generated successfully",
  "data": {
    "token": "mzNg99wzzixt2atLPmsyFpOH7SfkQ4qd",
    "expires_at": "2025-01-20T10:00:00Z"
  }
}
```

### **2. Submit KYC Request (Public)**
```http
POST /api/v1/public/kyc/submit
Content-Type: multipart/form-data

access_token: mzNg99wzzixt2atLPmsyFpOH7SfkQ4qd
account_code: BWC9119454067
request_type: Level Upgrade
priority_level: 2
level_to_upgrade_to: 1
request_description: Requesting level upgrade to access more services
files: [file1.pdf, file2.jpg]
file_description: ID documents and proof of address
```

**Response:**
```json
{
  "success": true,
  "message": "KYC request submitted successfully",
  "data": {
    "kyc_request_id": "KYC123456789012",
    "status": "Submitted",
    "uploaded_files_count": 2,
    "uploaded_files": [
      {
        "file_id": 1,
        "file_name": "KYC123456789012_abc123.pdf",
        "original_name": "id_document.pdf"
      }
    ]
  }
}
```

### **3. Check KYC Status (Public)**
```http
GET /api/v1/public/kyc/status/KYC123456789012
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
    "submitted_at": "2025-01-19T10:00:00Z",
    "completed_at": null,
    "has_files": true,
    "current_level": 0,
    "level_to_upgrade_to": 1,
    "latest_update": "2025-01-19T10:00:00Z",
    "estimated_processing_time": "3-5 business days"
  }
}
```

---

## 📁 **File Management**

### **1. Get File Categories (Internal)**
```http
GET /api/v1/KYC/files/categories
Authorization: Bearer <token>
```

### **2. Get File Categories (Public)**
```http
GET /api/v1/public/kyc/files/categories
```

**Response (Both Internal & Public):**
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

### **3. Get Submission Requirements (Public)**
```http
GET /api/v1/public/kyc/requirements
```

**Response:**
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

---

## 🔧 **Frontend Implementation Notes**

### **1. Token Management**
- Store JWT token in localStorage/sessionStorage for internal APIs
- Use token validation endpoint to verify client access tokens
- Handle token expiration gracefully

### **2. Error Handling**
All APIs return consistent error format:
```json
{
  "success": false,
  "message": "Error description",
  "data": null
}
```

**Common HTTP Status Codes:**
- `200`: Success
- `400`: Bad Request (validation errors)
- `401`: Unauthorized (invalid/missing token)
- `404`: Not Found
- `500`: Internal Server Error

### **3. File Upload**
- Use `multipart/form-data` for file uploads
- Maximum file size: 10MB
- Allowed extensions: `.pdf`, `.jpg`, `.jpeg`, `.png`, `.docx`, `.doc`
- Maximum 5 files per request

### **4. Privilege Structure Usage**
```javascript
// Example of how to use the enhanced privilege structure
const privilege = {
  level: 1,
  name: "Basic Access",
  services: ["can_cashout", "can_transfer"],
  limits: {
    cashout_limit: 500000,
    daily_transfer_limit: 1000000
  },
  requirements: [
    {
      type: "valid_id",
      name: "Valid Government ID",
      qty: 1
    }
  ]
};

// Check if user can perform an action
const canCashout = privilege.services.includes("can_cashout");

// Check limits
const cashoutAmount = 100000;
const canAffordCashout = cashoutAmount <= privilege.limits.cashout_limit;

// Show required documents
privilege.requirements.forEach(req => {
  console.log(`Need ${req.qty} ${req.name}`);
});
```

---

## 🚨 **Important Changes for Frontend Team**

### **1. Update Token Validation URL**
Change from:
```javascript
// OLD
const response = await fetch('/api/v1/KYC/tokens/validate', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ token, account_code })
});
```

To:
```javascript
// NEW
const response = await fetch('/api/v1/public/kyc/tokens/validate', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ token, account_code })
});
```

### **2. Enhanced Privilege Data Structure**
The privilege endpoints now return enhanced data with `services`, `limits`, and `requirements` arrays. Update your frontend to handle this new structure.

### **3. No Authentication for Public Endpoints**
These endpoints don't require JWT tokens:
- `POST /api/v1/public/kyc/tokens/validate`
- `GET /api/v1/public/kyc/privilege-levels/{companyId}`
- `POST /api/v1/public/kyc/submit`
- `GET /api/v1/public/kyc/status/{kycRequestId}`
- `GET /api/v1/public/kyc/requirements`
- `GET /api/v1/public/kyc/files/categories`
- `GET /api/v1/public/kyc/company-by-account` ⭐ **NEW**

---

## 📞 **Support & Testing**

### **Test Environment**
- **Base URL**: `https://localhost:7239`
- **Internal APIs**: Require JWT authentication
- **Public APIs**: No authentication required

### **Debug Endpoints**
- `POST /api/v1/auth/generate-bcrypt` - Generate BCrypt hashes for testing
- `POST /api/v1/auth/verify-bcrypt` - Verify BCrypt hashes
- `GET /api/v1/auth/debug-user-credentials` - Debug user credentials

### **Sample Test Data**
```json
{
  "company": {
    "company_id": 1,
    "company_name": "BLOOMS WELLNESS",
    "company_code": "BWC",
    "company_type": "networking"
  },
  "privilege": {
    "level": 1,
    "name": "Basic Access",
    "services": ["can_cashout"],
    "limits": { "cashout_limit": 500000 },
    "requirements": [
      { "type": "valid_id", "qty": 1 },
      { "type": "address_proof", "qty": 1 }
    ]
  }
}
```

---

## 🎯 **Next Steps for Frontend Team**

1. **Update API endpoints** - Change token validation URL
2. **Implement enhanced privilege structure** - Handle services, limits, requirements
3. **Test public endpoints** - Ensure no authentication is required
4. **Update file upload logic** - Handle new file categories and requirements
5. **Implement privilege-based UI** - Show/hide features based on user privileges

---

*Last Updated: January 19, 2025*
*API Version: v1*
*Contact: Backend Team for any questions or issues*
