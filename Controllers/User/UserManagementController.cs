// Controllers/Dashboard/UserDetailsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using CoreHRAPI.Data;
using CoreHRAPI.Utilities;
using CoreHRAPI.Models.User;
using CoreHRAPI.Models.Configuration;
using CoreHRAPI.Models.Global;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoreHRAPI.Controllers.Dashboard
{
    [ApiController]
    [Route("api/v1/uam")]
        public class UserManagementController : ControllerBase
        {
            private readonly ILogger<UserDetailsController> _logger;
            private readonly UAMRepository _uamRepository;
            private readonly EmailService _emailService;


            public UserManagementController(ILogger<UserDetailsController> logger, EmailService emailService, UAMRepository uamRepository)
            {
                _logger = logger;
                _uamRepository = uamRepository;
                _emailService = emailService;
            }

            [HttpGet("management/user/details")]
            public async Task<IActionResult> GetUserDetailsById([FromQuery] string userId)
            {
                var userDetails = await _uamRepository.GetUserDetailsByIdAsync(userId);
                if (userDetails == null)
                {
                    return NotFound();
                }
                return Ok(userDetails);
        }

        [HttpGet("management/load-system-users")]
        public async Task<IActionResult> LoadActiveBorrowers([FromQuery] bool isactive)
        {
            try
            {
                // Assuming you have a repository to fetch the master list

                var sys_usrs = await _uamRepository.GetSystemUserList(isactive);

                if (sys_usrs == null || !sys_usrs.Any())
                {
                    return NotFound("No system user list data found");
                }


                return Ok(new
                {
                    Message = "System user list data  loaded successfully",
                    Status = 200,
                    Data = sys_usrs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading System Users List");
                return BadRequest(new
                {
                    Message = "System user list data failed to load",
                    Status = 500,
                    Data = "Error"
                });
            }
        }

        [HttpPost("management/register-user")]
            public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest request)
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        return BadRequest(new
                        {
                            Message = "Invalid request data",
                            Status = 400,
                            Data = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                        });
                    }

                    // Generate a random password and hash it
                    var plainPassword = Custom.GenerateRandomPassword();
                    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);
                    request.CodedPassword = hashedPassword;

                    // Call stored procedure and map result
                    var result = await _uamRepository.RegisterUserAsync(request);

                    if (result == null)
                    {
                        return BadRequest(new
                        {
                            Message = "Failed to register user",
                            Status = 500,
                            Data = "No response from database"
                        });
                    }

                    if (result.Message.Contains("already exists"))
                    {
                        return Conflict(new
                        {
                            Message = result.Message,
                            Status = 409,
                            Data = new { Email = request.EmailAddress }
                        });
                    }

                    // Send welcome email with the plain password
                    try
                    {
                        await _emailService.SendWelcomeEMailAsync(
                            request.EmailAddress,
                            $"Welcome to the CORE HR System! {request.FirstName}",
                             $"Thank you {request.FirstName}, your System ID is {result.GeneratedUserId}" // Send plain password in email
                        );
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Failed to send welcome email to {Email}", request.EmailAddress);
                    }

                    return Ok(new
                    {
                        Message = result.Message,
                        Status = 200,
                        Data = new RegisterUserRequestResponse
                        {
                            GeneratedUserId = result.GeneratedUserId,
                            CodedId = result.CodedId,
                            CodedUsername = result.CodedUsername,
                            Message = result.Message + $" DEBUG: PASS IS {plainPassword}" // DEBUG REMOVE THIS LATER
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error registering new user: {FirstName} {Surname}", request.FirstName, request.Surname);

                    return BadRequest(new
                    {
                        Message = "Failed to register user",
                        Status = 500,
                        Data = ex.Message
                    });
                }
            }
        [HttpGet("management/load-positions")]
        public async Task<IActionResult> LoadPositions()
        {
            try
            {
                var positions = await _uamRepository.GetPositions();

                if (positions == null || !positions.Any())
                {
                    return NotFound("No positions data found");
                }

                return Ok(new
                {
                    Message = "Positions data loaded successfully",
                    Status = 200,
                    Data = positions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Positions");
                return BadRequest(new
                {
                    Message = "Positions data failed to load",
                    Status = 500,
                    Data = "Error"
                });
            }
        }

        [HttpGet("management/load-branches")]
        public async Task<IActionResult> LoadBranches()
        {
            try
            {
                var branches = await _uamRepository.GetBranches();

                if (branches == null || !branches.Any())
                {
                    return NotFound("No branches data found");
                }

                return Ok(new
                {
                    Message = "Branches data loaded successfully",
                    Status = 200,
                    Data = branches
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Branches");
                return BadRequest(new
                {
                    Message = "Branches data failed to load",
                    Status = 500,
                    Data = "Error"
                });
            }
        }

        [HttpGet("management/load-companies")]
        public async Task<IActionResult> LoadCompanies()
        {
            try
            {
                var companies = await _uamRepository.GetCompanies();

                if (companies == null || !companies.Any())
                {
                    return NotFound("No company data found");
                }

                return Ok(new
                {
                    Message = "Company data loaded successfully",
                    Status = 200,
                    Data = companies
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Companies");
                return BadRequest(new
                {
                    Message = "Company data failed to load",
                    Status = 500,
                    Data = "Error"
                });
            }
        }

        [HttpPut("management/activate-user")]
        public async Task<IActionResult> ActivateUser([FromBody] DeactivateUserRequest request, [FromQuery] string employeeid)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.UserId))
                {
                    return BadRequest(new
                    {
                        Message = "User ID is required",
                        Status = 400,
                        Data = "Invalid user ID"
                    });
                }

                var result = await _uamRepository.SetUserActiveAsync(request.UserId, employeeid);

                if (!result)
                {
                    return NotFound(new
                    {
                        Message = "Failed to activate user",
                        Status = 404,
                        Data = "User not found or already active"
                    });
                }

                return Ok(new
                {
                    Message = "User activated successfully",
                    Status = 200,
                    Data = new { UserId = request.UserId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user: {UserId}", request.UserId);

                return StatusCode(500, new
                {
                    Message = "Failed to activate user",
                    Status = 500,
                    Data = ex.Message
                });
            }
        }


        [HttpPut("management/deactivate-user")]
        public async Task<IActionResult> DeactivateUser([FromBody] DeactivateUserRequest request, [FromQuery] string employeeid)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.UserId))
                {
                    return BadRequest(new
                    {
                        Message = "User ID is required",
                        Status = 400,
                        Data = "Invalid user ID"
                    });
                }

                var result = await _uamRepository.SetUserInactiveAsync(request.UserId, employeeid);

                if (!result)
                {
                    return NotFound(new
                    {
                        Message = "Failed to deactivate user",
                        Status = 404,
                        Data = "User not found or already inactive"
                    });
                }

                return Ok(new
                {
                    Message = "User deactivated successfully",
                    Status = 200,
                    Data = new { UserId = request.UserId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user: {UserId}", request.UserId);

                return StatusCode(500, new
                {
                    Message = "Failed to deactivate user",
                    Status = 500,
                    Data = ex.Message
                });
            }
        }


        public interface IEmailService
        {
            Task SendWelcomeEmailAsync(string email, string firstName, string userId, string password);
        }



    }

}