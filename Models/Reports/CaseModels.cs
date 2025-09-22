namespace CoreHRAPI.Models.Reports
{
    public class CaseCreateDto
    {
        public string DebtorName { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }

    public class CaseUpdateDto
    {
        public string Status { get; set; }
        public string Notes { get; set; }
    }

    public class CaseAssignmentDto
    {
        public int CollectorId { get; set; }
    }
}
