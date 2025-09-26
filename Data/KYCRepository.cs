using System.Data;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using KYCAPI.Models.KYC;
using KYCAPI.Helpers;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KYCAPI.Data
{
    public class KYCRepository
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<KYCRepository> _logger;
        private readonly IConfiguration _configuration;

        public KYCRepository(ILogger<KYCRepository> logger, DatabaseContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _logger = logger;
            _configuration = configuration;
        }

        // Helper method to set session variable for audit trail
        private async Task SetAuditUserIdAsync(IDbConnection connection, string userId)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                try
                {
                    // SQL Server equivalent - set context info
                    await connection.ExecuteAsync("SET CONTEXT_INFO @UserId", new { UserId = Encoding.UTF8.GetBytes(userId.Trim().PadRight(128)[..128]) });
                    // Verify not needed for SQL Server context info
                    _logger.LogInformation("Session userid set to: {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set session userid, will use database user for audit trail");
                }
            }
        }

        // Helper method for logging audit trails
        private async Task LogKYCAuditTrailAsync(
            IDbConnection connection,
            string kycRequestId,
            byte actionType,
            string actionBy,
            byte? oldStatus = null,
            byte? newStatus = null,
            string? actionDetails = null)
        {
            try
            {
                var query = @"
                    INSERT INTO kyc_audit_trail (
                        kyc_request_id, action_type, action_by, action_timestamp, 
                        old_status, new_status, action_details
                    ) VALUES (
                        @kyc_request_id, @action_type, @action_by, GETDATE(), 
                        @old_status, @new_status, @action_details
                    )";

                await connection.ExecuteAsync(query, new
                {
                    kyc_request_id = kycRequestId,
                    action_type = actionType,
                    action_by = actionBy,
                    old_status = oldStatus,
                    new_status = newStatus,
                    action_details = actionDetails
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging KYC audit trail for request {KYCRequestId}", kycRequestId);
            }
        }

        // Helper method to generate unique IDs
        private string GenerateUniqueId(string prefix, int length = 16)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var randomBytes = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            var randomString = Convert.ToHexString(randomBytes);
            return $"{prefix}_{timestamp}_{randomString}"[..Math.Min(length, $"{prefix}_{timestamp}_{randomString}".Length)];
        }

        // Client Companies Methods
        public async Task<IEnumerable<ClientCompanyModel>> GetAllCompaniesAsync()
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = "SELECT * FROM client_companies WHERE is_active = 1 ORDER BY company_name";
                return await connection.QueryAsync<ClientCompanyModel>(query);
            });
        }

        public async Task<ClientCompanyModel?> GetCompanyByIdAsync(int companyId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = "SELECT * FROM client_companies WHERE company_id = @CompanyId AND is_active = 1";
                return await connection.QueryFirstOrDefaultAsync<ClientCompanyModel>(query, new { CompanyId = companyId });
            });
        }

        public async Task<int> CreateCompanyAsync(ClientCompanyModel company, string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                await SetAuditUserIdAsync(connection, userId);

                var query = @"
                    INSERT INTO client_companies (
                        company_name, company_code, company_type, is_active, 
                        created_at, updated_at, created_by, updated_by
                    ) VALUES (
                        @company_name, @company_code, @company_type, @is_active,
                        GETDATE(), GETDATE(), @created_by, @updated_by
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var parameters = new
                {
                    company.company_name,
                    company.company_code,
                    company.company_type,
                    company.is_active,
                    created_by = userId,
                    updated_by = userId
                };

                return await connection.QuerySingleAsync<int>(query, parameters);
            });
        }

        // Client Accounts Methods
        public async Task<IEnumerable<ClientAccountModel>> GetClientAccountsAsync(int? companyId = null, bool? isActive = null, int page = 1, int pageSize = 50)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var baseQuery = @"
                    SELECT ca.*, cc.company_name
                    FROM client_accounts ca
                    LEFT JOIN client_companies cc ON ca.company_id = cc.company_id
                    WHERE 1=1";

                var parameters = new DynamicParameters();

                if (companyId.HasValue)
                {
                    baseQuery += " AND ca.company_id = @CompanyId";
                    parameters.Add("CompanyId", companyId.Value);
                }

                if (isActive.HasValue)
                {
                    baseQuery += " AND ca.is_active = @IsActive";
                    parameters.Add("IsActive", isActive.Value);
                }

                baseQuery += " ORDER BY ca.autoid DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                parameters.Add("Offset", (page - 1) * pageSize);
                parameters.Add("PageSize", pageSize);

                return await connection.QueryAsync<ClientAccountModel>(baseQuery, parameters);
            });
        }

        public async Task<ClientAccountModel?> GetClientAccountByCodeAsync(string accountCode)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                    SELECT ca.*, cc.company_name
                    FROM client_accounts ca
                    LEFT JOIN client_companies cc ON ca.company_id = cc.company_id
                    WHERE ca.account_code = @AccountCode AND ca.is_active = 1";
                
                return await connection.QueryFirstOrDefaultAsync<ClientAccountModel>(query, new { AccountCode = accountCode });
            });
        }

        public async Task<int> CreateClientAccountAsync(CreateClientAccountDto clientDto, string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                await SetAuditUserIdAsync(connection, userId);

                // Get company code for account code generation
                var companyQuery = "SELECT company_code FROM client_companies WHERE company_id = @CompanyId";
                var companyCode = await connection.QueryFirstOrDefaultAsync<string>(companyQuery, new { CompanyId = clientDto.company_id });
                
                if (string.IsNullOrEmpty(companyCode))
                    throw new ArgumentException("Invalid company ID");

                // Generate unique account_code and account_id using updated helper methods
                var accountCode = KYCHelper.GenerateAccountCode(companyCode);
                var accountId = KYCHelper.GenerateAccountId();

                var query = @"
                    INSERT INTO client_accounts (
                        company_id, account_code, account_origin_number, account_id,
                        fname, mname, sname, account_status, current_privilege_level,
                        account_metadata, is_active, created_at, updated_at, created_by, updated_by
                    ) VALUES (
                        @company_id, @account_code, @account_origin_number, @account_id,
                        @fname, @mname, @sname, 1, 0,
                        @account_metadata, 1, GETDATE(), GETDATE(), @created_by, @updated_by
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var parameters = new
                {
                    clientDto.company_id,
                    account_code = accountCode,
                    clientDto.account_origin_number,
                    account_id = accountId,
                    clientDto.fname,
                    clientDto.mname,
                    clientDto.sname,
                    clientDto.account_metadata,
                    created_by = userId,
                    updated_by = userId
                };

                return await connection.QuerySingleAsync<int>(query, parameters);
            });
        }

        public async Task<bool> UpdateClientAccountAsync(int accountId, UpdateClientAccountDto updateDto)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                await SetAuditUserIdAsync(connection, updateDto.updated_by);

                var updateFields = new List<string>();
                var parameters = new DynamicParameters();
                parameters.Add("AccountId", accountId);
                parameters.Add("UpdatedBy", updateDto.updated_by);

                if (!string.IsNullOrEmpty(updateDto.fname))
                {
                    updateFields.Add("fname = @fname");
                    parameters.Add("fname", updateDto.fname);
                }

                if (!string.IsNullOrEmpty(updateDto.mname))
                {
                    updateFields.Add("mname = @mname");
                    parameters.Add("mname", updateDto.mname);
                }

                if (!string.IsNullOrEmpty(updateDto.sname))
                {
                    updateFields.Add("sname = @sname");
                    parameters.Add("sname", updateDto.sname);
                }

                if (updateDto.account_status.HasValue)
                {
                    updateFields.Add("account_status = @account_status");
                    parameters.Add("account_status", updateDto.account_status.Value);
                }

                if (updateDto.current_privilege_level.HasValue)
                {
                    updateFields.Add("current_privilege_level = @current_privilege_level");
                    parameters.Add("current_privilege_level", updateDto.current_privilege_level.Value);
                }

                if (!string.IsNullOrEmpty(updateDto.account_metadata))
                {
                    updateFields.Add("account_metadata = @account_metadata");
                    parameters.Add("account_metadata", updateDto.account_metadata);
                }

                if (updateDto.is_active.HasValue)
                {
                    updateFields.Add("is_active = @is_active");
                    parameters.Add("is_active", updateDto.is_active.Value);
                }

                if (!updateFields.Any())
                    return true;

                updateFields.Add("updated_at = GETDATE()");
                updateFields.Add("updated_by = @UpdatedBy");

                var query = $@"
                    UPDATE client_accounts 
                    SET {string.Join(", ", updateFields)}
                    WHERE autoid = @AccountId";

                var rowsAffected = await connection.ExecuteAsync(query, parameters);
                return rowsAffected > 0;
            });
        }

        public async Task<UpsertClientAccountResult> UpsertClientAccountAsync(UpsertClientAccountDto upsertDto, string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                await SetAuditUserIdAsync(connection, userId);

                // If account_origin_number is provided, check if account already exists
                if (!string.IsNullOrEmpty(upsertDto.account_origin_number))
                {
                var existingAccountQuery = @"
                    SELECT autoid, account_code, account_id, current_privilege_level
                    FROM client_accounts 
                    WHERE company_id = @CompanyId AND account_origin_number = @AccountOriginNumber";

                var existingAccount = await connection.QueryFirstOrDefaultAsync(existingAccountQuery, new
                {
                    CompanyId = upsertDto.company_id,
                    AccountOriginNumber = upsertDto.account_origin_number
                });

                if (existingAccount != null)
                {
                    // Update existing account - only update privilege level if it's different
                    if (existingAccount.current_privilege_level != upsertDto.current_privilege_level)
                    {
                        var updateQuery = @"
                            UPDATE client_accounts 
                            SET current_privilege_level = @NewPrivilegeLevel, 
                                updated_at = GETDATE(), 
                                updated_by = @UpdatedBy
                            WHERE autoid = @AccountId";

                        await connection.ExecuteAsync(updateQuery, new
                        {
                            NewPrivilegeLevel = upsertDto.current_privilege_level,
                            UpdatedBy = userId,
                            AccountId = existingAccount.autoid
                        });
                    }

                    return new UpsertClientAccountResult
                    {
                        ClientId = existingAccount.autoid,
                        AccountCode = existingAccount.account_code,
                        AccountId = existingAccount.account_id,
                        IsNewAccount = false
                    };
                }
                }

                // Create new account (either no account_origin_number provided or no existing account found)
                    // Get company code for account code generation
                    var companyQuery = "SELECT company_code FROM client_companies WHERE company_id = @CompanyId";
                    var companyCode = await connection.QueryFirstOrDefaultAsync<string>(companyQuery, new { CompanyId = upsertDto.company_id });
                    
                    if (string.IsNullOrEmpty(companyCode))
                        throw new ArgumentException("Invalid company ID");

                    // Generate new account code and account ID using updated helper methods
                    var accountCode = KYCHelper.GenerateAccountCode(companyCode);
                    var accountId = KYCHelper.GenerateAccountId();

                // Generate a default account_origin_number if not provided
                var accountOriginNumber = !string.IsNullOrEmpty(upsertDto.account_origin_number) 
                    ? upsertDto.account_origin_number 
                    : $"WEB_{companyCode}_{DateTime.UtcNow:yyyyMMddHHmmss}";

                    var insertQuery = @"
                        INSERT INTO client_accounts (
                            company_id, account_code, account_origin_number, account_id,
                            fname, mname, sname, account_status, current_privilege_level,
                            account_metadata, is_active, created_at, updated_at, created_by, updated_by
                        ) VALUES (
                            @company_id, @account_code, @account_origin_number, @account_id,
                            '', '', '', 1, @current_privilege_level,
                            '', 1, GETDATE(), GETDATE(), @created_by, @updated_by
                        );
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    var newClientId = await connection.QuerySingleAsync<int>(insertQuery, new
                    {
                        company_id = upsertDto.company_id,
                        account_code = accountCode,
                    account_origin_number = accountOriginNumber,
                        account_id = accountId,
                        current_privilege_level = upsertDto.current_privilege_level,
                        created_by = userId,
                        updated_by = userId
                    });

                    return new UpsertClientAccountResult
                    {
                        ClientId = newClientId,
                        AccountCode = accountCode,
                        AccountId = accountId,
                        IsNewAccount = true
                    };
            });
        }

        // Check if account exists and if account origin number is unique
        public async Task<AccountCheckResult> CheckAccountAndOriginExistsAsync(string accountCode)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                // First, get the account details if it exists
                var accountQuery = @"
                    SELECT company_id, account_origin_number
                    FROM client_accounts 
                    WHERE account_code = @AccountCode";

                var account = await connection.QueryFirstOrDefaultAsync(accountQuery, new { AccountCode = accountCode });

                if (account == null)
                {
                    return new AccountCheckResult
                    {
                        AccountExists = false,
                        AccountOriginNumber = null,
                        OriginNumberUnique = false,
                        CompanyId = 0
                    };
                }

                // Check if the account origin number is unique within the same company
                var duplicateQuery = @"
                    SELECT COUNT(1) 
                    FROM client_accounts 
                    WHERE company_id = @CompanyId 
                    AND account_origin_number = @AccountOriginNumber
                    AND account_code != @AccountCode";

                var duplicateCount = await connection.QuerySingleAsync<int>(duplicateQuery, new
                {
                    CompanyId = account.company_id,
                    AccountOriginNumber = account.account_origin_number,
                    AccountCode = accountCode
                });

                return new AccountCheckResult
                {
                    AccountExists = true,
                    AccountOriginNumber = account.account_origin_number,
                    OriginNumberUnique = duplicateCount == 0,
                    CompanyId = account.company_id
                };
            });
        }

        // KYC Access Tokens Methods
        public async Task<string> GenerateAccessTokenAsync(GenerateAccessTokenDto tokenDto)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                // Generate a secure random token (plain string, not Base64)
                var token = GenerateRandomToken(32);
                var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

                var expiresAt = DateTime.UtcNow.AddHours(tokenDto.hours_valid);

                var query = @"
                    INSERT INTO kyc_access_tokens (
                        account_code, token_hash, expires_at, is_used, created_at
                    ) VALUES (
                        @account_code, @token_hash, @expires_at, 0, GETDATE()
                    )";

                await connection.ExecuteAsync(query, new
                {
                    tokenDto.account_code,
                    token_hash = tokenHash,
                    expires_at = expiresAt
                });

                return token;
            });
        }

        // Helper method to generate random token string
        private string GenerateRandomToken(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task<bool> ValidateAndConsumeTokenAsync(string token, string accountCode)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

                var query = @"
                    UPDATE kyc_access_tokens 
                    SET is_used = 1, used_at = GETDATE()
                    WHERE account_code = @account_code 
                    AND token_hash = @token_hash 
                    AND expires_at > GETDATE() 
                    AND is_used = 0";

                var rowsAffected = await connection.ExecuteAsync(query, new
                {
                    account_code = accountCode,
                    token_hash = tokenHash
                });

                return rowsAffected > 0;
            });
        }

        public async Task<TokenValidationResult?> ValidateTokenAsync(string token, string accountCode)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

                var query = @"
                    SELECT kat.*, ca.current_privilege_level, cc.company_name, cc.company_code
                    FROM kyc_access_tokens kat
                    LEFT JOIN client_accounts ca ON kat.account_code = ca.account_code
                    LEFT JOIN client_companies cc ON ca.company_id = cc.company_id
                    WHERE kat.account_code = @account_code 
                    AND kat.token_hash = @token_hash 
                    AND kat.expires_at > GETDATE() 
                    AND kat.is_used = 0";

                var result = await connection.QueryFirstOrDefaultAsync(query, new
                {
                    account_code = accountCode,
                    token_hash = tokenHash
                });

                if (result == null) return null;

                return new TokenValidationResult
                {
                    IsValid = true,
                    AccountCode = result.account_code,
                    CurrentPrivilegeLevel = result.current_privilege_level,
                    CompanyName = result.company_name,
                    CompanyCode = result.company_code,
                    ExpiresAt = result.expires_at
                };
            });
        }

        // KYC Requests Methods
        public async Task<string> CreateKYCRequestAsync(CreateKYCRequestDto requestDto, string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                await SetAuditUserIdAsync(connection, userId);

                // Get client account details
                var clientAccount = await GetClientAccountByCodeAsync(requestDto.account_code);
                if (clientAccount == null)
                    throw new ArgumentException("Invalid account code");

                var kycRequestId = KYCHelper.GenerateKYCRequestId();

                var query = @"
                    INSERT INTO kyc_requests (
                        kyc_request_id, company_id, client_account_id, request_type,
                        request_status, priority_level, request_description, current_level,
                        level_to_upgrade_to, has_files, is_one_time_only, submitted_at,
                        created_at, updated_at, created_by, updated_by
                    ) VALUES (
                        @kyc_request_id, @company_id, @client_account_id, @request_type,
                        1, @priority_level, @request_description, @current_level,
                        @level_to_upgrade_to, @has_files, 1, GETDATE(),
                        GETDATE(), GETDATE(), @created_by, @updated_by
                    )";

                var parameters = new
                {
                    kyc_request_id = kycRequestId,
                    company_id = clientAccount.company_id,
                    client_account_id = clientAccount.autoid,
                    requestDto.request_type,
                    requestDto.priority_level,
                    requestDto.request_description,
                    current_level = clientAccount.current_privilege_level,
                    requestDto.level_to_upgrade_to,
                    requestDto.has_files,
                    created_by = userId,
                    updated_by = userId
                };

                await connection.ExecuteAsync(query, parameters);

                // Log audit trail
                await LogKYCAuditTrailAsync(connection, kycRequestId, 1, userId, null, 1, "KYC request created");

                return kycRequestId;
            });
        }

        public async Task<IEnumerable<KYCRequestModel>> GetKYCRequestsAsync(
            int? companyId = null,
            byte? status = null,
            byte? priorityLevel = null,
            int page = 1,
            int pageSize = 50)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var baseQuery = @"
                    SELECT kr.*, CONCAT(ca.fname, ' ', ca.mname, ' ', ca.sname) as client_full_name,
                           cc.company_name
                    FROM kyc_requests kr
                    LEFT JOIN client_accounts ca ON kr.client_account_id = ca.autoid
                    LEFT JOIN client_companies cc ON kr.company_id = cc.company_id
                    WHERE 1=1";

                var parameters = new DynamicParameters();

                if (companyId.HasValue)
                {
                    baseQuery += " AND kr.company_id = @CompanyId";
                    parameters.Add("CompanyId", companyId.Value);
                }

                if (status.HasValue)
                {
                    baseQuery += " AND kr.request_status = @Status";
                    parameters.Add("Status", status.Value);
                }

                if (priorityLevel.HasValue)
                {
                    baseQuery += " AND kr.priority_level = @PriorityLevel";
                    parameters.Add("PriorityLevel", priorityLevel.Value);
                }

                baseQuery += " ORDER BY kr.autoid DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                parameters.Add("Offset", (page - 1) * pageSize);
                parameters.Add("PageSize", pageSize);

                return await connection.QueryAsync<KYCRequestModel>(baseQuery, parameters);
            });
        }

        public async Task<KYCRequestDetailedModel?> GetKYCRequestDetailedAsync(string kycRequestId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                // Get main request details
                var requestQuery = @"
                    SELECT kr.*, CONCAT(ca.fname, ' ', ca.mname, ' ', ca.sname) as client_full_name,
                           cc.company_name
                    FROM kyc_requests kr
                    LEFT JOIN client_accounts ca ON kr.client_account_id = ca.autoid
                    LEFT JOIN client_companies cc ON kr.company_id = cc.company_id
                    WHERE kr.kyc_request_id = @KYCRequestId";

                var request = await connection.QueryFirstOrDefaultAsync<KYCRequestDetailedModel>(
                    requestQuery, new { KYCRequestId = kycRequestId });

                if (request == null) return null;

                // Get attached files
                var filesQuery = "SELECT * FROM kyc_media_files WHERE kyc_request_id = @KYCRequestId ORDER BY uploaded_at";
                request.attached_files = (await connection.QueryAsync<KYCMediaFileModel>(filesQuery, new { KYCRequestId = kycRequestId })).ToList();

                // Get approval actions
                var actionsQuery = @"
                    SELECT kaa.*, CONCAT(su.fname, ' ', su.mname, ' ', su.sname) as approver_name
                    FROM kyc_approval_actions kaa
                    LEFT JOIN sys_users su ON kaa.approver_user_id = su.autoid
                    WHERE kaa.kyc_request_id = @KYCRequestId
                    ORDER BY kaa.action_timestamp DESC";
                request.approval_actions = (await connection.QueryAsync<KYCApprovalActionModel>(actionsQuery, new { KYCRequestId = kycRequestId })).ToList();

                // Get audit trail
                var auditQuery = "SELECT * FROM kyc_audit_trail WHERE kyc_request_id = @KYCRequestId ORDER BY action_timestamp DESC";
                request.audit_trail = (await connection.QueryAsync<KYCAuditTrailModel>(auditQuery, new { KYCRequestId = kycRequestId })).ToList();

                return request;
            });
        }

        public async Task<bool> ProcessKYCRequestAsync(ProcessKYCRequestDto processDto)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                using var transaction = connection.BeginTransaction();
                try
                {
                    await SetAuditUserIdAsync(connection, processDto.approver_user_id);

                    // Get current request status
                    var currentRequest = await connection.QueryFirstOrDefaultAsync<KYCRequestModel>(
                        "SELECT * FROM kyc_requests WHERE kyc_request_id = @KYCRequestId",
                        new { KYCRequestId = processDto.kyc_request_id }, transaction);

                    if (currentRequest == null)
                        throw new ArgumentException("KYC request not found");

                    var oldStatus = currentRequest.request_status;
                    byte newStatus = processDto.action_type switch
                    {
                        1 => 3, // Approve
                        2 => 4, // Reject
                        3 => 5, // Archive
                        4 => 2, // Escalate (set to In Review)
                        _ => oldStatus
                    };

                    // Update request status
                    var updateQuery = @"
                        UPDATE kyc_requests 
                        SET request_status = @NewStatus, 
                            updated_at = GETDATE(), 
                            updated_by = @UpdatedBy";

                    if (processDto.action_type == 1 || processDto.action_type == 2) // Approved or Rejected
                    {
                        updateQuery += ", completed_at = GETDATE()";
                    }

                    if (processDto.action_type == 3) // Archived
                    {
                        updateQuery += ", archived_at = GETDATE()";
                    }

                    updateQuery += " WHERE kyc_request_id = @KYCRequestId";

                    await connection.ExecuteAsync(updateQuery, new
                    {
                        NewStatus = newStatus,
                        UpdatedBy = processDto.approver_user_id,
                        KYCRequestId = processDto.kyc_request_id
                    }, transaction);

                    // Record approval action
                    var approvalQuery = @"
                        INSERT INTO kyc_approval_actions (
                            kyc_request_id, approver_user_id, action_type, remarks,
                            action_timestamp, created_by
                        ) VALUES (
                            @kyc_request_id, @approver_user_id, @action_type, @remarks,
                            GETDATE(), @created_by
                        )";

                    await connection.ExecuteAsync(approvalQuery, new
                    {
                        processDto.kyc_request_id,
                        approver_user_id = int.Parse(processDto.approver_user_id),
                        processDto.action_type,
                        processDto.remarks,
                        created_by = processDto.approver_user_id
                    }, transaction);

                    // If approved, update client privilege level
                    if (processDto.action_type == 1 && currentRequest.level_to_upgrade_to > 0)
                    {
                        await connection.ExecuteAsync(@"
                            UPDATE client_accounts 
                            SET current_privilege_level = @NewLevel, updated_at = GETDATE(), updated_by = @UpdatedBy
                            WHERE autoid = @ClientAccountId",
                            new
                            {
                                NewLevel = currentRequest.level_to_upgrade_to,
                                UpdatedBy = processDto.approver_user_id,
                                ClientAccountId = currentRequest.client_account_id
                            }, transaction);
                    }

                    // Log audit trail
                    await LogKYCAuditTrailAsync(connection, processDto.kyc_request_id,
                        (byte)(processDto.action_type + 1), // Map to audit action types
                        processDto.approver_user_id, oldStatus, newStatus, processDto.remarks);

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "Error processing KYC request {KYCRequestId}", processDto.kyc_request_id);
                    throw;
                }
            });
        }

        // Dashboard and Analytics Methods
        public async Task<KYCDashboardSummaryModel> GetDashboardSummaryAsync(int? companyId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var baseQuery = @"
                    SELECT 
                        COUNT(*) as total_requests,
                        SUM(CASE WHEN request_status = 1 THEN 1 ELSE 0 END) as pending_requests,
                        SUM(CASE WHEN request_status = 2 THEN 1 ELSE 0 END) as in_review_requests,
                        SUM(CASE WHEN request_status = 3 THEN 1 ELSE 0 END) as approved_requests,
                        SUM(CASE WHEN request_status = 4 THEN 1 ELSE 0 END) as rejected_requests,
                        SUM(CASE WHEN request_status = 5 THEN 1 ELSE 0 END) as archived_requests,
                        SUM(CASE WHEN priority_level = 3 THEN 1 ELSE 0 END) as high_priority_requests,
                        SUM(CASE WHEN priority_level = 4 THEN 1 ELSE 0 END) as urgent_priority_requests,
                        CASE 
                            WHEN SUM(CASE WHEN request_status IN (3, 4) THEN 1 ELSE 0 END) > 0 THEN
                                CAST(SUM(CASE WHEN request_status = 3 THEN 1 ELSE 0 END) * 100.0 / 
                                     SUM(CASE WHEN request_status IN (3, 4) THEN 1 ELSE 0 END) AS DECIMAL(5,2))
                            ELSE 0 
                        END as approval_rate,
                        CASE 
                            WHEN SUM(CASE WHEN request_status IN (3, 4) THEN 1 ELSE 0 END) > 0 THEN
                                CAST(SUM(CASE WHEN request_status = 4 THEN 1 ELSE 0 END) * 100.0 / 
                                     SUM(CASE WHEN request_status IN (3, 4) THEN 1 ELSE 0 END) AS DECIMAL(5,2))
                            ELSE 0 
                        END as rejection_rate,
                        CASE 
                            WHEN COUNT(CASE WHEN completed_at IS NOT NULL THEN 1 END) > 0 THEN
                                AVG(CASE WHEN completed_at IS NOT NULL THEN 
                                    DATEDIFF(HOUR, submitted_at, completed_at) ELSE NULL END)
                            ELSE 0 
                        END as average_processing_hours
                    FROM kyc_requests
                    WHERE 1=1";

                var parameters = new DynamicParameters();

                if (companyId.HasValue)
                {
                    baseQuery += " AND company_id = @CompanyId";
                    parameters.Add("CompanyId", companyId.Value);
                }

                if (fromDate.HasValue)
                {
                    baseQuery += " AND created_at >= @FromDate";
                    parameters.Add("FromDate", fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    baseQuery += " AND created_at <= @ToDate";
                    parameters.Add("ToDate", toDate.Value);
                }

                return await connection.QueryFirstOrDefaultAsync<KYCDashboardSummaryModel>(baseQuery, parameters) ?? new KYCDashboardSummaryModel();
            });
        }

        public async Task<IEnumerable<KYCCompanyStatisticsModel>> GetCompanyStatisticsAsync()
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                    SELECT 
                        cc.company_id,
                        cc.company_name,
                        COUNT(DISTINCT ca.autoid) as total_clients,
                        COUNT(DISTINCT CASE WHEN ca.is_active = 1 THEN ca.autoid END) as active_clients,
                        COUNT(kr.autoid) as total_requests,
                        SUM(CASE WHEN kr.request_status = 1 THEN 1 ELSE 0 END) as pending_requests,
                        SUM(CASE WHEN kr.request_status = 3 THEN 1 ELSE 0 END) as approved_requests,
                        SUM(CASE WHEN kr.request_status = 4 THEN 1 ELSE 0 END) as rejected_requests,
                        CASE 
                            WHEN SUM(CASE WHEN kr.request_status IN (3, 4) THEN 1 ELSE 0 END) > 0 THEN
                                CAST(SUM(CASE WHEN kr.request_status = 3 THEN 1 ELSE 0 END) * 100.0 / 
                                     SUM(CASE WHEN kr.request_status IN (3, 4) THEN 1 ELSE 0 END) AS DECIMAL(5,2))
                            ELSE 0 
                        END as approval_rate,
                        CASE 
                            WHEN COUNT(CASE WHEN kr.completed_at IS NOT NULL THEN 1 END) > 0 THEN
                                AVG(CASE WHEN kr.completed_at IS NOT NULL THEN 
                                    DATEDIFF(HOUR, kr.submitted_at, kr.completed_at) ELSE NULL END)
                            ELSE 0 
                        END as average_processing_hours
                    FROM client_companies cc
                    LEFT JOIN client_accounts ca ON cc.company_id = ca.company_id
                    LEFT JOIN kyc_requests kr ON ca.autoid = kr.client_account_id
                    WHERE cc.is_active = 1
                    GROUP BY cc.company_id, cc.company_name
                    ORDER BY cc.company_name";

                return await connection.QueryAsync<KYCCompanyStatisticsModel>(query);
            });
        }

        // File Management Methods
        public async Task<int> SaveKYCMediaFileAsync(KYCMediaFileModel mediaFile)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                    INSERT INTO kyc_media_files (
                        kyc_request_id, file_name, file_original_name, file_type,
                        file_extension, file_size, file_path, file_url, mime_type,
                        file_category, file_description, is_verified, uploaded_at,                         uploaded_by
                    ) VALUES (
                        @kyc_request_id, @file_name, @file_original_name, @file_type,
                        @file_extension, @file_size, @file_path, @file_url, @mime_type,
                        @file_category, @file_description, @is_verified, GETDATE(), @uploaded_by
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                return await connection.QuerySingleAsync<int>(query, mediaFile);
            });
        }

        public async Task<IEnumerable<KYCMediaFileModel>> GetKYCMediaFilesAsync(string kycRequestId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = "SELECT * FROM kyc_media_files WHERE kyc_request_id = @KYCRequestId ORDER BY uploaded_at";
                return await connection.QueryAsync<KYCMediaFileModel>(query, new { KYCRequestId = kycRequestId });
            });
        }

        // System Users Methods
        public async Task<SystemUserModel?> GetSystemUserByIdAsync(string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = "SELECT * FROM sys_users WHERE user_id = @UserId AND is_active = 1";
                return await connection.QueryFirstOrDefaultAsync<SystemUserModel>(query, new { UserId = userId });
            });
        }

        public async Task<IEnumerable<SystemUserCompanyAccessModel>> GetUserCompanyAccessAsync(int userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                    SELECT suca.*, cc.company_name
                    FROM sys_user_company_access suca
                    LEFT JOIN client_companies cc ON suca.company_id = cc.company_id
                    WHERE suca.user_id = @UserId AND suca.is_active = 1";
                
                return await connection.QueryAsync<SystemUserCompanyAccessModel>(query, new { UserId = userId });
            });
        }

        // KYC Privileges Methods
        public async Task<IEnumerable<KYCPrivilegeModel>> GetKYCPrivilegesAsync(int? companyId = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var baseQuery = @"
                    SELECT kp.*, cc.company_name
                    FROM kyc_privileges kp
                    LEFT JOIN client_companies cc ON kp.company_id = cc.company_id
                    WHERE kp.is_active = 1";

                var parameters = new DynamicParameters();

                if (companyId.HasValue)
                {
                    baseQuery += " AND kp.company_id = @CompanyId";
                    parameters.Add("CompanyId", companyId.Value);
                }

                baseQuery += " ORDER BY kp.company_id, kp.privilege_level";

                return await connection.QueryAsync<KYCPrivilegeModel>(baseQuery, parameters);
            });
        }

        // File Categories Methods
        public async Task<IEnumerable<FileCategoryModel>> GetFileCategoriesAsync()
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                // Return static file categories since they're not stored in database
                var categories = new List<FileCategoryModel>
                {
                    new FileCategoryModel { Id = 1, Name = "ID Documents", Description = "Government-issued identification documents" },
                    new FileCategoryModel { Id = 2, Name = "Address Proof", Description = "Utility bills, bank statements, lease agreements" },
                    new FileCategoryModel { Id = 3, Name = "Financial Documents", Description = "Income verification, bank statements, tax returns" },
                    new FileCategoryModel { Id = 4, Name = "Authorization Documents", Description = "Signatures, authorization forms, power of attorney" },
                    new FileCategoryModel { Id = 99, Name = "General/Other", Description = "Miscellaneous documents" }
                };

                return categories;
            });
        }

        // Company Information Methods
        public async Task<object?> GetCompanyByAccountCodeAsync(string accountCode)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                    SELECT 
                        cc.company_id,
                        cc.company_name,
                        cc.company_code,
                        cc.company_type,
                        cc.is_active,
                        cc.created_at,
                        ca.account_code,
                        ca.current_privilege_level,
                        ca.account_status
                    FROM client_companies cc
                    INNER JOIN client_accounts ca ON cc.company_id = ca.company_id
                    WHERE ca.account_code = @account_code 
                    AND ca.is_active = 1 
                    AND cc.is_active = 1";

                var result = await connection.QueryFirstOrDefaultAsync(query, new { account_code = accountCode });

                if (result == null) return null;

                return new
                {
                    company_id = result.company_id,
                    company_name = result.company_name,
                    company_code = result.company_code,
                    company_type = result.company_type,
                    is_active = result.is_active,
                    created_at = result.created_at,
                    account_info = new
                    {
                        account_code = result.account_code,
                        current_privilege_level = result.current_privilege_level,
                        account_status = result.account_status
                    }
                };
            });
        }
    }
}
