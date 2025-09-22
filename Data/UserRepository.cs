// Data/UserRepository.cs
using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using CoreHRAPI.Models.Global;
using CoreHRAPI.Models.User;
using Microsoft.Extensions.Logging;

namespace CoreHRAPI.Data
{
    public class UserRepository
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<UserRepository> _logger;

        private static class Queries
        {
            public const string GetUserDetailsById = @"
            SELECT * FROM core.get_user_details_by_userid(@p_userid)"; // Ensure parameter name matches


            public const string GetUserDetailsByEmail = @"
                SELECT * FROM core.get_user_details_by_email(@p_email)";

            public const string GetModuleAccess = @"
                SELECT * 
                FROM core.sys_position_sys_access 
                WHERE position_id = @PositionId 
                AND status = true";

            public const string GetUserEmail = @"
                SELECT email_address 
                FROM core.main_user_contact_details 
                WHERE userid = @UserId";

            public const string GetUserPWStatus = @"
                SELECT pw_reset_req 
                FROM core.main_user_reset_status 
                WHERE userid = @UserId";

            public const string GetUserPosition = @"
                SELECT 
                    m.userid, 
                    p.position_desc, 
                    m.position_id 
                FROM core.main_user_position AS m 
                JOIN core.sys_user_positions_definitions AS p 
                    ON p.position_id = m.position_id 
                WHERE userid = @UserId";

            public const string UpdatePassword = @"
                UPDATE core.main_user_creds 
                SET codedpword = @HashedPassword 
                WHERE userid = @UserId";

            public const string StandardUpdatePassword = @"
                UPDATE core.main_user_creds 
                SET codedpword = @HashedPassword 
                WHERE userid = @UserId;

                UPDATE core.main_user_reset_status 
                SET pw_reset_req = false 
                WHERE userid = @UserId;";

            public const string SetPasswordResetFlag = @"
                UPDATE core.main_user_reset_status 
                SET pw_reset_req = @ResetRequired 
                WHERE userid = @UserId";
        }

        public UserRepository(DatabaseContext dbContext, ILogger<UserRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<UserDetails> GetUserDetailsByIdAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Fetching user details for ID: {UserId}", userId);

                return await _dbContext.ExecuteDapperAsync(async connection =>
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("p_userid", userId); // Ensure parameter name matches

                    var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        Queries.GetUserDetailsById,
                        parameters,
                        commandType: CommandType.Text);

                    if (result == null)
                    {
                        _logger.LogWarning("No user found for ID: {UserId}", userId);
                        return null;
                    }

                    return MapUserDetails(result);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user details for ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<UserDetails> GetUserDetailsByEmailAsync(string email)
        {
            try
            {
                _logger.LogInformation("Fetching user details for email: {Email}", email);

                return await _dbContext.ExecuteDapperAsync(async connection =>
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("p_email", email);

                    var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        Queries.GetUserDetailsByEmail,
                        parameters,
                        commandType: CommandType.Text);

                    if (result == null)
                    {
                        _logger.LogWarning("No user found for email: {Email}", email);
                        return null;
                    }

                    return MapUserDetails(result);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user details for email: {Email}", email);
                throw;
            }
        }

        private static UserDetails MapUserDetails(dynamic result)
        {
            try
            {
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
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Error mapping user details", ex);
            }
        }
        public async Task<PositionAccessModel> GetModuleAccessByPosition(int positionId)
        {
            try
            {
                return await _dbContext.ExecuteDapperAsync(async connection =>
                {
                    var parameters = new { PositionId = positionId };
                    return await connection.QueryFirstOrDefaultAsync<PositionAccessModel>(
                        Queries.GetModuleAccess,
                        parameters
                    );
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting module access for position: {PositionId}", positionId);
                throw;
            }
        }

        public async Task<string> GetUserEmailByIdAsync(string userId)
        {
            try
            {
                return await _dbContext.ExecuteDapperAsync(async connection =>
                {
                    var parameters = new { UserId = userId };
                    return await connection.QueryFirstOrDefaultAsync<string>(
                        Queries.GetUserEmail,
                        parameters
                    );
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<string> GetUserPWStatus(string userId)
        {
            try
            {
                return await _dbContext.ExecuteDapperAsync(async connection =>
                {
                    var parameters = new { UserId = userId };
                    return await connection.QueryFirstOrDefaultAsync<string>(
                        Queries.GetUserPWStatus,
                        parameters
                    );
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting password status for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<string> GetUserPositionID(string userId)
        {
            try
            {
                return await _dbContext.ExecuteDapperAsync(async connection =>
                {
                    var parameters = new { UserId = userId };
                    return await connection.QueryFirstOrDefaultAsync<string>(
                        Queries.GetUserPosition,
                        parameters
                    );
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting position ID for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateUserPasswordAsync(string userId, string hashedPassword)
        {
            try
            {
                return await _dbContext.ExecuteDapperAsync(async connection =>
                {
                    var parameters = new { UserId = userId, HashedPassword = hashedPassword };
                    var result = await connection.ExecuteAsync(
                        Queries.UpdatePassword,
                        parameters
                    );
                    return result > 0;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> StandardUpdateUserPasswordAsync(string userId, string hashedPassword)
        {
            try
            {
                return await _dbContext.ExecuteDapperAsync(async connection =>
                {
                    var parameters = new { UserId = userId, HashedPassword = hashedPassword };
                    var result = await connection.ExecuteAsync(
                        Queries.StandardUpdatePassword,
                        parameters
                    );
                    return result > 0;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing standard password update for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> SetPasswordResetFlagAsync(string userId, bool resetRequired)
        {
            try
            {
                return await _dbContext.ExecuteDapperAsync(async connection =>
                {
                    var parameters = new { UserId = userId, ResetRequired = resetRequired };
                    var result = await connection.ExecuteAsync(
                        Queries.SetPasswordResetFlag,
                        parameters
                    );
                    return result > 0;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting password reset flag for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpsertUserProfilePictureAsync(string userId, string profilePicturePath)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                const string sql = @"
                INSERT INTO core.main_user_secondary_details (userid, profile_picture_path, updated_at)
                VALUES (@UserId, @ProfilePicturePath, NOW())
                ON CONFLICT (userid) DO UPDATE
                SET profile_picture_path = EXCLUDED.profile_picture_path,
                    updated_at = NOW();";

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", userId);
                parameters.Add("@ProfilePicturePath", profilePicturePath);

                var affectedRows = await connection.ExecuteAsync(sql, parameters);
                return affectedRows > 0;
            });
        }

        public async Task<string> GetUserProfilePicturePathAsync(string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                const string sql = @"SELECT profile_picture_path 
                             FROM core.main_user_secondary_details 
                             WHERE userid = @UserId";

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", userId);

                return await connection.QueryFirstOrDefaultAsync<string>(sql, parameters);
            });
        }


        public async Task<UserPositionDto?> GetUserPositionIdAndDesc(string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                const string sql = @"
            SELECT 
                a.userid AS UserId, 
                a.position_id AS PositionId, 
                b.position_desc AS PositionDesc 
            FROM core.main_user_position a  
            JOIN core.sys_user_positions_definitions b 
                ON b.position_id = a.position_id 
            WHERE a.userid = @UserId";

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", userId);

                return await connection.QueryFirstOrDefaultAsync<UserPositionDto>(sql, parameters);
            });
        }


    }
}



/* FUTURE TOKEN BASED SINGLE USE LINK PW RESET SCHEME FOR FUTURE UPDATE
 * 
 * 
 * 
 * 
 * 
 * 
 * 
// Migrated: Token-based Password Reset Flow

// 1. Controller - Step 1: Request password reset
[HttpPost("user/request-reset")]
public async Task<IActionResult> RequestReset([FromBody] ResetRequest request)
{
    var userDetails = !string.IsNullOrEmpty(request.UserId)
        ? await _userRepository.GetUserDetailsByIdAsync(request.UserId)
        : await _userRepository.GetUserDetailsByEmailAsync(request.UserEmail);

    if (userDetails == null)
        return NotFound(APIResponse<object>.Fail("User not found"));

    var token = Guid.NewGuid().ToString();
    var expiry = DateTime.UtcNow.AddHours(1);
    await _userRepository.StoreResetTokenAsync(userDetails.UserId, token, expiry);

    var resetUrl = $"https://yourfrontend.com/reset-password?token={token}";
    var body = $"Click the following link to reset your password: {resetUrl}";

    await _emailService.SendEmailAsync(userDetails.EmailAddress, "Password Reset", body);
    return Ok(APIResponse<object>.Success("Reset link sent"));
}

// 2. Controller - Step 2: Perform the actual reset
[HttpPost("user/perform-reset")]
public async Task<IActionResult> PerformReset([FromBody] ResetTokenRequest request)
{
    if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NewPassword))
        return BadRequest(APIResponse<object>.Fail("Token and new password required"));

    var tokenData = await _userRepository.GetValidResetTokenAsync(request.Token);
    if (tokenData == null)
        return BadRequest(APIResponse<object>.Fail("Invalid or expired token"));

    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
    var updateResult = await _userRepository.UpdateUserPasswordAsync(tokenData.UserId, hashedPassword);

    if (!updateResult)
        return StatusCode(500, APIResponse<object>.Fail("Error updating password"));

    await _userRepository.MarkResetTokenUsedAsync(request.Token);
    await _userRepository.SetPasswordResetFlagAsync(tokenData.UserId, false); // Clear reset flag

    return Ok(APIResponse<object>.Success("Password updated successfully"));
}

// 3. Repository Methods Implementation (PostgreSQL, Dapper-style)

public async Task StoreResetTokenAsync(string userId, string token, DateTime expiry)
{
    var sql = @"
        INSERT INTO core.user_password_reset_tokens (user_id, token, expiry)
        VALUES (@UserId, @Token, @Expiry)
    ";

    using var connection = _dbContext.CreateConnection();
    await connection.ExecuteAsync(sql, new { UserId = userId, Token = token, Expiry = expiry });
}

public async Task<ResetTokenData> GetValidResetTokenAsync(string token)
{
    var sql = @"
        SELECT user_id AS UserId FROM core.user_password_reset_tokens
        WHERE token = @Token AND expiry > NOW() AND used = FALSE
        LIMIT 1
    ";

    using var connection = _dbContext.CreateConnection();
    return await connection.QueryFirstOrDefaultAsync<ResetTokenData>(sql, new { Token = token });
}

public async Task MarkResetTokenUsedAsync(string token)
{
    var sql = @"
        UPDATE core.user_password_reset_tokens
        SET used = TRUE
        WHERE token = @Token
    ";

    using var connection = _dbContext.CreateConnection();
    await connection.ExecuteAsync(sql, new { Token = token });
}

// DTO Class
public class ResetTokenData
{
    public string UserId { get; set; }
}

// 4. SQL Table: PostgreSQL-compatible
--
-- CREATE TABLE core.user_password_reset_tokens (
--   token_id SERIAL PRIMARY KEY,
--   user_id VARCHAR(100) NOT NULL,
--   token TEXT NOT NULL,
--   expiry TIMESTAMP NOT NULL,
--   used BOOLEAN DEFAULT FALSE,
--   created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
-- );
 * 
 * 
 * 
 * 
 */