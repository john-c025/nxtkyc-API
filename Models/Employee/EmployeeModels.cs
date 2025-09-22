using System.ComponentModel.DataAnnotations;

namespace CoreHRAPI.Models.BIS
{
    public class EmployeeModels
    {
    }

    public class EmployeeTransferModel
    {
        [Required]
        public string empid { get; set; }

        [Required]
        public int from_company { get; set; }

        [Required]
        public int to_company { get; set; }

        [Required]
        public DateTime effective_date { get; set; }

        public string remarks { get; set; }
    }

    public class EmployeePromotionModel
    {
        [Required]
        public string empid { get; set; }

        [Required]
        public int old_position { get; set; }

        [Required]
        public int new_position { get; set; }

        [Required]
        public DateTime effective_date { get; set; }

        public string remarks { get; set; }
    }
    public class EmployeeRegularizationModel
    {
        [Required]
        public string empid { get; set; }

        public string remarks { get; set; }

       
    }

    public class EmployeeOffboardingModel
    {
        [Required]
        public string empid { get; set; }

        [Required]
        public DateTime last_day { get; set; }

        [Required]
        public string reason { get; set; }

        public bool exit_interview_done { get; set; } = false;
    }

    public class EmployeeAppraisalModel
    {
        [Required]
        public string empid { get; set; }

        [Required]
        public DateTime appraisal_date { get; set; }

        [Required]
        public string appraisal_type { get; set; }

        public string remarks { get; set; }
    }

    public class EmploymentHistoryModel
    {
        public long autoid { get; set; }
        public string empid { get; set; }
        public int record_type { get; set; }
        public object json_payload { get; set; }
        public DateTime date_updated { get; set; }
        public bool status { get; set; }
    }

}
