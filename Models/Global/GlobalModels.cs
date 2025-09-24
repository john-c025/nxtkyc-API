namespace KYCAPI.Models.Global
{
    public class GlobalModels
    {
    }

    public class Response<T>
    {
        public string? message { get; set; }
        public int? status_code { get; set; }
        public T? data { get; set; }
    }
    public class UpdateReadModel
    {
        public string? UserId { get; set; }
        public int? notifId { get; set; }
    }
    public class PositionAccessModel
    {
        public int position_id { get; set; }
        public bool has_cirs_access { get; set; }
        public bool has_mgmt_access { get; set; }
        public bool has_collector_view { get; set; }
        public bool has_dcs_access { get; set; }
        public bool has_admin_access { get; set; }

        public bool? has_uam_access { get; set; }
        public bool? has_legal_access { get; set; }
    }
    public class MainRecordStatusModel
    {
        public int main_remarkid { get; set; }
        public int sub_status_borrowerid { get; set; }
        public int sub_status_unitid { get; set; }
        public bool requested_visit { get; set; }
        public string pn_number { get; set; }
        public string refno { get; set; }
        public string additional_remarks { get; set; }

        public string updated_by { get; set; }
        public string? telecollector { get; set; }
        public bool status { get; set; } = true;
    }

    public class NotificationModel
    {
        public string userId { get;set; }
        public int actionType { get;set; }
        public bool is_unique { get;set; }
        public string? notificationDesc { get;set; } = string.Empty;
    }

    public class AllNotificationsModel
    {
        public int notification_id { get; set; }
        
        public string? notification_description { get; set; }

        public int action_type_id { get; set; }
        public bool is_unread { get; set; }
      
        public bool is_unique { get; set; }
        public string? user_id { get; set; }
        public bool status { get; set; }

        public DateTime date_notified { get; set; }
    }


    public class SystemStatusModel
    {
        public int moduleId { get; set; }
        public string module_desc { get; set; }
        public string under_maintenance { get; set; }
    }

    public class AreasModel
    {
        public string? main_areadesc { get; set; }

        public string? sub_areadesc { get; set; }

        public string? spec_areadesc { get; set; }
        public string? main_areaid { get; set; }

        public string? sub_areaid { get; set; }
        public string? spec_areaid { get; set; }
    
    }

    public class LoanModel
    {
        public int? main_loanid { get; set; }

        public string? main_loandesc { get; set; }

        public int? sub_loanid { get; set; }
        public string? sub_loandesc { get; set; }
       
    }
    public class MainSpecAreaCountModel
    {
        public string? main_areadesc { get; set; }
        public string? sub_areadesc { get; set; }
        public string? sub_areaid { get; set; }
        public int? spec_area_count_in_sub { get; set; }

    }

    public class MainAreaCount
    {
        public int? main_areacount { get; set; }
        public int? sub_areacount { get; set; }
        public int? spec_areacount { get; set; }
    }
    public class SpecArea
    {
        public string? spec_areaid { get; set; }
        public string? spec_areadesc { get; set; }
        public string? sub_areaid { get; set; }
        public string? sub_areadesc { get; set; }
        public string? main_areaid { get; set; }
        public string? main_areadesc { get; set; }
    }

    public class AddSpecAreaRequest
    {
        public string SubAreaId { get; set; }
        public string SpecAreaDesc { get; set; }
    }

    public class RemarksStatus
    {
        public int remarkid { get; set; }
        public string remark_name { get; set; }
        public string remark_desc { get; set; }
        public bool status { get; set; }
    }

    public class BorrowerStatus
    {
        public int autoid { get; set; }
        public string sub_status_name_borrower { get; set; }
        public string sub_status_desc_borrower { get; set; }
        public bool status { get; set; }
    }

    public class UnitStatus
    {
        public int autoid { get; set; }
        public string sub_status_name_unit { get; set; }
        public string sub_status_desc_unit { get; set; }
        public bool status { get; set; }
    }


    public class RequestModel
    {
        public long autoid { get; set; }
        public string initiator_id { get; set; }
        public long request_type { get; set; }
        public string request_message { get; set; }
        public bool is_approved { get; set; }
        public bool is_rejected { get; set; }
        public string refno { get; set; }
        public DateTime? createdat { get; set; }
        public bool is_used { get; set; }
    }

    public class UploadModel
    {
        public long autoid { get; set; }
        public string uploader_id { get; set; }
        public DateTime uploaded_at { get; set; }
        public string? request_id { get; set; }
    }

    public class RequestUploadDto
    {
        public string UserId { get; set; }
        public int RequestType { get; set; } = 6; // 5 = extra upload, 6 = reupload
        public string Message { get; set; }
    }






}
