using System.Text.Json;
using KYCAPI.Data;
using KYCAPI.Models.Configuration;
using KYCAPI.Models.Global;
using KYCAPI.Models.User;
using KYCAPI.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace KYCAPI.Controllers.Global
{
    [ApiController]
    [Route("api/v1/global")]
    public class GlobalController : ControllerBase
    {
        private readonly ILogger<GlobalController> _logger;
        private readonly GlobalRepository _globalRepository;
        private readonly EmailService _emailService;
        public GlobalController(ILogger<GlobalController> logger, EmailService emailService, GlobalRepository globalRepository)
        {
            _logger = logger;
            _globalRepository = globalRepository;
            _emailService = emailService;
        }

        // --------------------------------------------------------------- NOTIFICATIONS ----------------------------------------- //

        [HttpPost("/notifications/send-notification")]
        public async Task<IActionResult> SendNotificationBase(string userId, int actionType, bool isUnique, string notificationDesc)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(APIResponse<object>.Fail("User ID is required"));
            }

            if (actionType <= 0)
            {
                return BadRequest(APIResponse<object>.Fail("Valid Action Type is required"));
            }

            try
            {
                var insertResult = await _globalRepository.InsertNewNotification(userId, actionType, isUnique, notificationDesc);

                if (!insertResult)
                {
                    return StatusCode(500, APIResponse<object>.Fail("Error inserting notification"));
                }

                _logger.LogInformation($"Notification inserted for user {userId}");

                return Ok(APIResponse<object>.Success("Notification sent successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending notification");
                return StatusCode(500, APIResponse<object>.Fail("An error occurred while processing your request"));
            }
        }

        [HttpPost("notifications/send-reset-password")]
        public async Task<IActionResult> SendNotification(string userId)
        {
            if (userId == null)
            {
                return BadRequest(APIResponse<object>.Fail("User Id is required"));
            }

            return await SendNotificationBase(
                userId,
                2,
                false,
                "Password has been Reset, Contact support if you did not request this transaction"
            );
        }

        [HttpPost("notifications/send-update-request-notification")]
        public async Task<IActionResult> SendNotificationUpdate(string userId)
        {
            if (userId == null)
            {
                return BadRequest(APIResponse<object>.Fail("User Id is required"));
            }

            return await SendNotificationBase(
                userId,
                5,
                false,
                "You have sent an DATA UPDATE request, Contact support if you did not request this transaction"
            );
        }

        [HttpPost("notifications/send-admin-update-request")]
        public async Task<IActionResult> SendNotificationAdminUpdateRequest(string userId)
        {
            if (userId == null)
            {
                return BadRequest(APIResponse<object>.Fail("User Id is required"));
            }

            return await SendNotificationBase(
                userId,
                6,
                false,
                "User "+ userId + " has sent a DATA UPDATE request! Check your System Requests"
            );
        }

        [HttpPost("/notifications/send-update-password")]
        public async Task<IActionResult> SendUpdateNotification(string userId)
        {
            if (userId == null)
            {
                return BadRequest(APIResponse<object>.Fail("User Id is required"));
            }

            return await SendNotificationBase(
                userId,
                1,
                false,
                "Your Password has been Recently Updated, Contact support if you did not initiate this transaction"
            );
        }

        [HttpPost("/notifications/send-upload-masterlist")]
        public async Task<IActionResult> SendUploadNotification(string userId)
        {
            if (userId == null)
            {
                return BadRequest(APIResponse<object>.Fail("User Id is required"));
            }

            return await SendNotificationBase(
                userId,
                3,
                false,
                "You have recently uploaded a new masterlist file, Contact support if you did not initiate this transaction"
            );
        }

        [HttpPost("/notifications/send-upload-request")]
        public async Task<IActionResult> SendUploadRequestNotification(string requestorID, string userId)
        {
            if (userId == null)
            {
                return BadRequest(APIResponse<object>.Fail("Recipient user id is required"));
            }

            return await SendNotificationBase(
                userId,
                3,
                false,
                $"User {requestorID} Has Requested to Reupload / Edit a masterlist instance"
            );
        }


        [HttpGet("load-user-notifications")]
        public async Task<IActionResult> LoadCollectionCompanies([FromQuery] string userId)
        {
            try
            {
                // Assuming you have a repository to fetch the master list

                var notifications = await _globalRepository.GetUserNotifications(userId);

                if (notifications == null || !notifications.Any())
                {
                    return NotFound("No user notification data found");
                }


                return Ok(new
                {
                    Message = "User notification data loaded successfully",
                    Status = 200,
                    Data = notifications
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading notifications list");
                return StatusCode(500, "Error loading the notifications list");
            }
        }

        [HttpPost("notifications/update-read")]
        public async Task<IActionResult> UpdateUserPassword([FromBody] UpdateReadModel request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserId) || request.notifId == null)
                {
                    return BadRequest(APIResponse<object>.Fail("UserId and Notif ID cannot be empty"));
                }

               
                bool updateSuccess = await _globalRepository.UpdateToReadNotification(request.UserId, request.notifId);

                if (!updateSuccess)
                {
                    return BadRequest(APIResponse<object>.Fail("Failed to update notification read status"));
                }

                return Ok(APIResponse<object>.Success("Notification status updated successfully!"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notication status for user {UserId}", request.UserId);
                return StatusCode(500, APIResponse<object>.Fail("An error occurred while updating notification stattus", 500));
            }
        }






        // --------------------------------------------------------------- OTHER GLOBAL CONTROLLER ----------------------------------------- //

        [HttpGet("load-sytem-status")]
        public async Task<IActionResult> LoadSystemStatus()
        {
            try
            {
                // Assuming you have a repository to fetch the master list

                var status_list = await _globalRepository.GetSystemStatus();

                if (status_list == null || !status_list.Any())
                {
                    return NotFound("No status data found");
                }


                return Ok(new
                {
                    Message = "System Status data loaded successfully",
                    Status = 200,
                    Data = status_list
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading status_list");
                return StatusCode(500, "Error loading status_list");
            }
        }






        // --------------------------------------------------------------- SUBMIT REQUEST API HERE ----------------------------------------- //
        [HttpPost("submit-request")]
        public async Task<IActionResult> SubmitRequest(
        [FromQuery] string initiatorId,
        [FromQuery] long requestType,
        [FromBody] JsonElement payload)  // Use JsonElement from System.Text.Json
        {
            try
            {
                // Convert JsonElement to JObject
                var jObject = JObject.Parse(payload.ToString());
                var messageJson = jObject.ToString(Formatting.None);
                var refNo = await _globalRepository.InsertRequestAsync(initiatorId, requestType, messageJson);

                if (string.IsNullOrEmpty(refNo))
                    return StatusCode(500, "Failed to submit request");

                return Ok(new { Message = "Request submitted for approval", RefNo = refNo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting request");
                return StatusCode(500, "Internal server error");
            }
        }






        [HttpGet("admin/get-pending-requests")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var requests = await _globalRepository.GetPendingRequestsAsync();
            return Ok(requests);
        }

        [HttpGet("admin/get-all-requests")]
        public async Task<IActionResult> GetAllRequests()
        {
            var requests = await _globalRepository.GetAllRequestsAsync();
            return Ok(requests);
        }


        [HttpGet("can-upload")]
        public async Task<IActionResult> CanUpload([FromQuery] string userId)
        {
            var lastUpload = await _globalRepository.GetLatestUploadAsync(userId);

            if (lastUpload == null || lastUpload.uploaded_at.AddMonths(1) <= DateTime.UtcNow)
            {
                return Ok(new { canUpload = true, reason = "No upload this month yet" });
            }

            var approvedRequest = await _globalRepository.GetValidApprovedRequestAsync(userId, lastUpload.uploaded_at);
            if (approvedRequest != null)
            {
                return Ok(new
                {
                    canUpload = true,
                    reason = "Approved reupload request",
                    requestId = approvedRequest.autoid,
                    refno = approvedRequest.refno
                });
            }

            return Ok(new { canUpload = false, reason = "Access to Masterlist upload is locked. Request access." });
        }




        [HttpPut("global/dashboard-config/update")]
        public async Task<IActionResult> UpdateDashboardConfig([FromBody] DashboardConfigRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new DashboardConfigResponse
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                // Check if this is a company-wide update
                if (request.IsCompanyWideUpdate == true)
                {
                    return await UpdateCompanyWideConfig(request);
                }

                // Individual user update
                return await UpdateIndividualUserConfig(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dashboard config for user {UserId} and company {CompanyId}",
                    request.UserId, request.CompanyId);
                return StatusCode(500, new DashboardConfigResponse
                {
                    Success = false,
                    Message = "Internal server error occurred"
                });
            }
        }

        private async Task<IActionResult> UpdateIndividualUserConfig(DashboardConfigRequest request)
        {
            var existingConfig = await _globalRepository.GetDashboardConfig(request.UserId, request.CompanyId);
            if (existingConfig == null)
            {
                return NotFound(new DashboardConfigResponse
                {
                    Success = false,
                    Message = "Dashboard configuration not found"
                });
            }

            existingConfig.DashboardConfigData = request.DashboardConfigData;
            existingConfig.Version = IncrementVersion(existingConfig.Version);

            var result = await _globalRepository.UpdateDashboardConfig(existingConfig);

            return Ok(new DashboardConfigResponse
            {
                Success = true,
                Message = "User dashboard configuration updated successfully",
                Data = result
            });
        }

        private async Task<IActionResult> UpdateCompanyWideConfig(DashboardConfigRequest request)
        {
            // Get all existing configs for the company
            var existingConfigs = await _globalRepository.GetDashboardConfigsByCompany(request.CompanyId);

            if (!existingConfigs.Any())
            {
                return NotFound(new DashboardConfigResponse
                {
                    Success = false,
                    Message = "No dashboard configurations found for this company"
                });
            }

            var updatedConfigs = new List<DashboardConfig>();

            foreach (var existingConfig in existingConfigs)
            {
                // Merge company-wide settings with existing user settings
                var mergedConfig = MergeCompanyWideSettings(existingConfig, request.DashboardConfigData);
                mergedConfig.Version = IncrementVersion(existingConfig.Version);

                var result = await _globalRepository.UpdateDashboardConfig(mergedConfig);
                updatedConfigs.Add(result);
            }

            return Ok(new DashboardConfigsResponse
            {
                Success = true,
                Message = $"Company dashboard configuration updated successfully for {updatedConfigs.Count} users",
                Data = updatedConfigs
            });
        }
        private DashboardConfig MergeCompanyWideSettings(DashboardConfig existingConfig, DashboardConfigData companySettings)
        {
            // Create a deep copy using JSON serialization
            var jsonString = JsonConvert.SerializeObject(existingConfig);
            var mergedConfig = JsonConvert.DeserializeObject<DashboardConfig>(jsonString);

            // Now you can safely modify mergedConfig without affecting the original
            if (companySettings.Preferences != null)
            {
                mergedConfig.DashboardConfigData.Preferences = companySettings.Preferences;
            }

            if (companySettings.WidgetSettings != null)
            {
                mergedConfig.DashboardConfigData.WidgetSettings = companySettings.WidgetSettings;
            }

            // Rest of your merging logic...
            return mergedConfig;
        }
        private string IncrementVersion(string currentVersion)
        {
            if (string.IsNullOrEmpty(currentVersion) || !currentVersion.Contains('.'))
                return "1.0";

            var parts = currentVersion.Split('.');
            if (parts.Length == 2 && int.TryParse(parts[1], out int minor))
            {
                return $"{parts[0]}.{minor + 1}";
            }

            return "1.0";
        }







    }
}
