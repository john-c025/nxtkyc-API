using Microsoft.AspNetCore.Mvc;
using KYCAPI.Data;
using KYCAPI.Models.KYC;
using KYCAPI.Models.Configuration;
using KYCAPI.Helpers;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace KYCAPI.Controllers.KYC
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize] // Add JWT authentication requirement
    public class KYCController : ControllerBase
    {
        private readonly KYCRepository _kycRepository;
        private readonly ILogger<KYCController> _logger;
        private readonly IConfiguration _configuration;

        public KYCController(KYCRepository kycRepository, ILogger<KYCController> logger, IConfiguration configuration)
        {
            _kycRepository = kycRepository;
            _logger = logger;
            _configuration = configuration;
        }

        // Helper method to get current user ID from JWT claims
        private string GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            return userIdClaim?.Value ?? throw new UnauthorizedAccessException("User ID not found in token");
        }

        #region Client Account Management

        /// <summary>
        /// Get all client accounts with optional filtering
        /// </summary>
        [HttpGet("clients")]
        public async Task<IActionResult> GetClientAccounts(
            [FromQuery] int? companyId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var clients = await _kycRepository.GetClientAccountsAsync(companyId, isActive, page, pageSize);

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "Client accounts retrieved successfully",
                    Data = clients
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client accounts");
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get client account by account code
        /// </summary>
        [HttpGet("clients/{accountCode}")]
        public async Task<IActionResult> GetClientAccountByCode(string accountCode)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var client = await _kycRepository.GetClientAccountByCodeAsync(accountCode);
                if (client == null)
                {
                    return NotFound(new APIResponse { Success = false, Message = "Client account not found" });
                }

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "Client account retrieved successfully",
                    Data = client
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client account with code: {AccountCode}", accountCode);
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create a new client account
        /// </summary>
        [HttpPost("clients")]
        public async Task<IActionResult> CreateClientAccount([FromBody] CreateClientAccountDto clientDto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                clientDto.created_by = currentUserId;
                var clientId = await _kycRepository.CreateClientAccountAsync(clientDto, currentUserId);

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "Client account created successfully",
                    Data = new { client_id = clientId }
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating client account");
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update client account
        /// </summary>
        [HttpPut("clients/{accountId}")]
        public async Task<IActionResult> UpdateClientAccount(int accountId, [FromBody] UpdateClientAccountDto updateDto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                updateDto.updated_by = currentUserId;
                var success = await _kycRepository.UpdateClientAccountAsync(accountId, updateDto);

                if (!success)
                {
                    return NotFound(new APIResponse { Success = false, Message = "Client account not found" });
                }

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "Client account updated successfully"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client account with ID: {AccountId}", accountId);
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create or update client account with minimal parameters (upsert) - No auth required for external client use
        /// </summary>
        [HttpPost("clients/create")]
        [AllowAnonymous] // Override authorization for this specific endpoint
        public async Task<IActionResult> UpsertClientAccount([FromBody] UpsertClientAccountDto upsertDto)
        {
            try
            {
                var result = await _kycRepository.UpsertClientAccountAsync(upsertDto, "EXTERNAL_CLIENT");

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = result.IsNewAccount ? "Client account created successfully" : "Client account updated successfully",
                    Data = new { 
                        client_id = result.ClientId,
                        account_code = result.AccountCode,
                        account_id = result.AccountId,
                        is_new_account = result.IsNewAccount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting client account");
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        #endregion

        #region Access Token Management

        /// <summary>
        /// Generate access token for KYC request
        /// </summary>
        [HttpPost("tokens/generate")]
        public async Task<IActionResult> GenerateAccessToken([FromBody] GenerateAccessTokenDto tokenDto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Validate account code exists
                var client = await _kycRepository.GetClientAccountByCodeAsync(tokenDto.account_code);
                if (client == null)
                {
                    return BadRequest(new APIResponse { Success = false, Message = "Invalid account code" });
                }

                var token = await _kycRepository.GenerateAccessTokenAsync(tokenDto);

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "Access token generated successfully",
                    Data = new { 
                        token = token, 
                        expires_in_hours = tokenDto.hours_valid,
                        account_code = tokenDto.account_code
                    }
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating access token for account: {AccountCode}", tokenDto.account_code);
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }


        #endregion

        #region KYC Request Management

        /// <summary>
        /// Create a new KYC request
        /// </summary>
        [HttpPost("requests")]
        public async Task<IActionResult> CreateKYCRequest([FromBody] CreateKYCRequestDto requestDto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var kycRequestId = await _kycRepository.CreateKYCRequestAsync(requestDto, currentUserId);

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "KYC request created successfully",
                    Data = new { kyc_request_id = kycRequestId }
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new APIResponse { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating KYC request");
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get KYC requests with optional filtering
        /// </summary>
        [HttpGet("requests")]
        public async Task<IActionResult> GetKYCRequests(
            [FromQuery] int? companyId = null,
            [FromQuery] byte? status = null,
            [FromQuery] byte? priorityLevel = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var requests = await _kycRepository.GetKYCRequestsAsync(companyId, status, priorityLevel, page, pageSize);

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "KYC requests retrieved successfully",
                    Data = requests
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving KYC requests");
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get detailed KYC request by ID
        /// </summary>
        [HttpGet("requests/{kycRequestId}")]
        public async Task<IActionResult> GetKYCRequestDetailed(string kycRequestId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var request = await _kycRepository.GetKYCRequestDetailedAsync(kycRequestId);
                if (request == null)
                {
                    return NotFound(new APIResponse { Success = false, Message = "KYC request not found" });
                }

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "KYC request retrieved successfully",
                    Data = request
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving KYC request: {KYCRequestId}", kycRequestId);
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Process KYC request (approve/reject/archive/escalate)
        /// </summary>
        [HttpPost("requests/process")]
        public async Task<IActionResult> ProcessKYCRequest([FromBody] ProcessKYCRequestDto processDto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                processDto.approver_user_id = currentUserId;

                // Validate action type
                if (processDto.action_type < 1 || processDto.action_type > 4)
                {
                    return BadRequest(new APIResponse { Success = false, Message = "Invalid action type" });
                }

                var success = await _kycRepository.ProcessKYCRequestAsync(processDto);

                if (!success)
                {
                    return BadRequest(new APIResponse { Success = false, Message = "Failed to process KYC request" });
                }

                var actionName = processDto.action_type switch
                {
                    1 => "approved",
                    2 => "rejected",
                    3 => "archived",
                    4 => "escalated",
                    _ => "processed"
                };

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = $"KYC request {actionName} successfully"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new APIResponse { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing KYC request: {KYCRequestId}", processDto.kyc_request_id);
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        #endregion

        #region File Management

        /// <summary>
        /// Get media files for a KYC request
        /// </summary>
        [HttpGet("requests/{kycRequestId}/files")]
        public async Task<IActionResult> GetKYCMediaFiles(string kycRequestId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var files = await _kycRepository.GetKYCMediaFilesAsync(kycRequestId);

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "KYC media files retrieved successfully",
                    Data = files
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving KYC media files for request: {KYCRequestId}", kycRequestId);
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Upload files for a KYC request
        /// </summary>
        [HttpPost("requests/{kycRequestId}/files")]
        public async Task<IActionResult> UploadKYCFiles(string kycRequestId, [FromForm] List<IFormFile> files, [FromForm] string? fileDescription = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                if (!files.Any())
                {
                    return BadRequest(new APIResponse { Success = false, Message = "No files provided" });
                }

                // Validate KYC request exists
                var kycRequest = await _kycRepository.GetKYCRequestDetailedAsync(kycRequestId);
                if (kycRequest == null)
                {
                    return NotFound(new APIResponse { Success = false, Message = "KYC request not found" });
                }

                var uploadedFiles = new List<object>();
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "KYC");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var fileExtension = Path.GetExtension(file.FileName);
                        var fileName = $"{Guid.NewGuid()}{fileExtension}";
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
                            file_url = $"/uploads/kyc/{fileName}",
                            mime_type = file.ContentType,
                            file_category = 1, // General category
                            file_description = fileDescription,
                            uploaded_by = currentUserId
                        };

                        var fileId = await _kycRepository.SaveKYCMediaFileAsync(mediaFile);
                        uploadedFiles.Add(new { file_id = fileId, file_name = fileName, original_name = file.FileName });
                    }
                }

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "Files uploaded successfully",
                    Data = uploadedFiles
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading files for KYC request: {KYCRequestId}", kycRequestId);
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        #endregion

        #region Dashboard and Analytics

        /// <summary>
        /// Get KYC dashboard summary statistics
        /// </summary>
        [HttpGet("dashboard/summary")]
        public async Task<IActionResult> GetDashboardSummary(
            [FromQuery] int? companyId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var summary = await _kycRepository.GetDashboardSummaryAsync(companyId, fromDate, toDate);

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "Dashboard summary retrieved successfully",
                    Data = summary
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard summary");
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get company statistics
        /// </summary>
        [HttpGet("dashboard/company-statistics")]
        public async Task<IActionResult> GetCompanyStatistics()
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var statistics = await _kycRepository.GetCompanyStatisticsAsync();

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "Company statistics retrieved successfully",
                    Data = statistics
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company statistics");
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        #endregion

        #region Company Management

        /// <summary>
        /// Get all companies
        /// </summary>
        [HttpGet("companies")]
        public async Task<IActionResult> GetAllCompanies()
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var companies = await _kycRepository.GetAllCompaniesAsync();

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "Companies retrieved successfully",
                    Data = companies
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving companies");
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get company by ID
        /// </summary>
        [HttpGet("companies/{companyId}")]
        public async Task<IActionResult> GetCompanyById(int companyId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var company = await _kycRepository.GetCompanyByIdAsync(companyId);
                if (company == null)
                {
                    return NotFound(new APIResponse { Success = false, Message = "Company not found" });
                }

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "Company retrieved successfully",
                    Data = company
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company with ID: {CompanyId}", companyId);
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create a new company
        /// </summary>
        [HttpPost("companies")]
        public async Task<IActionResult> CreateCompany([FromBody] ClientCompanyModel company)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                company.created_by = currentUserId;
                company.updated_by = currentUserId;

                var companyId = await _kycRepository.CreateCompanyAsync(company, currentUserId);

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "Company created successfully",
                    Data = new { company_id = companyId }
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        #endregion

        #region Privilege Management

        /// <summary>
        /// Get KYC privilege levels for dropdowns
        /// </summary>
        [HttpGet("privileges")]
        public async Task<IActionResult> GetKYCPrivileges([FromQuery] int? companyId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var privileges = await _kycRepository.GetKYCPrivilegesAsync(companyId);
                
                var enhancedPrivileges = privileges.Select(p => new
                {
                    autoid = p.autoid,
                    company_id = p.company_id,
                    privilege_level = p.privilege_level,
                    privilege_name = p.privilege_name,
                    privilege_description = p.privilege_description,
                    services = GetServicesFromJson(p.privileges_json),
                    limits = GetLimitsFromJson(p.privileges_json),
                    requirements = GetRequirementsFromJson(p.privileges_json),
                    is_active = p.is_active,
                    created_at = p.created_at,
                    updated_at = p.updated_at,
                    created_by = p.created_by,
                    updated_by = p.updated_by,
                    company_name = p.company_id
                }).OrderBy(p => p.company_id).ThenBy(p => p.privilege_level);

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "KYC privileges retrieved successfully",
                    Data = enhancedPrivileges
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving KYC privileges");
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        #endregion

        #region File Category Management

        /// <summary>
        /// Get file categories for file management
        /// </summary>
        [HttpGet("files/categories")]
        public async Task<IActionResult> GetFileCategories()
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var categories = await _kycRepository.GetFileCategoriesAsync();

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "File categories retrieved successfully",
                    Data = categories
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new APIResponse { Success = false, Message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file categories");
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        #endregion

        #region Status Reference

        /// <summary>
        /// Get status reference data
        /// </summary>
        [HttpGet("reference/statuses")]
        public IActionResult GetStatusReference()
        {
            try
            {
                var statuses = new
                {
                    request_statuses = new[]
                    {
                        new { value = 1, name = "Pending" },
                        new { value = 2, name = "In Review" },
                        new { value = 3, name = "Approved" },
                        new { value = 4, name = "Rejected" },
                        new { value = 5, name = "Archived" }
                    },
                    priority_levels = new[]
                    {
                        new { value = 1, name = "Low" },
                        new { value = 2, name = "Medium" },
                        new { value = 3, name = "High" },
                        new { value = 4, name = "Urgent" }
                    },
                    action_types = new[]
                    {
                        new { value = 1, name = "Approve" },
                        new { value = 2, name = "Reject" },
                        new { value = 3, name = "Archive" },
                        new { value = 4, name = "Escalate" }
                    }
                };

                return Ok(new APIResponse
                {
                    Success = true,
                    Message = "Status reference data retrieved successfully",
                    Data = statuses
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status reference data");
                return StatusCode(500, new APIResponse { Success = false, Message = "Internal server error" });
            }
        }

        #endregion

        #region Helper Methods

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
}
