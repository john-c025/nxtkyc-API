-- ============================================================================
-- KYC SERVICE TEST DATA SCRIPT
-- Company: BLOOMS WELLNESS (Networking company with Level 2 privilege)
-- Contact: Lhee Blooms (no middle name)
-- ============================================================================

USE [nextapp_kyc_service]
GO

-- ============================================================================
-- 1. SETUP PHASE - Create Company and Base Configuration
-- ============================================================================

-- Insert Company
INSERT INTO [dbo].[client_companies] (
    [company_name], 
    [company_code], 
    [company_type], 
    [is_active], 
    [created_by], 
    [updated_by]
) VALUES (
    'BLOOMS WELLNESS',
    'BWC001',
    'networking',
    1,
    'SYSTEM',
    'SYSTEM'
);

-- Get the company_id for subsequent inserts
DECLARE @company_id INT = SCOPE_IDENTITY();
PRINT 'Created company with ID: ' + CAST(@company_id AS NVARCHAR(10));

-- ============================================================================
-- 2. Define KYC Privilege Levels (Level 0, 1, and 2)
-- ============================================================================

-- Level 0 - Basic/Default Level
INSERT INTO [dbo].[kyc_privileges] (
    [company_id],
    [privilege_level],
    [privilege_name],
    [privilege_description],
    [privileges_json],
    [is_active],
    [created_by],
    [updated_by]
) VALUES (
    @company_id,
    0,
    'Basic Access',
    'Default level for new accounts with basic services access',
    '{"services": ["basic_consultation", "health_tips"], "limits": {"monthly_consultations": 2, "resource_downloads": 5}}',
    1,
    'SYSTEM',
    'SYSTEM'
);

-- Level 1 - Standard Level
INSERT INTO [dbo].[kyc_privileges] (
    [company_id],
    [privilege_level],
    [privilege_name],
    [privilege_description],
    [privileges_json],
    [is_active],
    [created_by],
    [updated_by]
) VALUES (
    @company_id,
    1,
    'Standard Access',
    'Enhanced access with additional wellness services and higher limits',
    '{"services": ["basic_consultation", "health_tips", "nutrition_planning", "fitness_tracking"], "limits": {"monthly_consultations": 5, "resource_downloads": 15, "nutrition_plans": 3}}',
    1,
    'SYSTEM',
    'SYSTEM'
);

-- Level 2 - Premium Level (Maximum for this company)
INSERT INTO [dbo].[kyc_privileges] (
    [company_id],
    [privilege_level],
    [privilege_name],
    [privilege_description],
    [privileges_json],
    [is_active],
    [created_by],
    [updated_by]
) VALUES (
    @company_id,
    2,
    'Premium Access',
    'Full access to all wellness services with unlimited usage',
    '{"services": ["basic_consultation", "health_tips", "nutrition_planning", "fitness_tracking", "premium_coaching", "group_sessions", "specialist_referrals"], "limits": {"monthly_consultations": "unlimited", "resource_downloads": "unlimited", "nutrition_plans": "unlimited", "coaching_sessions": 8}}',
    1,
    'SYSTEM',
    'SYSTEM'
);

-- ============================================================================
-- 3. Create System Users for KYC Management
-- ============================================================================

-- Admin User
INSERT INTO [dbo].[sys_users] (
    [system_user_key],
    [user_id],
    [fname],
    [mname],
    [sname],
    [email],
    [mobileno],
    [is_active]
) VALUES (
    'ADMIN001',
    'ADMIN001',
    'John',
    'Michael',
    'Administrator',
    'admin@bloomswellness.com',
    '+1234567890',
    1
);

-- KYC Reviewer User
INSERT INTO [dbo].[sys_users] (
    [system_user_key],
    [user_id],
    [fname],
    [mname],
    [sname],
    [email],
    [mobileno],
    [is_active]
) VALUES (
    'KYC001',
    'KYC001',
    'Sarah',
    'Jane',
    'Reviewer',
    'kyc.reviewer@bloomswellness.com',
    '+1234567891',
    1
);

-- ============================================================================
-- 4. Set User Credentials
-- ============================================================================

-- Admin Credentials (password: Admin123!)
INSERT INTO [dbo].[sys_user_credentials] (
    [user_id],
    [coded_id],
    [username],
    [coded_username],
    [coded_password],
    [status]
) VALUES (
    'ADMIN001',
    'QURNSU4wMDE=', -- Base64 encoded ADMIN001
    'admin.blooms',
    'YWRtaW4uYmxvb21z', -- Base64 encoded admin.blooms
    '$2a$10$X8l8VQ.fB5F2yDx0YjqRmeq1ZO2hHUzOG.oELKpL3FY8hG2kV3Dpm', -- BCrypt hash of Admin123!
    1
);

-- KYC Reviewer Credentials (password: Kyc123!)
INSERT INTO [dbo].[sys_user_credentials] (
    [user_id],
    [coded_id],
    [username],
    [coded_username],
    [coded_password],
    [status]
) VALUES (
    'KYC001',
    'S1lDMDAx', -- Base64 encoded KYC001
    'kyc.reviewer',
    'a3ljLnJldmlld2Vy', -- Base64 encoded kyc.reviewer
    '$2a$10$Y9m9WR.gC6G3zEy1ZkrSneR2ZP3iIVAPH.pFMpM4GZ9iH3lW4Eqn.', -- BCrypt hash of Kyc123!
    1
);

-- ============================================================================
-- 5. Assign User Company Access Permissions
-- ============================================================================

-- Admin full access
INSERT INTO [dbo].[sys_user_company_access] (
    [user_id],
    [company_id],
    [can_approve],
    [can_reject],
    [can_archive],
    [is_active],
    [assigned_by],
    [updated_by]
) VALUES (
    (SELECT autoid FROM sys_users WHERE user_id = 'ADMIN001'),
    @company_id,
    1, -- can approve
    1, -- can reject
    1, -- can archive
    1, -- is active
    'SYSTEM',
    'SYSTEM'
);

-- KYC Reviewer access
INSERT INTO [dbo].[sys_user_company_access] (
    [user_id],
    [company_id],
    [can_approve],
    [can_reject],
    [can_archive],
    [is_active],
    [assigned_by],
    [updated_by]
) VALUES (
    (SELECT autoid FROM sys_users WHERE user_id = 'KYC001'),
    @company_id,
    1, -- can approve
    1, -- can reject
    0, -- cannot archive
    1, -- is active
    'SYSTEM',
    'SYSTEM'
);

-- ============================================================================
-- 6. CLIENT REGISTRATION - Create Sample Client Account
-- ============================================================================

-- Create client account for Lhee Blooms
INSERT INTO [dbo].[client_accounts] (
    [company_id],
    [account_code],
    [account_origin_number],
    [account_id],
    [fname],
    [mname],
    [sname],
    [account_status],
    [current_privilege_level],
    [account_metadata],
    [is_active],
    [created_by],
    [updated_by]
) VALUES (
    @company_id,
    'BWC-ACC-001',
    'BLOOM-ORG-12345',
    'LHEE-BLOOMS-001',
    'Lhee',
    '', -- No middle name as requested
    'Blooms',
    1, -- Active status
    0, -- Starting at level 0
    '{"registration_source": "company_portal", "contact_preferences": {"email": true, "sms": false}, "special_notes": "Company contact person"}',
    1,
    'SYSTEM',
    'SYSTEM'
);

-- Get the client account ID for subsequent operations
DECLARE @client_account_id INT = SCOPE_IDENTITY();
PRINT 'Created client account with ID: ' + CAST(@client_account_id AS NVARCHAR(10));

-- ============================================================================
-- 7. SAMPLE KYC REQUEST WORKFLOW
-- ============================================================================

-- Generate access token for KYC request
INSERT INTO [dbo].[kyc_access_tokens] (
    [account_code],
    [token_hash],
    [expires_at],
    [is_used],
    [used_at],
    [kyc_request_id]
) VALUES (
    'BWC-ACC-001',
    'b8c9d0e1f2g3h4i5j6k7l8m9n0o1p2q3r4s5t6u7v8w9x0y1z2', -- Sample token hash
    DATEADD(DAY, 7, GETDATE()), -- Expires in 7 days
    0, -- Not used yet
    NULL,
    NULL -- Will be updated when request is created
);

DECLARE @token_id INT = SCOPE_IDENTITY();

-- Create a sample KYC request for privilege upgrade (Level 0 to Level 1)
DECLARE @kyc_request_id NVARCHAR(64) = 'KYC-' + FORMAT(@company_id, '000') + '-' + FORMAT(GETDATE(), 'yyyyMMdd') + '-001';

INSERT INTO [dbo].[kyc_requests] (
    [kyc_request_id],
    [company_id],
    [client_account_id],
    [token_id],
    [request_type],
    [request_status],
    [priority_level],
    [request_description],
    [current_level],
    [level_to_upgrade_to],
    [has_files],
    [is_one_time_only],
    [submitted_at],
    [created_by],
    [updated_by]
) VALUES (
    @kyc_request_id,
    @company_id,
    @client_account_id,
    @token_id,
    'PRIVILEGE_UPGRADE',
    1, -- Pending status
    2, -- Medium priority
    'Request to upgrade from Basic to Standard access level for enhanced wellness services',
    0, -- Current level 0
    1, -- Upgrade to level 1
    1, -- Has supporting files
    1, -- One-time request
    GETDATE(),
    'BWC-ACC-001',
    'BWC-ACC-001'
);

-- Update the access token with the KYC request ID
UPDATE [dbo].[kyc_access_tokens] 
SET [kyc_request_id] = @kyc_request_id, [is_used] = 1, [used_at] = GETDATE()
WHERE [autoid] = @token_id;

-- ============================================================================
-- 8. SAMPLE MEDIA FILES (Simulated)
-- ============================================================================

-- Identity Document
INSERT INTO [dbo].[kyc_media_files] (
    [kyc_request_id],
    [file_name],
    [file_original_name],
    [file_type],
    [file_extension],
    [file_size],
    [file_path],
    [file_url],
    [mime_type],
    [file_category],
    [file_description],
    [is_verified],
    [uploaded_by]
) VALUES (
    @kyc_request_id,
    'id_document_' + REPLACE(NEWID(), '-', '') + '.pdf',
    'lhee_blooms_id.pdf',
    1, -- Document type
    '.pdf',
    2048576, -- 2MB
    '/uploads/kyc/' + @kyc_request_id + '/documents/',
    NULL,
    'application/pdf',
    1, -- Identity document category
    'Government-issued ID document for identity verification',
    0, -- Not yet verified
    'BWC-ACC-001'
);

-- Proof of Address
INSERT INTO [dbo].[kyc_media_files] (
    [kyc_request_id],
    [file_name],
    [file_original_name],
    [file_type],
    [file_extension],
    [file_size],
    [file_path],
    [file_url],
    [mime_type],
    [file_category],
    [file_description],
    [is_verified],
    [uploaded_by]
) VALUES (
    @kyc_request_id,
    'address_proof_' + REPLACE(NEWID(), '-', '') + '.pdf',
    'utility_bill_proof.pdf',
    1, -- Document type
    '.pdf',
    1536789, -- 1.5MB
    '/uploads/kyc/' + @kyc_request_id + '/documents/',
    NULL,
    'application/pdf',
    2, -- Address proof category
    'Utility bill for address verification',
    0, -- Not yet verified
    'BWC-ACC-001'
);

-- ============================================================================
-- 9. AUDIT TRAIL - Initial Request Creation
-- ============================================================================

INSERT INTO [dbo].[kyc_audit_trail] (
    [kyc_request_id],
    [action_type],
    [action_by],
    [action_timestamp],
    [old_status],
    [new_status],
    [action_details]
) VALUES (
    @kyc_request_id,
    1, -- Created
    'BWC-ACC-001',
    GETDATE(),
    NULL, -- No previous status
    1, -- New status: Pending
    'KYC request created for privilege upgrade from Level 0 to Level 1'
);

-- ============================================================================
-- 10. SAMPLE DATA SUMMARY
-- ============================================================================

PRINT '============================================================================';
PRINT 'TEST DATA CREATION COMPLETED SUCCESSFULLY';
PRINT '============================================================================';
PRINT 'Company: BLOOMS WELLNESS (ID: ' + CAST(@company_id AS NVARCHAR(10)) + ')';
PRINT 'Company Code: BWC001';
PRINT 'Company Type: networking';
PRINT 'Maximum Privilege Level: 2 (Premium Access)';
PRINT '';
PRINT 'Contact Person: Lhee Blooms (Account ID: ' + CAST(@client_account_id AS NVARCHAR(10)) + ')';
PRINT 'Account Code: BWC-ACC-001';
PRINT 'Current Privilege Level: 0 (Basic Access)';
PRINT '';
PRINT 'Sample KYC Request: ' + @kyc_request_id;
PRINT 'Request Type: PRIVILEGE_UPGRADE (Level 0 → Level 1)';
PRINT 'Status: Pending (1)';
PRINT 'Priority: Medium (2)';
PRINT 'Has Files: Yes (2 documents uploaded)';
PRINT '';
PRINT 'System Users Created:';
PRINT '- ADMIN001 (admin.blooms) - Full permissions';
PRINT '- KYC001 (kyc.reviewer) - Review permissions';
PRINT '';
PRINT 'Available Privilege Levels:';
PRINT '- Level 0: Basic Access (default)';
PRINT '- Level 1: Standard Access';
PRINT '- Level 2: Premium Access (maximum)';
PRINT '============================================================================';

-- ============================================================================
-- 11. VERIFICATION QUERIES (Optional - for testing)
-- ============================================================================

-- Verify company creation
SELECT 'Company Info' as Section, * FROM client_companies WHERE company_id = @company_id;

-- Verify privilege levels
SELECT 'Privilege Levels' as Section, privilege_level, privilege_name, privilege_description 
FROM kyc_privileges WHERE company_id = @company_id ORDER BY privilege_level;

-- Verify client account
SELECT 'Client Account' as Section, account_code, fname, mname, sname, current_privilege_level 
FROM client_accounts WHERE company_id = @company_id;

-- Verify KYC request
SELECT 'KYC Request' as Section, kyc_request_id, request_type, request_status, current_level, level_to_upgrade_to 
FROM kyc_requests WHERE kyc_request_id = @kyc_request_id;

-- Verify uploaded files
SELECT 'Uploaded Files' as Section, file_original_name, file_category, file_description, is_verified 
FROM kyc_media_files WHERE kyc_request_id = @kyc_request_id;

-- Verify system users
SELECT 'System Users' as Section, user_id, fname, sname, email FROM sys_users;

-- Verify user permissions
SELECT 'User Permissions' as Section, u.user_id, u.fname, u.sname, uca.can_approve, uca.can_reject, uca.can_archive 
FROM sys_users u 
JOIN sys_user_company_access uca ON u.autoid = uca.user_id 
WHERE uca.company_id = @company_id;
