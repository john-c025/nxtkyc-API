using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CoreHRAPI.Models.Auth;
using System.Security.Cryptography;
using System.Text;
using CoreHRAPI.Data;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CoreHRAPI.Models.User;

namespace CoreHRAPI.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly UserCredentialsRepository _userCredentialsRepository;
        private readonly IConfiguration _config;

        public AuthController(
            ILogger<AuthController> logger,
            UserCredentialsRepository userCredentialsRepository,
            IConfiguration config)
        {
            _logger = logger;
            _userCredentialsRepository = userCredentialsRepository;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (loginDto == null || string.IsNullOrEmpty(loginDto.Username) || string.IsNullOrEmpty(loginDto.Password))
                {
                    _logger.LogWarning("Login attempt with missing credentials");
                    return BadRequest(new { Message = "Username and password are required", Code = 400 });
                }

                // Hash the username using MD5
                string codedUsername = GetMd5HashAscii(loginDto.Username);
                _logger.LogInformation("Attempting login for hashed username");

                // Get user from repository
                var user = await _userCredentialsRepository.GetUserByCodedUsernameAsync(codedUsername);

                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found");
                    return Unauthorized(new { Message = "Login Failed! Check your credentials!", Code = 401 });
                }

                // Verify password exists
                if (string.IsNullOrEmpty(user.codedpword))
                {
                    _logger.LogError("User found but password is null or empty");
                    return StatusCode(500, new { Message = "Internal server error", Code = 500 });
                }

                // Verify the password using BCrypt
                bool isPasswordValid;
                try
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.codedpword);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during password verification");
                    return StatusCode(500, new { Message = "Internal server error", Code = 500 });
                }

                if (!isPasswordValid)
                {
                    _logger.LogWarning("Login failed: Invalid password");
                    return Unauthorized(new { Message = "Login Failed! Check your credentials!", Code = 401 });
                }

                // Generate JWT token
                string token;
                try
                {
                    token = GenerateJSONWebToken(user);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating JWT token");
                    return StatusCode(500, new { Message = "Error generating authentication token", Code = 500 });
                }

                _logger.LogInformation("Login successful for user ID: {UserId}", user.userid);
                return Ok(new
                {
                    Message = "Login successful",
                    UserId = user.userid,
                    Token = token,
                    Code = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during login attempt");
                return StatusCode(500, new { Message = "Internal server error", Code = 500 });
            }
        }


        private string GenerateJSONWebToken(UserCredentials user)
        {
            try
            {
                var jwtKey = _config["Jwt:Key"];
                _logger.LogInformation($"JWT Key from config: {jwtKey}");
                _logger.LogInformation($"JWT Key length: {jwtKey?.Length}");
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.codedusername),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("UserId", user.userid)
                };

                var token = new JwtSecurityToken(
                    issuer: _config["Jwt:Issuer"],
                    audience: _config["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(120),
                    signingCredentials: credentials);

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token");
                throw;
            }
        }
        //fc46ac7ac81f4746adcaa50165611ec6
        [HttpPost("encrypt-string")]
        public IActionResult TestMd5([FromBody] string input)
        {
            try
            {
                // Original ASCII implementation
                string asciiHash = GetMd5HashAscii(input);

                // UTF8 implementation (PostgreSQL style)
                string utf8Hash = GetMd5HashUtf8(input);

                return Ok(new
                {
                    Input = input,
                    AsciiHash = asciiHash,
                    Utf8Hash = utf8Hash,
                    Message = "MD5 hash generated successfully",
                    Code = 200
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "Error generating MD5 hash",
                    Error = ex.Message,
                    Code = 400
                });
            }
        }

        private string GetMd5HashAscii(string input)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.ASCII.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private string GetMd5HashUtf8(string input)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);
                return string.Concat(hashBytes.Select(x => x.ToString("x2")));
            }
        }
        private string GetMd5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.ASCII.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // For now, just return success as token invalidation happens client-side
            return Ok(new { Message = "Successfully logged out", Code = 200 });
        }

        //[HttpPost("forgot-password")]
        //public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(forgotPasswordDto.Email))
        //        {
        //            return BadRequest(new { Message = "Email is required", Code = 400 });
        //        }

        //        // TODO: Implement password reset logic
        //        return Ok(new
        //        {
        //            Message = "If an account exists with this email, you will receive password reset instructions",
        //            Code = 200
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error processing forgot password request");
        //        return StatusCode(500, new { Message = "Internal server error", Code = 500 });
        //    }
        //}


        [HttpGet("check-user-status")]
        public async Task<IActionResult> CheckUserStatus([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest(new
                    {
                        Message = "Email address is required",
                        Status = 400,
                        Data = (object)null
                    });
                }

                var result = await _userCredentialsRepository.CheckIfUserIsActive(email);

                if (result == null)
                {
                    return NotFound(new
                    {
                        Message = "User not found",
                        Status = 404,
                        Data = new { Email = email }
                    });
                }

                return Ok(new
                {
                    Message = "User status retrieved successfully",
                    Status = 200,
                    Data = new
                    {
                        IsActive = result.is_user_active,
                        UserId = result.userid,
                        EmailAddress = result.email_address
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user status for email: {Email}", email);

                return StatusCode(500, new
                {
                    Message = "Failed to check user status",
                    Status = 500,
                    Data = ex.Message
                });
            }
        }
    }
}