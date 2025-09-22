using Dapper;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using CoreHRAPI.Models.User;

namespace CoreHRAPI.Data
{
    public class UserCredentialsRepository
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<UserCredentialsRepository> _logger;

        private static class Queries
        {
            public const string GetUserByCodedUsername = @"
                SELECT 
                    userid,
                    codedid,
                    codedusername,
                    codedpword,
                    status
                FROM core.main_user_creds 
                WHERE codedusername = @CodedUsername";

            public const string CheckUserIfActive = @"SELECT u.status AS is_user_active, up.userid, up.email_address FROM core.main_user_primary_details u JOIN core.main_user_contact_details up ON up.userid = u.userid WHERE up.email_address = @Email";
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

    