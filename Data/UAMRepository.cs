using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using KYCAPI.Models.Global;
using KYCAPI.Models.User;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace KYCAPI.Data
{
    public class UAMRepository
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<UAMRepository> _logger;

        private static class StoredProcedures
        {
            public const string RegisterUser = "core.register_system_user";
            // Changed from stored procedure to function call
            public const string GetUserDetails = "SELECT * FROM core.get_user_details_by_userid(@p_userid)";
        }

        public UAMRepository(DatabaseContext dbContext, ILogger<UAMRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // Helper method to set session variable for audit trail
        private async Task SetAuditUserIdAsync(IDbConnection connection, string userId)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                try
                {
                    await connection.ExecuteAsync("SELECT set_config('app.userid', @UserId, true);", new { UserId = userId.Trim() });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set session userid, will use database user for audit trail");
                }
            }
        }

        // Helper method for logging audit trails
        private async Task LogAuditTrailAsync(
            IDbConnection connection,
            string tableName,
            long recordId,
            string action,
            string userid,
            object oldData = null,
            object newData = null,
            string remarks = null)
        {
            try
            {
                // Safely serialize the data using Newtonsoft.Json
                string oldDataJson = null;
                string newDataJson = null;

                if (oldData != null)
                {
                    try
                    {
                        oldDataJson = JsonConvert.SerializeObject(oldData, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            DateFormatHandling = DateFormatHandling.IsoDateFormat,
                            DateTimeZoneHandling = DateTimeZoneHandling.Utc
                        });
                    }
                    catch (Exception serializeEx)
                    {
                        _logger.LogWarning(serializeEx, "Failed to serialize old data for record {RecordId}, using fallback", recordId);
                        oldDataJson = JsonConvert.SerializeObject(new { error = "Serialization failed - partial data only" });
                    }
                }

                if (newData != null)
                {
                    try
                    {
                        newDataJson = JsonConvert.SerializeObject(newData, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            DateFormatHandling = DateFormatHandling.IsoDateFormat,
                            DateTimeZoneHandling = DateTimeZoneHandling.Utc
                        });
                    }
                    catch (Exception serializeEx)
                    {
                        _logger.LogWarning(serializeEx, "Failed to serialize new data for record {RecordId}, using fallback", recordId);
                        newDataJson = JsonConvert.SerializeObject(new { error = "Serialization failed - partial data only" });
                    }
                }

                var query = @"
                    INSERT INTO core.sys_audit_trails (
                        table_name, 
                        record_id, 
                        action, 
                        userid, 
                        old_data, 
                        new_data, 
                        action_date, 
                        remarks
                    ) VALUES (
                        @table_name, 
                        @record_id, 
                        @action, 
                        @userid, 
                        @old_data::jsonb, 
                        @new_data::jsonb, 
                        NOW(), 
                        @remarks
                    )";

                var parameters = new
                {
                    table_name = tableName,
                    record_id = recordId,
                    action = action,
                    userid = userid ?? "unknown",
                    old_data = oldDataJson,
                    new_data = newDataJson,
                    remarks = remarks
                };

                await connection.ExecuteAsync(query, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging audit trail for {Action} on {TableName} record {RecordId}", action, tableName, recordId);
            }
        }

        public async Task<RegisterUserRequestResponse> RegisterUserAsync(RegisterUserRequest request)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                string sql = @"SELECT 
            generated_userid as ""GeneratedUserId"",
            coded_id as ""CodedId"",
            coded_username as ""CodedUsername"",
            message as ""Message""
            FROM core.register_system_user(
                @codedpword, 
                @fname, 
                @mname, 
                @sname, 
                @positionid, 
                @contact_number, 
                @email_address)";

                var parameters = new DynamicParameters();
                parameters.Add("@codedpword", request.CodedPassword);
                parameters.Add("@fname", request.FirstName);
                parameters.Add("@mname", request.MiddleName);
                parameters.Add("@sname", request.Surname);
                parameters.Add("@positionid", request.PositionId);
                parameters.Add("@contact_number", request.ContactNumber);
                parameters.Add("@email_address", request.EmailAddress);


                var result = await connection.QueryFirstOrDefaultAsync<RegisterUserRequestResponse>(
                    sql,
                    parameters,
                    commandType: CommandType.Text
                );



                if (result == null)
                    throw new Exception("Failed to register user");

                return result;
            });
        }
        public async Task<UserDetails> GetUserDetailsByIdAsync(string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var parameters = new DynamicParameters();
                parameters.Add("p_userid", userId);

                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    StoredProcedures.GetUserDetails,
                    parameters,
                    commandType: CommandType.Text);

                if (result == null) return null;

                return new UserDetails
                {
                    UserId = result.userid,
                    CodedId = result.codedid,
                    DateRegistered = result.date_registered,
                    FName = result.fname,
                    MName = result.mname,
                    SName = result.sname,
                    ContactNumber = result.contact_number,
                    EmailAddress = result.email_address,
                    PositionId = result.position_id,
                    PositionDesc = result.position_desc,
                    IsCollector = result.is_collector,
                    BranchDesc = result.branch_desc,
                    CountryCode = result.country_code,
                    CityCode = result.city_code,
                    StateCode = result.state_code,
                    Address = result.address,
                    AccessLevel = result.access_level,
                    ActionPermissions = new ActionPermissions
                    {
                        InsertAccess = Convert.ToBoolean(result.insert_access),
                        UpdateAccess = Convert.ToBoolean(result.update_access),
                        UploadAccess = Convert.ToBoolean(result.upload_access),
                        DeleteAccess = Convert.ToBoolean(result.delete_access)
                    },
                    ModuleAccess = new ModuleAccess
                    {
                        can_view_profiles = Convert.ToBoolean(result.can_view_profiles),
                        can_update_basic_info = Convert.ToBoolean(result.can_update_basic_info),
                        can_generate_basic_reports = Convert.ToBoolean(result.can_generate_basic_reports),
                        can_company_scoped_only = Convert.ToBoolean(result.can_company_scoped_only),
                        can_delete_records = Convert.ToBoolean(result.can_delete_records),
                        can_manage_full_records = Convert.ToBoolean(result.can_manage_full_records),
                        can_manage_employment_lifecycle = Convert.ToBoolean(result.can_manage_employment_lifecycle),
                        can_generate_advanced_reports = Convert.ToBoolean(result.can_generate_advanced_reports),
                        can_access_all_companies = Convert.ToBoolean(result.can_access_all_companies),
                        can_view_audit_logs = Convert.ToBoolean(result.can_view_audit_logs)
                    },
                    IsUserActive = Convert.ToBoolean(result.is_user_active)
                };
            });
        }

        public async Task<bool> UpdateUserPasswordAsync(string userId, string hashedPassword, string updatedBy)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                await SetAuditUserIdAsync(connection, updatedBy);

                // Get current user data before update
                var currentUser = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT * FROM core.main_user_creds WHERE userid = @UserId",
                    new { UserId = userId });

                if (currentUser == null)
                {
                    return false;
                }

                var query = @"
                UPDATE core.main_user_creds 
                SET codedpword = @HashedPassword 
                WHERE userid = @UserId";

                var parameters = new { UserId = userId, HashedPassword = hashedPassword };
                var result = await connection.ExecuteAsync(query, parameters);

                if (result > 0)
                {
                    // Log audit trail
                    await LogAuditTrailAsync(connection, "main_user_creds",
                        Convert.ToInt64(currentUser.autoid), "UPDATE PASSWORD", updatedBy,
                        currentUser, new { userid = userId, password_updated = true },
                        $"Password updated for user {userId}");
                }

                return result > 0;
            });
        }

        public async Task<string> GetUserEmailByIdAsync(string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                SELECT email_address 
                FROM core.main_user_contact_details 
                WHERE userid = @UserId";

                var parameters = new { UserId = userId };
                return await connection.QueryFirstOrDefaultAsync<string>(query, parameters);
            });
        }

        public async Task<string> GetModuleAccessByPosition(string positionId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                SELECT * 
                FROM core.sys_position_sys_access 
                WHERE position_id = @PositionId 
                AND status = true";

                var parameters = new { PositionId = positionId };
                return await connection.QueryFirstOrDefaultAsync<string>(query, parameters);
            });
        }

        public async Task<IEnumerable<Positions>> GetPositions()
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                SELECT position_id, position_desc, is_collector
                FROM core.sys_user_positions_definitions 
                WHERE status = true";

                return await connection.QueryAsync<Positions>(query);
            });
        }

        public async Task<bool> SetUserInactiveAsync(string userId, string updatedBy)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                await SetAuditUserIdAsync(connection, updatedBy);

                // Get current user data before update
                var currentUser = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT * FROM core.main_user_primary_details WHERE userid = @UserId",
                    new { UserId = userId });

                if (currentUser == null)
                {
                    return false;
                }

                var query = @"
                UPDATE core.main_user_primary_details 
                SET status = false
                WHERE userid = @UserId";

                var parameters = new { UserId = userId };
                var result = await connection.ExecuteAsync(query, parameters);

                if (result > 0)
                {
                    // Log audit trail
                    await LogAuditTrailAsync(connection, "main_user_primary_details",
                        Convert.ToInt64(currentUser.autoid), "DEACTIVATE USER", updatedBy,
                        currentUser, new { userid = userId, status = false },
                        $"User {userId} deactivated");
                }

                return result > 0;
            });
        }

        public async Task<bool> SetUserActiveAsync(string userId, string updatedBy)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                await SetAuditUserIdAsync(connection, updatedBy);

                // Get current user data before update
                var currentUser = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT * FROM core.main_user_primary_details WHERE userid = @UserId",
                    new { UserId = userId });

                if (currentUser == null)
                {
                    return false;
                }

                var query = @"
                UPDATE core.main_user_primary_details 
                SET status = true
                WHERE userid = @UserId";

                var parameters = new { UserId = userId };
                var result = await connection.ExecuteAsync(query, parameters);

                if (result > 0)
                {
                    // Log audit trail
                    await LogAuditTrailAsync(connection, "main_user_primary_details",
                        Convert.ToInt64(currentUser.autoid), "ACTIVATE USER", updatedBy,
                        currentUser, new { userid = userId, status = true },
                        $"User {userId} activated");
                }

                return result > 0;
            });
        }

        public async Task<IEnumerable<Branches>> GetBranches()
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                SELECT branchid, branch_desc
                FROM core.sys_branches_definitions 
                WHERE status = true";

                return await connection.QueryAsync<Branches>(query);
            });
        }

        public async Task<IEnumerable<Companies>> GetCompanies()
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                SELECT company_id, company_name
                FROM core.sys_affiliate_companies 
                WHERE status = true";

                return await connection.QueryAsync<Companies>(query);
            });
        }

        public async Task<IEnumerable<UserDetails>> GetSystemUserList(bool? isactive)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
        SELECT * FROM core.get_user_details_by_userid(u.userid)
        FROM core.main_user_primary_details u
        WHERE (@IsActive IS NULL OR u.status = @IsActive)";

                var parameters = new { IsActive = isactive };
                var results = await connection.QueryAsync<dynamic>(query, parameters);

                return results.Select(result => new UserDetails
                {
                    UserId = result.userid,
                    CodedId = result.codedid,
                    DateRegistered = result.date_registered,
                    FName = result.fname,
                    MName = result.mname,
                    SName = result.sname,
                    ContactNumber = result.contact_number,
                    EmailAddress = result.email_address,
                    PositionId = result.position_id,
                    PositionDesc = result.position_desc,
                    IsCollector = Convert.ToBoolean(result.is_collector),
                    BranchDesc = result.branch_desc,
                    CountryCode = result.country_code,
                    CityCode = result.city_code,
                    StateCode = result.state_code,
                    Address = result.address,
                    AccessLevel = result.access_level,
                    ActionPermissions = new ActionPermissions
                    {
                        InsertAccess = Convert.ToBoolean(result.insert_access),
                        UpdateAccess = Convert.ToBoolean(result.update_access),
                        UploadAccess = Convert.ToBoolean(result.upload_access),
                        DeleteAccess = Convert.ToBoolean(result.delete_access)
                    },
                    ModuleAccess = new ModuleAccess
                    {
                        can_view_profiles = Convert.ToBoolean(result.can_view_profiles),
                        can_update_basic_info = Convert.ToBoolean(result.can_update_basic_info),
                        can_generate_basic_reports = Convert.ToBoolean(result.can_generate_basic_reports),
                        can_company_scoped_only = Convert.ToBoolean(result.can_company_scoped_only),
                        can_delete_records = Convert.ToBoolean(result.can_delete_records),
                        can_manage_full_records = Convert.ToBoolean(result.can_manage_full_records),
                        can_manage_employment_lifecycle = Convert.ToBoolean(result.can_manage_employment_lifecycle),
                        can_generate_advanced_reports = Convert.ToBoolean(result.can_generate_advanced_reports),
                        can_access_all_companies = Convert.ToBoolean(result.can_access_all_companies),
                        can_view_audit_logs = Convert.ToBoolean(result.can_view_audit_logs)
                    },
                    IsUserActive = Convert.ToBoolean(result.is_user_active)
                });
            });
        }
    }
}