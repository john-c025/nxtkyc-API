// Controllers/Dashboard/UserDetailsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using CoreHRAPI.Data;
using CoreHRAPI.Utilities;
using CoreHRAPI.Models.User;
using CoreHRAPI.Models.Global;
using CoreHRAPI.Models.Configuration;
using System.Net.Mail;

namespace CoreHRAPI.Controllers.Dashboard
{
    [ApiController]
    [Route("api/v1/dashboard")]
    public class UserDetailsController : ControllerBase
    {
        private readonly ILogger<UserDetailsController> _logger;
        private readonly UserRepository _userRepository;
        private readonly EmailService _emailService;


        public UserDetailsController(ILogger<UserDetailsController> logger, EmailService emailService, UserRepository userRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
            _emailService = emailService;
        }

        [HttpGet("user/details")]
        public async Task<IActionResult> GetUserDetailsById([FromQuery] string userId)
        {
            var userDetails = await _userRepository.GetUserDetailsByIdAsync(userId);
            if (userDetails == null)
            {
                return NotFound();
            }
            return Ok(userDetails);
        }


        [HttpGet("user/position")]
        public async Task<IActionResult> GetUserPosition([FromQuery] string userId)
        {
            var userDetails = await _userRepository.GetUserPositionIdAndDesc(userId);
            if (userDetails == null)
            {
                return NotFound();
            }
            return Ok(userDetails);
        }


        [HttpGet("user/get-system-access-types")]
        public async Task<IActionResult> GetSystemAccessByPositionId([FromQuery] int positionId){
            var accessDetails = await _userRepository.GetModuleAccessByPosition(positionId);
            if(accessDetails == null)
            {
                return NotFound(new Response<PositionAccessModel> { message = "Access Data could not be found", data = null, status_code = 404 });
            }
            return Ok(new Response<PositionAccessModel> { message = $"Access Data found for position id {positionId}", data = accessDetails, status_code = 200 });
        }
        [HttpPost("user/reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserId) && string.IsNullOrEmpty(request.UserEmail))
                {
                    return BadRequest(APIResponse<object>.Fail("Either UserId or Email is required"));
                }

                UserDetails userDetails;

                // Check if we're using userId or email
                if (!string.IsNullOrEmpty(request.UserId))
                {
                    userDetails = await _userRepository.GetUserDetailsByIdAsync(request.UserId);
                }
                else
                {
                    userDetails = await _userRepository.GetUserDetailsByEmailAsync(request.UserEmail);
                }

                if (userDetails == null)
                {
                    return NotFound(APIResponse<object>.Fail("User not found"));
                }

                // Generate and hash new password
                var newPassword = Custom.GenerateRandomPassword();
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

                // Update password
                var updateResult = await _userRepository.UpdateUserPasswordAsync(userDetails.UserId, hashedPassword);
                if (!updateResult)
                {
                    return StatusCode(500, APIResponse<object>.Fail("Error updating password"));
                }

                // Set reset flag
                var flagResult = await _userRepository.SetPasswordResetFlagAsync(userDetails.UserId, true);
                if (!flagResult)
                {
                    return StatusCode(500, APIResponse<object>.Fail("Error setting password reset flag"));
                }

                // Send email with new password
                try
                {
                    await _emailService.SendEmailAsync(userDetails.EmailAddress, "Password Reset", $"{newPassword}");


                    _logger.LogInformation("Password reset successful for user: {UserId}, Email: {Email}",
                        userDetails.UserId, userDetails.EmailAddress);

                    return Ok(APIResponse<object>.Success("Password reset successfully and sent to email"));
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send password reset email to {Email}", userDetails.EmailAddress);
                    return StatusCode(500, APIResponse<object>.Fail("Password reset successful but failed to send email"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return StatusCode(500, APIResponse<object>.Fail("An error occurred during password reset"));
            }
        }

        [HttpPut("user/update-password")]
        public async Task<IActionResult> UpdateUserPassword([FromBody] UpdatePasswordRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.NewPassword) || string.IsNullOrEmpty(request.UserId))
                {
                    return BadRequest(APIResponse<object>.Fail("UserId and Password cannot be empty"));
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                bool updateSuccess = await _userRepository.StandardUpdateUserPasswordAsync(request.UserId, hashedPassword);

                if (!updateSuccess)
                {
                    return BadRequest(APIResponse<object>.Fail("Failed to update password"));
                }

                return Ok(APIResponse<object>.Success("Password updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password for user {UserId}", request.UserId);
                return StatusCode(500, APIResponse<object>.Fail("An error occurred while updating password", 500));
            }
        }

        [HttpGet("user/details/pw-status")]
        public async Task<IActionResult> GetUserPWStatus([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(APIResponse<object>.Fail("UserId is required"));
            }

            var userStatus = await _userRepository.GetUserPWStatus(userId);
            if (userStatus == null)
            {
                return NotFound(APIResponse<object>.Fail("User not found"));
            }

            var response = new
            {
                pw_reset_req = userStatus
            };

            return Ok(APIResponse<object>.Success("User password status retrieved", response));
        }

        [HttpPost("upload-profile-picture")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadProfilePicture([FromForm] UploadProfilePictureRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new { Message = "No file uploaded", Status = 400 });

            var allowedContentTypes = new[] { "image/jpeg", "image/png" };
            if (!allowedContentTypes.Contains(request.File.ContentType.ToLower()))
            {
                return BadRequest(new { Message = "Only JPEG and PNG files are allowed", Status = 400 });
            }

            try
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "ProfilePictures");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.File.FileName)}";
                var fullPath = Path.Combine(uploadsFolder, fileName);

                await using var stream = new FileStream(fullPath, FileMode.Create);
                await request.File.CopyToAsync(stream);

                var relativePath = Path.Combine("Uploads", "ProfilePictures", fileName).Replace("\\", "/");

                var success = await _userRepository.UpsertUserProfilePictureAsync(request.UserId, relativePath);

                if (!success)
                {
                    return StatusCode(500, new { Message = "Failed to update profile picture", Status = 500 });
                }

                return Ok(new
                {
                    Message = "Profile picture uploaded successfully",
                    Status = 200,
                    Data = new
                    {
                        UserId = request.UserId,
                        ProfilePicturePath = relativePath
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile picture for user {UserId}", request.UserId);

                return StatusCode(500, new
                {
                    Message = "Internal server error",
                    Status = 500,
                    Data = ex.Message
                });
            }
        }

        [HttpGet("user/details/profile-picture")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfilePicture([FromQuery] string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(new
                    {
                        Message = "UserId query parameter is required",
                        Status = 400
                    });
                }

                // Fetch the profile picture path for the user
                var profilePicturePath = await _userRepository.GetUserProfilePicturePathAsync(userId);

                if (string.IsNullOrEmpty(profilePicturePath))
                {
                    return NotFound(new
                    {
                        Message = "Profile picture not found",
                        Status = 404
                    });
                }

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), profilePicturePath.TrimStart('/'));

                // Check if file exists
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new
                    {
                        Message = "Profile picture not found on server",
                        Status = 404
                    });
                }

                // Serve the file as image content
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var fileExtension = Path.GetExtension(profilePicturePath).ToLower();
                var contentType = fileExtension == ".jpg" || fileExtension == ".jpeg" ? "image/jpeg" : "image/png";

                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile picture for user {UserId}", userId);
                return StatusCode(500, new
                {
                    Message = "Internal server error",
                    Status = 500,
                    Data = ex.Message
                });
            }
        }



    }

}