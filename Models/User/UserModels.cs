using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreHRAPI.Models.User
{
    public class UserPositionDto
    {
        public string UserId { get; set; }
        public int PositionId { get; set; }
        public string PositionDesc { get; set; }
    }
    public class UserDetails
    {
        public string UserId { get; set; }
        public string CodedId { get; set; }
        public DateTime? DateRegistered { get; set; }
        public string FName { get; set; }
        public string MName { get; set; }
        public string SName { get; set; }
        public string ContactNumber { get; set; }
        public string EmailAddress { get; set; }
        public int PositionId { get; set; }
        public string PositionDesc { get; set; }
        public bool? IsCollector { get; set; }
        public string BranchDesc { get; set; }
        public string CountryCode { get; set; }
        public string CityCode { get; set; }
        public string StateCode { get; set; }
        public string Address { get; set; }
        public string AccessLevel { get; set; }
        public ActionPermissions ActionPermissions { get; set; }
        public ModuleAccess ModuleAccess { get; set; } // Now contains the employee access permissions
        public bool? IsUserActive { get; set; }
    }

    public class ActionPermissions
    {
        public bool? InsertAccess { get; set; }
        public bool? UpdateAccess { get; set; }
        public bool? UploadAccess { get; set; }
        public bool? DeleteAccess { get; set; }
    }



    // New class for employee access permissions
    public class ModuleAccess
    {
        public bool? can_view_profiles { get; set; }
        public bool? can_update_basic_info { get; set; }
        public bool? can_generate_basic_reports { get; set; }
        public bool? can_company_scoped_only { get; set; }
        public bool? can_delete_records { get; set; }
        public bool? can_manage_full_records { get; set; }
        public bool? can_manage_employment_lifecycle { get; set; }
        public bool? can_generate_advanced_reports { get; set; }
        public bool? can_access_all_companies { get; set; }
        public bool? can_view_audit_logs { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string? UserId { get; set; } = "";
        public string? UserEmail { get; set; } = "";
    }

    public class UpdatePasswordRequest
    {
        public string UserId { get; set; }
        public string NewPassword { get; set; }
    }

    public class RegisterUserRequest
    {
        public string CodedPassword { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Surname { get; set; }
        public int PositionId { get; set; }
        public string ContactNumber { get; set; }
        public string EmailAddress { get; set; }
      
    }

    public class RegisterUserRequestResponse
    {
        [Column("generated_userid")]
        public string GeneratedUserId { get; set; }

        [Column("coded_id")]
        public string CodedId { get; set; }

        [Column("coded_username")]
        public string CodedUsername { get; set; }

        [Column("message")]
        public string Message { get; set; }
    }

    public class UserCredentials
    {
        public string userid { get; set; }
        public string codedid { get; set; }
        public string codedusername { get; set; }
        public string codedpword { get; set; }
        public bool status { get; set; }
    }

    public class UserStatusCheck
    {
        public string is_user_active { get; set; }
        public string userid { get; set; }
        public string email_address { get; set; }
    }

    public class Positions
    {
        public string position_id { get; set; }
        public string position_desc { get; set; }
        public bool is_collector { get; set; }
    }

    public class Branches
    {
        public string branchid { get; set; }
        public string branch_desc { get; set; }
    }

    public class Companies
    {
        public string company_id { get; set; }
        public string company_name { get; set; }
    }

    public class DeactivateUserRequest
    {
        public string UserId { get; set; }
    }

    public class UploadProfilePictureRequest
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public IFormFile File { get; set; }
    }
}