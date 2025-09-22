using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Collections.Generic;
using System.Net;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Sprache;

namespace CoreHRAPI.Models.Reports
{
    public class ReportsModel
    {
        public class EmployeeMasterFileModel
        {
            public long? autoid { get; set; }
            public string full_name { get; set; }
            public string fname { get; set; }
            public string mname { get; set; }
            public string sname { get; set; }
            public string? active_status { get; set; }
            public int? age { get; set; }
            public string? civil_status { get; set; }
            public string? gender { get; set; }
            public DateTime? birthdate { get; set; }
            public string address { get; set; }
            public string contact_no { get; set; }
            public string email { get; set; }
            public DateTime? start_date { get; set; }
            public int? service_length_years { get; set; }
            public int? service_length_months { get; set; }
            public string position_name { get; set; }
            public string employee_status_type { get; set; }
            public string? department_id { get; set; }
            public string sss { get; set; }
            public string tin { get; set; }
            public string philhealth { get; set; }
            public string pagibig { get; set; }
            public string coc_attendance { get; set; }
            public string coc_acknowledgement { get; set; }
            public string disciplinary_action { get; set; }
            public string disciplinary_action_description { get; set; }
            public DateTime? disciplinary_action_effectivity { get; set; }
            public string? discipinary_action_status { get; set; }
            public string labor_case { get; set; }
            public string labor_case_reason { get; set; }
            public string? labor_case_status { get; set; }
            public DateTime? dateupdated { get; set; }
            public DateTime? snapshot_month { get; set; }
            public long? companyid { get; set; }
            public string? work_location { get; set; }
        }

        public class MasterListExtractionModel
            {
                public string Davao { get; set; }
                public string? Tele { get;set; }
                public decimal? TotalPayment { get; set; }
                public decimal? PrincipalPayment { get; set; }
                public decimal? PenaltyPayment { get; set; }
                public decimal? ServiceCharge { get; set; }
                public decimal? ParkingFee { get; set; }
                public decimal? LegalFee { get; set; }
                public decimal? Interest { get; set; }

                public decimal? FixedPrincipal { get; set; }

                public string Field { get; set; }
                public string MONTH { get; set; }
                public int Age { get; set; }
                [Column("PN Number")] // This maps the property to "PN Number" in Excel
                public string PNNumber { get; set; }
                [Column("Ref No")]
                public string RefNo { get; set; }
                [Column("Borrower Name")]
                public string BorrowerName { get; set; }
                
                public int Terms { get; set; }
                public DateTime? Maturity { get; set; }
                [Column("First Due")]
                public DateTime? FirstDue { get; set; }
                [Column("Last Applied")]
                public DateTime? LastApplied { get; set; }
                [Column("Last Payment Date")]
                public DateTime? LastPaymentDate { get; set; }
                public decimal Collectibles { get; set; }
                public decimal Amortization { get; set; }
                public decimal Paid { get; set; }
                public decimal Remain { get; set; }
                public decimal Overdue { get; set; }
                public decimal Balance { get; set; }
                public decimal Principal { get; set; }
                [Column("Loan Type")]
                public string LoanType { get; set; }
                public string AO { get; set; }

                public string CM { get; set; }

                public string Address { get; set; }
                public string Area { get; set; }

                [Column("Collection Company")]
                public string CollectionCompany { get; set; }

                [Column("Contact No.")]
                public string ContactNo { get; set; }
                public string ProductType { get; set; }
        
                [Column("Balance Less Amort")]
                public decimal BalanceLessAmort { get; set; }

        }

        public class CIRSCompanies
        {
            public int autoid { get; set; }
            public string company_name { get; set; }
            public bool status { get; set; }
        }
        public class FormattedAreaViewModel
        {
            public string refno { get; set; }
            public string pn_number { get; set; }
            public string borrower_name { get; set; }
            public string spec_areadesc { get; set; }
            public string sub_areadesc { get; set; }
            public string main_areadesc { get; set; }
            public string actual_address { get; set; }
        }

        public class MasterlistAddressModel
        {
            public string refno { get; set; }
            public string pn_number { get; set; }
            public string address { get; set; }
        }

        public class AreaListResponse
        {
            public List<SpecAreaData> Data { get; set; }
        }

        public class SpecAreaData
        {
            public string main_areadesc { get; set; }
            public string sub_areadesc { get; set; }
            public string spec_areadesc { get; set; }
            public string main_areaid { get; set; }
            public string sub_areaid { get; set; }
            public string spec_areaid { get; set; }
        }

        public class CIRSMasterListModel
        {
            public string branch { get; set; }
            public string field_collector { get; set; }
            public string month { get; set; }
            public int age { get; set; }
            public string pn_number { get; set; }
            public string refno { get; set; }
            public string borrower_name { get; set; }
            public int terms { get; set; }
            public DateTime? maturity { get; set; }
            public DateTime? first_due { get; set; }
            public DateTime? last_applied { get; set; }
            public DateTime? last_payment_date { get; set; }
            public decimal collectibles { get; set; }
            public decimal amortization { get; set; }
            public decimal paid { get; set; }
            public decimal remain { get; set; }
            public decimal overdue { get; set; }
            public decimal balance { get; set; }
            public decimal principal { get; set; }
            public string loan_type { get; set; }
            public string ao { get; set; }

            public string cm { get; set; }

            public string address { get; set; }
            public string area { get; set; }
            public string collection_company { get; set; }
            public string contact_no { get; set; }
            public string product_type { get; set; }
            public string balance_less_amount { get; set; }
            public DateTime? date_updated { get; set; }

        }

        public class TelecollectorForAssignmentRecordsModel
        {
            public string telecollector { get; set; }
            public string field_collector { get; set; }
            public string? fname_field { get; set; }
            public string? fname_tele { get; set; }
            public string pn_number { get; set; }
            public string refno { get; set; }
            public string borrower_name { get; set; }
            public decimal? overdue { get; set; }
            public decimal? balance { get; set; }
            public decimal? prinicipal { get; set; }
            public string loan_type { get; set; }
            public long commpanyid { get; set; }
            public string remark_name { get; set; }
            public string sub_status_name_borrower { get; set; }
            public string sub_status_name_unit { get; set; }
            public bool? requested_visit { get; set; }
            public DateTime? date_updated { get; set; }
        }


        public class FieldcollectorForAssignmentRecordsModel
        {
            public string telecollector { get; set; }
            public string field_collector { get; set; }
            public string pn_number { get; set; }
            public string refno { get; set; }
            public string borrower_name { get; set; }
            public decimal? overdue { get; set; }
            public decimal? balance { get; set; }
            public decimal? prinicipal { get; set; }
            public string loan_type { get; set; }
            public long commpanyid { get; set; }
            public string remark_name { get; set; }
            public string sub_status_name_borrower { get; set; }
            public string sub_status_name_unit { get; set; }
            public bool? has_visited { get; set; }
            public DateTime? date_updated { get; set; }
        }

        public class TeleFieldBindingModel
        {
            public int autoid { get; set; }
            public int spec_areaid { get; set; }
            public int main_areaid { get; set; }
            public int sub_areaid { get; set; }
            public string? field_collectorid { get; set; }
            public string? telecollectorid { get; set; }

            public string? fname_field { get; set; }
            public string? fname_tele { get; set; }
            public string? company_name { get; set; }
            public int? companyid { get; set; }
            public DateTime date_updated { get; set; }
            public bool status { get; set; }
            public string spec_areadesc { get; set; }
            public string sub_areadesc { get; set; }
            public string main_areadesc { get; set; }
        }
        public class CIRSAnalyticsSummary
        {
            // =====================
            // Optional Grouping
            // =====================
            public string FieldCollector { get; set; } // if grouped by collector

            // =====================
            // Raw Aggregates
            // =====================
            public decimal TotalCollectibles { get; set; }      // SUM(overdue)
            public int TotalAccounts { get; set; }              // COUNT(*)
            public decimal RunningCollection { get; set; }      // SUM(paid)
            public decimal PrincipalCollection { get; set; }    // SUM(principal)
            public int PrincipalAccounts { get; set; }          // COUNT(principal > 0)
            public decimal Overdue { get; set; }                // SUM(overdue)
            public int OverdueAccounts { get; set; }            // COUNT(overdue > 0)

            public decimal? ActualPenalty { get; set; }         // SUM(penaltyPayment)
            public decimal? Provision { get; set; }             // SUM(fixedPrincipal)
            public int? ProvisionAccounts { get; set; }         // COUNT(fixedPrincipal > 0)
            public decimal? ActualFixed { get; set; }           // SUM(fixedPrincipal where paid)
            public int? ActualFixedAccounts { get; set; }       // COUNT(fixedPrincipal > 0 where paid)

            // =====================
            // Derived Fields
            // =====================

            public decimal TargetCollection => TotalCollectibles;

            public decimal ActualCollectionPercent =>
                TotalCollectibles == 0 ? 0 : RunningCollection / TotalCollectibles;

            public decimal PrincipalCollectionPercent =>
                TotalCollectibles == 0 ? 0 : PrincipalCollection / TotalCollectibles;

            public decimal PrinOverdueOnly => Overdue - PrincipalCollection;

            public decimal OneIsToOnePercent =>
                TotalCollectibles == 0 ? 0 : PrinOverdueOnly / TotalCollectibles;

            public decimal Target90PercentColl => PrincipalCollection - TargetCollection;

            public int PrincipalLessOverdueAcct => PrincipalAccounts - OverdueAccounts;

            public decimal ExcessOverdue => Overdue - PrincipalCollection;

            public int ExcessOverdueAccounts => OverdueAccounts;

            public decimal? TargetPenalty =>
                PrincipalCollection == 0 ? (decimal?)null : PrincipalCollection * 0.15m;

            public decimal? PenaltyPercent =>
                (ActualPenalty.HasValue && RunningCollection != 0)
                    ? ActualPenalty.Value / RunningCollection
                    : null;

            public decimal? ProvisionFixedPercent =>
                (Provision.HasValue && Provision != 0)
                    ? (ActualFixed ?? 0) / Provision
                    : null;

            public decimal? Slide =>
                (Provision.HasValue && ActualFixed.HasValue)
                    ? Provision - ActualFixed
                    : null;

            public int? SlideAccounts =>
                (ProvisionAccounts.HasValue && ActualFixedAccounts.HasValue)
                    ? ProvisionAccounts - ActualFixedAccounts
                    : null;
        }

        public class CollectionActivityLogModel
        {
            public long activityid { get; set; }
            public string refno { get; set; }
            public string pn_number { get; set; }
            public string action_key { get; set; }
            public string description { get; set; }
            public string additional_remarks { get; set; }
            public string collectorid { get; set; }
            public string collector_type { get; set; }
            public string updated_by { get; set; }
            public decimal? amount { get; set; }
            public DateTime date_updated { get; set; }
            public string telecollector { get; set; }
        }
        public class CollectorUser
        {
            public string userid { get; set; }
            public string FullName { get; set; }
            public string position_desc { get; set; }
            public bool is_collector { get; set; }
            public int position_id { get; set; }
        }

        public class TelecollectorDailyReportModel
        {
            public int report_id { get; set; }
            public DateTime report_date { get; set; }
            public int telecollectorid { get; set; }
            public string telecollector_name { get; set; }
            public string branch { get; set; }

            public string handle_acct_code { get; set; }
            public string handle_acct_desc { get; set; }

            public decimal collection_for_day { get; set; }
            public int collection_for_day_count { get; set; }

            public decimal principal { get; set; }
            public decimal penalty { get; set; }
            public decimal penalty_percent { get; set; }

            public decimal forecast { get; set; }
            public int forecast_count { get; set; }

            public decimal on_track_figures { get; set; }
            public decimal on_track_percent { get; set; }

            public decimal collectibles { get; set; }
            public int collectibles_count { get; set; }

            public decimal actual_running_collx { get; set; }
            public decimal actual_running_collx_percent { get; set; }
            public decimal actual_running_principal { get; set; }
            public decimal actual_running_principal_percent { get; set; }
            public decimal actual_running_penalty { get; set; }
            public decimal actual_running_penalty_percent { get; set; }

            public decimal var_otf { get; set; }

            public decimal on_track_figures_2nd { get; set; }
            public decimal on_track_percent_2nd { get; set; }
            public decimal second_month_provision { get; set; }
            public int second_month_count { get; set; }
            public decimal second_month_fixed_for_day { get; set; }
            public int second_month_fixed_count { get; set; }
            public decimal second_month_running_fixed { get; set; }
            public int second_month_running_fixed_count { get; set; }
            public decimal second_month_variance { get; set; }
            public int second_month_variance_count { get; set; }

            public decimal var_otf_2nd { get; set; }
        }

        public class TelecollectorDailyReportInsertModel
        {
            public DateTime report_date { get; set; }
            public int telecollectorid { get; set; }
            public string tele_name { get; set; }
            public string branch { get; set; }
            public string handle_acct { get; set; }
            public decimal collection_for_the_day_amount { get; set; }
            public int collection_for_the_day_count { get; set; }
            public decimal principal_amount { get; set; }
            public decimal penalty_amount { get; set; }
            public decimal penalty_percentage { get; set; }
            public decimal forecast_amount { get; set; }
            public int forecast_count { get; set; }
            public decimal on_track_figures_amount { get; set; }
            public decimal on_track_figures_percentage { get; set; }
            public decimal collectibles_amount { get; set; }
            public int collectibles_count { get; set; }
            public decimal actual_running_collx_amount { get; set; }
            public decimal actual_running_collx_percentage { get; set; }
            public decimal actual_running_principal_amount { get; set; }
            public decimal actual_running_principal_percentage { get; set; }
            public decimal actual_running_penalty_amount { get; set; }
            public decimal actual_running_penalty_percentage { get; set; }
            public decimal var_otf_amount { get; set; }
            public decimal on_track_figures_2ndmon_amount { get; set; }
            public decimal on_track_figures_2ndmon_percentage { get; set; }
            public decimal second_mon_provision_amount { get; set; }
            public int second_mon_provision_count { get; set; }
            public decimal second_mon_fixed_for_the_day_amount { get; set; }
            public int second_mon_fixed_for_the_day_count { get; set; }
            public decimal second_mon_running_fixed_amount { get; set; }
            public int second_mon_running_fixed_count { get; set; }
            public decimal second_mon_variance_amount { get; set; }
            public int second_mon_variance_count { get; set; }
            public decimal var_otf_2ndmon_amount { get; set; }
        }
        public class CollectorUserModel
        {
            public int position_id { get; set; }
            public string position_desc { get; set; }
            public string userid { get; set; }
            public string fname { get; set; }
            public string mname { get; set; }
            public string sname { get; set; }
            public string profile_picture_path { get; set; }
            public int main_companyid { get; set; }
            public int sub_companyid { get; set; }
        }

        public class AssignCollectorByAreaPostModel
        {
            // Existing single-assign parameter
            public int? autoid { get; set; }

            // New optional bulk parameters
            public string? mainareaid { get; set; }
            public string? subareaid { get; set; }

            // Optional collector assignments
            public string? fieldcollectorid { get; set; }
            public string? telecollectorid { get; set; }
            public int? companyid { get; set; }
        }


        public class ActionTypeListModel
        {
            public int action_type_id { get; set; }
            public string action_key { get; set; }
            public string description { get; set; }
            public bool status  { get; set; }
        }

        public class CollectionActivityModel
        {
            public int ActionTypeId { get; set; }
            public string Refno { get; set; }
            public string PnNumber { get; set; }
            public string CollectorType { get; set; }  // "tele" or "field"
            public string PerformedBy { get; set; }     // username or id
            public string Remarks { get; set; }
            public string? Telecollector { get; set; }
            public decimal? Amount { get; set; }        // optional
        }


        public class FieldCollectionActivityModel
        {
            public int ActionTypeId { get; set; }
            public string Refno { get; set; }
            public string PnNumber { get; set; }
            public string CollectorType { get; set; }  // "tele" or "field"
            public string PerformedBy { get; set; }     // username or id
            public string Remarks { get; set; }
            public string? Telecollector { get; set; }
            public decimal? Amount { get; set; }        // optional
        }

        ///




















        // Update your EmployeeHRDetailsModel to include city_names
        // Update your EmployeeHRDetailsModel to include city_name
        public class EmployeeHRDetailsModel
        {
            public long autoid { get; set; }
            public DateTime date_registered { get; set; }

            [Required]
            [StringLength(50)]
            public string empid { get; set; }

            [Required]
            [StringLength(255)]
            public string fname { get; set; }

            [StringLength(255)]
            public string mname { get; set; }

            [Required]
            [StringLength(255)]
            public string sname { get; set; }

            public int? age { get; set; }
            public DateTime? birth_date { get; set; }
            public int? civil_status { get; set; }
            public int? gender { get; set; }

            [StringLength(50)]
            public string contact_no { get; set; }

            [StringLength(255)]
            public string email { get; set; }

            public string address { get; set; }
            public int? company_id { get; set; }
            public int? employment_status { get; set; }
            public int? position_id { get; set; }
            public string? position_desc { get; set; }
            public int? department_id { get; set; }
            public DateTime? start_date { get; set; }

            [StringLength(50)]
            public string sss { get; set; }

            [StringLength(50)]
            public string tin { get; set; }

            [StringLength(50)]
            public string philhealth { get; set; }

            [StringLength(50)]
            public string pagibig { get; set; }

            public bool active_status { get; set; } = true;
            public bool record_status { get; set; } = true;
            public object city_code { get; set; }
            public string city_name { get; set; } // Add this property
        }

        public class EmployeePositionModel
        {
            public long autoid { get; set; }
            public int position_id { get; set; }
            public string position_desc { get; set; } = string.Empty;
            public long companyid { get; set; }
            public bool status { get; set; }
        }
        public class EmployeeHRDetailsCreateModel
        {
            public string? empid { get; set; }

            [Required]
            [StringLength(255)]
            public string fname { get; set; }

            [StringLength(255)]
            public string mname { get; set; }

            [Required]
            [StringLength(255)]
            public string sname { get; set; }

            public int? age { get; set; }
            public DateTime? birth_date { get; set; }
            public int? civil_status { get; set; }
            public int? gender { get; set; }

            [StringLength(50)]
            public string contact_no { get; set; }

            [StringLength(255)]
            public string email { get; set; }

            public string address { get; set; }
            public int? company_id { get; set; }
            public int? employment_status { get; set; }
            public int? position_id { get; set; }
            public int? department_id { get; set; }
            public DateTime? start_date { get; set; }

            [StringLength(50)]
            public string sss { get; set; }

            [StringLength(50)]
            public string tin { get; set; }

            [StringLength(50)]
            public string philhealth { get; set; }

            [StringLength(50)]
            public string pagibig { get; set; }

            public bool active_status { get; set; } = true;
            public JsonElement? city_code { get; set; } // Changed to JsonElement
        }
        public class EmployeeHRDetailsUpdateModel
        {
            public long? autoid { get; set; } // Made nullable and removed [Required]

            [StringLength(50)]
            public string empid { get; set; }

            [StringLength(255)]
            public string? fname { get; set; }

            [StringLength(255)]
            public string? mname { get; set; }

            [StringLength(255)]
            public string? sname { get; set; }

            public int? age { get; set; }
            public DateTime? birth_date { get; set; }
            public int? civil_status { get; set; }
            public int? gender { get; set; }

            [StringLength(50)]
            public string? contact_no { get; set; }

            [StringLength(255)]
            public string? email { get; set; }

            public string? address { get; set; }
            public int? company_id { get; set; }
            public int? employment_status { get; set; }
            public int? position_id { get; set; }
            public int? department_id { get; set; }
            public DateTime? start_date { get; set; }

            [StringLength(50)]
            public string? sss { get; set; }

            [StringLength(50)]
            public string? tin { get; set; }

            [StringLength(50)]
            public string? philhealth { get; set; }

            [StringLength(50)]
            public string? pagibig { get; set; }

            public bool? active_status { get; set; }
            public object? city_code { get; set; }
        }

        public class HeadcountReportModel
        {
            public long autoid { get; set; }
            public string empid { get; set; }
            public string full_name { get; set; }
            public string fname { get; set; }
            public string mname { get; set; }
            public string sname { get; set; }
            public int? age { get; set; }
            public DateTime? birth_date { get; set; }
            public int? civil_status { get; set; }
            public int? gender { get; set; }
            public string contact_no { get; set; }
            public string email { get; set; }
            public string address { get; set; }
            public int? company_id { get; set; }
            public string company_name { get; set; }
            public int? employment_status { get; set; }
            public string employment_status_name { get; set; }
            public int? position_id { get; set; }
            public string position_name { get; set; }
            public int? department_id { get; set; }
            public string department_name { get; set; }
            public DateTime? start_date { get; set; }
            public string sss { get; set; }
            public string tin { get; set; }
            public string philhealth { get; set; }
            public string pagibig { get; set; }
            public bool? active_status { get; set; }
            public bool record_status { get; set; }
            public DateTime? date_registered { get; set; }
            public object city_code { get; set; }
            public string city_name { get; set; }

            // Calculated fields
            public int service_length_years { get; set; }
            public int service_length_months { get; set; }
            public int service_length_days { get; set; }
            public string service_length_formatted { get; set; }
        }
        // Models/Requests/CreatePositionDepartmentMappingRequest.cs
        public class CreatePositionDepartmentMappingRequest
        {
            public int PositionId { get; set; }
            public int DepartmentId { get; set; }
            public int CompanyId { get; set; }
            public DateTime? EffectiveDate { get; set; }
            public string CreatedBy { get; set; }
        }

        // Models/Requests/UpdatePositionDepartmentMappingRequest.cs
        public class UpdatePositionDepartmentMappingRequest
        {
            public int? DepartmentId { get; set; }
            public DateTime? EffectiveDate { get; set; }
            public string UpdatedBy { get; set; }
        }
        public class HeadcountSummaryModel
        {
            public int total_headcount { get; set; }
            public int company_id { get; set; }
            public string company_name { get; set; }
            public int regular_employees { get; set; }
            public int probationary_employees { get; set; }
            public int contractual_employees { get; set; }
            public int male_employees { get; set; }
            public int female_employees { get; set; }
            public double average_service_length_years { get; set; }
            public int employees_less_than_1_year { get; set; }
            public int employees_1_to_5_years { get; set; }
            public int employees_5_to_10_years { get; set; }
            public int employees_more_than_10_years { get; set; }
        }


    }
}
