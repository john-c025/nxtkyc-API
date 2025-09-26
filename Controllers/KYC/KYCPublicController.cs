using Microsoft.AspNetCore.Mvc;
using KYCAPI.Data;
using KYCAPI.Models.KYC;
using KYCAPI.Models.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace KYCAPI.Controllers.KYC
{
    [ApiController]
    [Route("api/v1/public/kyc")]
    public class KYCPublicController : ControllerBase
    {
        private readonly KYCRepository _kycRepository;
        private readonly ILogger<KYCPublicController> _logger;
        private readonly IConfiguration _configuration;

        public KYCPublicController(KYCRepository kycRepository, ILogger<KYCPublicController> logger, IConfiguration configuration)
        {
            _kycRepository = kycRepository;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Submit KYC request using access token (Public endpoint for clients)
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitKYCRequest([FromForm] SubmitKYCRequestDto requestDto)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(requestDto.access_token) || string.IsNullOrEmpty(requestDto.account_code))
                {
                    return BadRequest(new APIResponse { Success = false, Message = "Access token and account code are required" });
                }

                // Validate and consume token
                var tokenValid = await _kycRepository.ValidateAndConsumeTokenAsync(requestDto.access_token, requestDto.account_code);
                if (!tokenValid)
                {
                    return Unauthorized(new APIResponse { Success = false, Message = "Invalid or expired access token" });
                }

                // Create KYC request
                var createRequestDto = new CreateKYCRequestDto
                {
                    account_code = requestDto.account_code,
                    request_type = requestDto.request_type ?? "Level Upgrade",
                    priority_level = requestDto.priority_level,
                    request_description = requestDto.request_description,
                    level_to_upgrade_to = requestDto.level_to_upgrade_to,
                    has_files = requestDto.files?.Any() == true
                };

                var kycRequestId = await _kycRepository.CreateKYCRequestAsync(createRequestDto, "SYSTEM_PUBLIC");

                // Handle file uploads if any
                var uploadedFiles = new List<object>();
                if (requestDto.files?.Any() == true)
                {
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "KYC", "Public");
                    
                    // Create directory if it doesn't exist
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    foreach (var file in requestDto.files)
                    {
                        if (file.Length > 0)
                        {
                            // Validate file size (max 10MB)
                            if (file.Length > 10 * 1024 * 1024)
                            {
                                return BadRequest(new APIResponse { Success = false, Message = $"File {file.FileName} exceeds maximum size of 10MB" });
                            }

                            // Validate file extension
                            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".docx", ".doc" };
                            var fileExtension = Path.GetExtension(file.FileName).ToLower();
                            if (!allowedExtensions.Contains(fileExtension))
                            {
                                return BadRequest(new APIResponse { Success = false, Message = $"File type {fileExtension} is not allowed" });
                            }

                            var fileName = $"{kycRequestId}_{Guid.NewGuid()}{fileExtension}";
                            var filePath = Path.Combine(uploadsPath, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var mediaFile = new KYCMediaFileModel
                            {
                                kyc_request_id = kycRequestId,
                                file_name = fileName,
                                file_original_name = file.FileName,
                                file_type = 1, // Document type
                                file_extension = fileExtension,
                                file_size = file.Length,
                                file_path = filePath,
                                file_url = $"/uploads/kyc/public/{fileName}",
                                mime_type = file.ContentType,
                                file_category = 1, // General category
                                file_description = requestDto.file_description,
                                uploaded_by = "CLIENT_PUBLIC"
                            };

                            var fileId = await _kycRepository.SaveKYCMediaFileAsync(mediaFile);
                            uploadedFiles.Add(new { file_id = fileId, file_name = fileName, original_name = file.FileName });
                        }
                    }
                }

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "KYC request submitted successfully",
                    Data = new 
                    { 
                        kyc_request_id = kycRequestId,
                        status = "Submitted",
                        uploaded_files_count = uploadedFiles.Count,
                        uploaded_files = uploadedFiles
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new APIResponse { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting public KYC request");
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error while processing your request" });
            }
        }

        /// <summary>
        /// Check KYC request status using request ID (Public endpoint - No auth required)
        /// </summary>
        [HttpGet("status/{kycRequestId}")]
        public async Task<IActionResult> CheckKYCStatus(string kycRequestId)
        {
            try
            {
                var request = await _kycRepository.GetKYCRequestDetailedAsync(kycRequestId);
                if (request == null)
                {
                    return NotFound(new APIResponse { Success = false, Message = "KYC request not found" });
                }

                // Return limited information for public access
                var publicResponse = new
                {
                    kyc_request_id = request.kyc_request_id,
                    request_type = request.request_type,
                    request_status = request.request_status,
                    request_status_name = request.request_status_name,
                    priority_level = request.priority_level,
                    priority_level_name = request.priority_level_name,
                    submitted_at = request.submitted_at,
                    completed_at = request.completed_at,
                    has_files = request.has_files,
                    current_level = request.current_level,
                    level_to_upgrade_to = request.level_to_upgrade_to,
                    // Only show latest status update, not full audit trail
                    latest_update = request.audit_trail?.FirstOrDefault()?.action_timestamp,
                    estimated_processing_time = GetEstimatedProcessingTime(request.priority_level)
                };

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "KYC request status retrieved successfully",
                    Data = publicResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking KYC status for request: {KYCRequestId}", kycRequestId);
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get available privilege levels for upgrade (Public endpoint - No auth required)
        /// </summary>
        [HttpGet("privilege-levels/{companyId}")]
        public async Task<IActionResult> GetAvailablePrivilegeLevels(int companyId)
        {
            try
            {
                // Get privileges from database
                var privileges = await _kycRepository.GetKYCPrivilegesAsync(companyId);
                
                var privilegeLevels = privileges.Select(p => new
                {
                    level = p.privilege_level,
                    name = p.privilege_name,
                    description = p.privilege_description,
                    services = GetServicesFromJson(p.privileges_json),
                    limits = GetLimitsFromJson(p.privileges_json),
                    requirements = GetRequirementsFromJson(p.privileges_json),
                    is_active = p.is_active
                }).OrderBy(p => p.level);

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "Available privilege levels retrieved successfully",
                    Data = privilegeLevels
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving privilege levels for company: {CompanyId}", companyId);
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Validate access token for client pages (Public endpoint - No auth required)
        /// </summary>
        [HttpPost("tokens/validate")]
        public async Task<IActionResult> ValidateAccessToken([FromBody] ValidateTokenDto validateDto)
        {
            try
            {
                var validationResult = await _kycRepository.ValidateTokenAsync(validateDto.token, validateDto.account_code);
                
                if (validationResult == null)
                {
                    return Ok(new APIResponse
                    {
                        Success = true,
                        Message = "Token is invalid or expired",
                        Data = new { is_valid = false }
                    });
                }

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "Token is valid",
                    Data = new
                    {
                        is_valid = validationResult.IsValid,
                        account_code = validationResult.AccountCode,
                        current_privilege_level = validationResult.CurrentPrivilegeLevel,
                        company_name = validationResult.CompanyName,
                        company_code = validationResult.CompanyCode,
                        expires_at = validationResult.ExpiresAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating access token for account: {AccountCode}", validateDto.account_code);
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get company information by account code (Public endpoint - No auth required)
        /// </summary>
        [HttpGet("company-by-account")]
        public async Task<IActionResult> GetCompanyByAccountCode([FromQuery] string account_code)
        {
            try
            {
                if (string.IsNullOrEmpty(account_code))
                {
                    return BadRequest(new APIResponse { Success = false, Message = "Account code is required" });
                }

                var companyInfo = await _kycRepository.GetCompanyByAccountCodeAsync(account_code);
                
                if (companyInfo == null)
                {
                    return NotFound(new APIResponse { Success = false, Message = "Account code not found" });
                }

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "Company information retrieved successfully",
                    Data = companyInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company information for account: {AccountCode}", account_code);
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get file categories for file management (Public endpoint - No auth required)
        /// </summary>
        [HttpGet("files/categories")]
        public async Task<IActionResult> GetFileCategories()
        {
            try
            {
                var categories = await _kycRepository.GetFileCategoriesAsync();

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "File categories retrieved successfully",
                    Data = categories
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file categories");
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Check if account exists and if account origin number is unique (Public endpoint - No auth required)
        /// </summary>
        [HttpGet("check-account")]
        public async Task<IActionResult> CheckAccountExists([FromQuery] string account_code)
        {
            try
            {
                if (string.IsNullOrEmpty(account_code))
                {
                    return BadRequest(new APIResponse { Success = false, Message = "Account code is required" });
                }

                var result = await _kycRepository.CheckAccountAndOriginExistsAsync(account_code);

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = result.AccountExists ? "Account exists" : "Account not found",
                    Data = new
                    {
                        account_code = account_code,
                        account_exists = result.AccountExists,
                        account_origin_number = result.AccountOriginNumber,
                        origin_number_unique = result.OriginNumberUnique,
                        company_id = result.CompanyId,
                        message = result.AccountExists 
                            ? (result.OriginNumberUnique 
                                ? "Account exists and origin number is unique" 
                                : "Account exists but origin number has duplicates")
                            : "Account not found"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking account existence for account code: {AccountCode}", account_code);
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get submission requirements for KYC (Public endpoint - No auth required)
        /// </summary>
        [HttpGet("requirements")]
        public IActionResult GetSubmissionRequirements()
        {
            try
            {
                var requirements = new
                {
                    file_requirements = new
                    {
                        max_file_size_mb = 10,
                        allowed_extensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".docx", ".doc" },
                        max_files_per_request = 5,
                        recommended_files = new[]
                        {
                            "Valid government-issued ID",
                            "Proof of address",
                            "Income verification documents",
                            "Bank statements (if applicable)"
                        }
                    },
                    request_types = new[]
                    {
                        "Level Upgrade",
                        "Account Verification",
                        "Document Update",
                        "Special Access Request"
                    },
                    processing_times = new
                    {
                        low_priority = "5-7 business days",
                        medium_priority = "3-5 business days",
                        high_priority = "1-3 business days",
                        urgent_priority = "Within 24 hours"
                    }
                };

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "Submission requirements retrieved successfully",
                    Data = requirements
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving submission requirements");
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        #region Helper Methods

        private string GetEstimatedProcessingTime(byte priorityLevel)
        {
            return priorityLevel switch
            {
                1 => "5-7 business days",
                2 => "3-5 business days",
                3 => "1-3 business days",
                4 => "Within 24 hours",
                _ => "3-5 business days"
            };
        }

        private List<string> GetServicesFromJson(string? privilegesJson)
        {
            if (string.IsNullOrEmpty(privilegesJson))
                return new List<string>();

            try
            {
                var json = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(privilegesJson);
                if (json != null && json.ContainsKey("services"))
                {
                    var services = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json["services"].ToString() ?? "[]");
                    return services ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing services from privileges JSON: {Json}", privilegesJson);
            }

            return new List<string>();
        }

        private Dictionary<string, object> GetLimitsFromJson(string? privilegesJson)
        {
            if (string.IsNullOrEmpty(privilegesJson))
                return new Dictionary<string, object>();

            try
            {
                var json = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(privilegesJson);
                if (json != null && json.ContainsKey("limits"))
                {
                    var limits = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json["limits"].ToString() ?? "{}");
                    return limits ?? new Dictionary<string, object>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing limits from privileges JSON: {Json}", privilegesJson);
            }

            return new Dictionary<string, object>();
        }

        private List<object> GetRequirementsFromJson(string? privilegesJson)
        {
            if (string.IsNullOrEmpty(privilegesJson))
                return new List<object>();

            try
            {
                var json = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(privilegesJson);
                if (json != null && json.ContainsKey("requirements"))
                {
                    var requirements = System.Text.Json.JsonSerializer.Deserialize<List<object>>(json["requirements"].ToString() ?? "[]");
                    return requirements ?? new List<object>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing requirements from privileges JSON: {Json}", privilegesJson);
            }

            return new List<object>();
        }

        #endregion
    }

    /// <summary>
    /// DTO for public KYC request submission
    /// </summary>
    public class SubmitKYCRequestDto
    {
        [Required]
        public string access_token { get; set; }
        
        [Required]
        public string account_code { get; set; }
        
        public string? request_type { get; set; } = "Level Upgrade";
        
        public byte priority_level { get; set; } = 2;
        
        public string? request_description { get; set; }
        
        public byte level_to_upgrade_to { get; set; } = 1;
        
        public List<IFormFile>? files { get; set; }
        
        public string? file_description { get; set; }
    }

    /// <summary>
    /// DTO for token validation
    /// </summary>
    public class ValidateTokenDto
    {
        [Required]
        public string token { get; set; }
        
        [Required]
        public string account_code { get; set; }
    }
}
