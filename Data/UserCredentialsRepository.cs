using Dapper;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using KYCAPI.Models.User;

namespace KYCAPI.Data
{
    public class UserCredentialsRepository
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<UserCredentialsRepository> _logger;

        private static class Queries
        {
            public const string GetUserByCodedUsername = @"
                SELECT 
                    user_id as userid,
                    coded_id as codedid,
                    coded_username as codedusername,
                    coded_password as codedpword,
                    status
                FROM dbo.sys_user_credentials 
                WHERE coded_username = @CodedUsername";

            public const string CheckUserIfActive = @"SELECT is_active AS is_user_active, userid, email FROM dbo.sys_users WHERE email = @Email";
        }

        public UserCredentialsRepository(ILogger<UserCredentialsRepository> logger, DatabaseContext dbContext)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<UserCredentials> GetUserByCodedUsernameAsync(string codedUsername)
        {
            try
            {
                _logger.LogInformation("Attempting to fetch user credentials for coded username: {CodedUsername}", codedUsername);

                return await _dbContext.ExecuteDapperAsync(async connection =>
                {
                    var result = await connection.QueryFirstOrDefaultAsync<UserCredentials>(
                        Queries.GetUserByCodedUsername,
                        new { CodedUsername = codedUsername }
                    );

                    if (result == null)
                    {
                        _logger.LogWarning("No user found for coded username: {CodedUsername}", codedUsername);
                    }

                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user credentials for coded username: {CodedUsername}", codedUsername);
                throw;
            }
        }

        public async Task<UserStatusCheck> CheckIfUserIsActive(string email)
        {
            try
            {
                _logger.LogInformation("Attempting to fetch user credentials status for account:", email);

                return await _dbContext.ExecuteDapperAsync(async connection =>
                {
                    var result = await connection.QueryFirstOrDefaultAsync<UserStatusCheck>(
                        Queries.CheckUserIfActive,
                        new { Email = email }
                    );

                    if (result == null)
                    {
                        _logger.LogWarning("No user found for email: {Email}", email);
                    }

                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user credentials for email: {Email}", email);
                throw;
            }
        }
    }
}

    