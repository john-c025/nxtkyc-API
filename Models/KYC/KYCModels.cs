using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace KYCAPI.Models.KYC
{
    // Client Companies Model
    public class ClientCompanyModel
    {
        public int company_id { get; set; }
        [Required]
        public string company_name { get; set; }
        [Required]
        public string company_code { get; set; }
        [Required]
        public string company_type { get; set; }
        public bool is_active { get; set; } = true;
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        [Required]
        public string created_by { get; set; }
        [Required]
        public string updated_by { get; set; }
    }

    // Client Accounts Model
    public class ClientAccountModel
    {
        public int autoid { get; set; }
        public int company_id { get; set; }
        [Required]
        public string account_code { get; set; }
        [Required]
        public string account_origin_number { get; set; }
        [Required]
        public string account_id { get; set; }
        [Required]
        public string fname { get; set; }
        [Required]
        public string mname { get; set; }
        [Required]
        public string sname { get; set; }
        public byte account_status { get; set; } = 1;
        public byte current_privilege_level { get; set; } = 0;
        public string? account_metadata { get; set; }
        public bool is_active { get; set; } = true;
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        [Required]
        public string created_by { get; set; }
        [Required]
        public string updated_by { get; set; }

        // Additional computed properties
        public string full_name => $"{fname} {mname} {sname}".Replace("  ", " ").Trim();
    }

    // KYC Privileges Model
    public class KYCPrivilegeModel
    {
        public int autoid { get; set; }
        public int company_id { get; set; }
        public byte privilege_level { get; set; }
        [Required]
        public string privilege_name { get; set; }
        public string? privilege_description { get; set; }
        public string? privileges_json { get; set; }
        public bool is_active { get; set; } = true;
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        [Required]
        public string created_by { get; set; }
        [Required]
        public string updated_by { get; set; }
    }

    // KYC Requests Model
    public class KYCRequestModel
    {
        public int autoid { get; set; }
        [Required]
        public string kyc_request_id { get; set; }
        public int company_id { get; set; }
        public int client_account_id { get; set; }
        public int? token_id { get; set; }
        [Required]
        public string request_type { get; set; }
        public byte request_status { get; set; } = 1; // 1=Pending, 2=In Review, 3=Approved, 4=Rejected, 5=Archived
        public byte priority_level { get; set; } = 2; // 1=Low, 2=Medium, 3=High, 4=Urgent
        public string? request_description { get; set; }
        public byte current_level { get; set; } = 0;
        public byte level_to_upgrade_to { get; set; } = 0;
        public bool has_files { get; set; } = false;
        public bool is_one_time_only { get; set; } = true;
        public DateTime submitted_at { get; set; }
        public DateTime? completed_at { get; set; }
        public DateTime? archived_at { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        [Required]
        public string created_by { get; set; }
        [Required]
        public string updated_by { get; set; }

        // Additional computed properties
        public string request_status_name => request_status switch
        {
            1 => "Pending",
            2 => "In Review",
            3 => "Approved",
            4 => "Rejected",
            5 => "Archived",
            _ => "Unknown"
        };

        public string priority_level_name => priority_level switch
        {
            1 => "Low",
            2 => "Medium",
            3 => "High",
            4 => "Urgent",
            _ => "Unknown"
        };
    }

    // KYC Media Files Model
    public class KYCMediaFileModel
    {
        public int autoid { get; set; }
        [Required]
        public string kyc_request_id { get; set; }
        [Required]
        public string file_name { get; set; }
        [Required]
        public string file_original_name { get; set; }
        public byte file_type { get; set; }
        [Required]
        public string file_extension { get; set; }
        public long file_size { get; set; }
        [Required]
        public string file_path { get; set; }
        public string? file_url { get; set; }
        [Required]
        public string mime_type { get; set; }
        public byte? file_category { get; set; }
        public string? file_description { get; set; }
        public bool is_verified { get; set; } = false;
        public DateTime uploaded_at { get; set; }
        [Required]
        public string uploaded_by { get; set; }
        public DateTime? verified_at { get; set; }
        public string? verified_by { get; set; }
    }

    // KYC Access Tokens Model
    public class KYCAccessTokenModel
    {
        public int autoid { get; set; }
        [Required]
        public string account_code { get; set; }
        [Required]
        public string token_hash { get; set; }
        public DateTime expires_at { get; set; }
        public bool is_used { get; set; } = false;
        public DateTime? used_at { get; set; }
        public string? kyc_request_id { get; set; }
        public DateTime created_at { get; set; }
    }

    // KYC Approval Actions Model
    public class KYCApprovalActionModel
    {
        public int autoid { get; set; }
        [Required]
        public string kyc_request_id { get; set; }
        public int approver_user_id { get; set; }
        public byte action_type { get; set; } // 1=approve, 2=reject, 3=archive, 4=escalate
        public string? remarks { get; set; }
        public DateTime action_timestamp { get; set; }
        [Required]
        public string created_by { get; set; }

        public string action_type_name => action_type switch
        {
            1 => "Approve",
            2 => "Reject",
            3 => "Archive",
            4 => "Escalate",
            _ => "Unknown"
        };
    }

    // KYC Audit Trail Model
    public class KYCAuditTrailModel
    {
        public int autoid { get; set; }
        [Required]
        public string kyc_request_id { get; set; }
        public byte action_type { get; set; } // 1=created, 2=approved, 3=rejected, 4=archived, 5=escalated
        [Required]
        public string action_by { get; set; }
        public DateTime action_timestamp { get; set; }
        public byte? old_status { get; set; }
        public byte? new_status { get; set; }
        public string? action_details { get; set; }

        public string action_type_name => action_type switch
        {
            1 => "Created",
            2 => "Approved",
            3 => "Rejected",
            4 => "Archived",
            5 => "Escalated",
            _ => "Unknown"
        };
    }

    // System Users Model
    public class SystemUserModel
    {
        public int autoid { get; set; }
        [Required]
        public string system_user_key { get; set; }
        [Required]
        public string user_id { get; set; }
        [Required]
        public string fname { get; set; }
        [Required]
        public string mname { get; set; }
        [Required]
        public string sname { get; set; }
        [Required]
        [EmailAddress]
        public string email { get; set; }
        public string? mobileno { get; set; }
        public bool is_active { get; set; } = true;
        public DateTime date_registered { get; set; }
        public DateTime updated_at { get; set; }

        public string full_name => $"{fname} {mname} {sname}".Replace("  ", " ").Trim();
    }

    // System User Company Access Model
    public class SystemUserCompanyAccessModel
    {
        public int autoid { get; set; }
        public int user_id { get; set; }
        public int company_id { get; set; }
        public bool can_approve { get; set; } = true;
        public bool can_reject { get; set; } = true;
        public bool can_archive { get; set; } = true;
        public bool is_active { get; set; } = true;
        public DateTime assigned_at { get; set; }
        [Required]
        public string assigned_by { get; set; }
        public DateTime updated_at { get; set; }
        [Required]
        public string updated_by { get; set; }
    }

    // System User Credentials Model
    public class SystemUserCredentialsModel
    {
        public int autoid { get; set; }
        [Required]
        public string user_id { get; set; }
        [Required]
        public string coded_id { get; set; }
        [Required]
        public string username { get; set; }
        [Required]
        public string coded_username { get; set; }
        [Required]
        public string coded_password { get; set; }
        public bool status { get; set; } = true;
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    // DTO Models for API Operations
    public class CreateKYCRequestDto
    {
        [Required]
        public string account_code { get; set; }
        [Required]
        public string request_type { get; set; }
        public byte priority_level { get; set; } = 2;
        public string? request_description { get; set; }
        public byte level_to_upgrade_to { get; set; }
        public bool has_files { get; set; } = false;
        public List<IFormFile>? files { get; set; }
    }

    public class ProcessKYCRequestDto
    {
        [Required]
        public string kyc_request_id { get; set; }
        [Required]
        public byte action_type { get; set; } // 1=approve, 2=reject, 3=archive, 4=escalate
        public string? remarks { get; set; }
        [Required]
        public string approver_user_id { get; set; }
    }

    public class CreateClientAccountDto
    {
        public int company_id { get; set; }
        [Required]
        public string account_origin_number { get; set; }
        [Required]
        public string fname { get; set; }
        [Required]
        public string mname { get; set; }
        [Required]
        public string sname { get; set; }
        public string? account_metadata { get; set; }
        [Required]
        public string created_by { get; set; }
    }

    public class UpdateClientAccountDto
    {
        public string? fname { get; set; }
        public string? mname { get; set; }
        public string? sname { get; set; }
        public byte? account_status { get; set; }
        public byte? current_privilege_level { get; set; }
        public string? account_metadata { get; set; }
        public bool? is_active { get; set; }
        [Required]
        public string updated_by { get; set; }
    }

    public class GenerateAccessTokenDto
    {
        [Required]
        public string account_code { get; set; }
        public int hours_valid { get; set; } = 24; // Default 24 hours
    }

    // Dashboard/Report Models
    public class KYCDashboardSummaryModel
    {
        public int total_requests { get; set; }
        public int pending_requests { get; set; }
        public int in_review_requests { get; set; }
        public int approved_requests { get; set; }
        public int rejected_requests { get; set; }
        public int archived_requests { get; set; }
        public int high_priority_requests { get; set; }
        public int urgent_priority_requests { get; set; }
        public decimal approval_rate { get; set; }
        public decimal rejection_rate { get; set; }
        public double average_processing_hours { get; set; }
    }

    public class KYCCompanyStatisticsModel
    {
        public int company_id { get; set; }
        public string company_name { get; set; }
        public int total_clients { get; set; }
        public int active_clients { get; set; }
        public int total_requests { get; set; }
        public int pending_requests { get; set; }
        public int approved_requests { get; set; }
        public int rejected_requests { get; set; }
        public decimal approval_rate { get; set; }
        public double average_processing_hours { get; set; }
    }

    public class KYCRequestDetailedModel : KYCRequestModel
    {
        public string client_full_name { get; set; }
        public string company_name { get; set; }
        public List<KYCMediaFileModel> attached_files { get; set; } = new();
        public List<KYCApprovalActionModel> approval_actions { get; set; } = new();
        public List<KYCAuditTrailModel> audit_trail { get; set; } = new();
    }

    // DTOs for Upsert Client Account
    public class UpsertClientAccountDto
    {
        public string? account_origin_number { get; set; }
        
        [Required]
        public int company_id { get; set; }
        
        public byte current_privilege_level { get; set; } = 0;
    }

    public class UpsertClientAccountResult
    {
        public int ClientId { get; set; }
        public string AccountCode { get; set; }
        public string AccountId { get; set; }
        public bool IsNewAccount { get; set; }
    }

    // Token Validation Result
    public class TokenValidationResult
    {
        public bool IsValid { get; set; }
        public string AccountCode { get; set; }
        public byte CurrentPrivilegeLevel { get; set; }
        public string CompanyName { get; set; }
        public string CompanyCode { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    // File Category Model
    public class FileCategoryModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    // Validate Token DTO
    public class ValidateTokenDto
    {
        [Required]
        public string token { get; set; }
        
        [Required]
        public string account_code { get; set; }
    }

    // Account Check Result
    public class AccountCheckResult
    {
        public bool AccountExists { get; set; }
        public string? AccountOriginNumber { get; set; }
        public bool OriginNumberUnique { get; set; }
        public int CompanyId { get; set; }
    }
}
