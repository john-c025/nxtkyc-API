// Controllers/Reports/EmployeeController.cs
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CoreHRAPI.Models.Reports;
using CoreHRAPI.Data;
using Microsoft.Extensions.Logging;
using CoreHRAPI.Models.BIS;

namespace CoreHRAPI.Controllers.Reports
{
    [ApiController]
    [Route("api/v1/employee-lifecycle")]
    public class EmployeeController : ControllerBase
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly EmployeeRepository _employeeRepository;

        public EmployeeController(ILogger<EmployeeController> logger, EmployeeRepository employeeRepository)
        {
            _logger = logger;
            _employeeRepository = employeeRepository;
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> TransferEmployee([FromBody] EmployeeTransferModel transfer, [FromQuery] string userid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userid))
                {
                    return BadRequest("User ID is required");
                }

                var success = await _employeeRepository.TransferEmployeeAsync(transfer, userid);

                if (!success)
                {
                    return NotFound("Employee not found or could not be transferred");
                }

                return Ok(new
                {
                    Message = "Employee transferred successfully",
                    EmpId = transfer.empid,
                    FromCompany = transfer.from_company,
                    ToCompany = transfer.to_company,
                    EffectiveDate = transfer.effective_date
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring employee");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("promote")]
        public async Task<IActionResult> PromoteEmployee([FromBody] EmployeePromotionModel promotion, [FromQuery] string userid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userid))
                {
                    return BadRequest("User ID is required");
                }

                var success = await _employeeRepository.PromoteEmployeeAsync(promotion, userid);

                if (!success)
                {
                    return NotFound("Employee not found or could not be promoted");
                }

                return Ok(new
                {
                    Message = "Employee promoted successfully",
                    EmpId = promotion.empid,
                    OldPosition = promotion.old_position,
                    NewPosition = promotion.new_position,
                    EffectiveDate = promotion.effective_date
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting employee");
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPost("regularize")]
        public async Task<IActionResult> RegularizeEmployee([FromBody] EmployeeRegularizationModel regularization, [FromQuery] string userid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userid))
                {
                    return BadRequest("User ID is required");
                }

                var success = await _employeeRepository.RegularizeEmployeeAsync(regularization, userid);

                if (!success)
                {
                    return NotFound("Employee not found or could not be regularized");
                }

                return Ok(new
                {
                    Message = "Employee regularized successfully",
                    EmpId = regularization.empid,
                    RegularizationDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    Remarks = regularization.remarks
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regularizing employee");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("offboard")]
        public async Task<IActionResult> OffboardEmployee([FromBody] EmployeeOffboardingModel offboarding, [FromQuery] string userid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userid))
                {
                    return BadRequest("User ID is required");
                }

                var success = await _employeeRepository.OffboardEmployeeAsync(offboarding, userid);

                if (!success)
                {
                    return NotFound("Employee not found or could not be offboarded");
                }

                return Ok(new
                {
                    Message = "Employee offboarded successfully",
                    EmpId = offboarding.empid,
                    LastDay = offboarding.last_day,
                    Reason = offboarding.reason
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error offboarding employee");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("appraisal")]
        public async Task<IActionResult> AddEmployeeAppraisal([FromBody] EmployeeAppraisalModel appraisal, [FromQuery] string userid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userid))
                {
                    return BadRequest("User ID is required");
                }

                var success = await _employeeRepository.AddEmployeeAppraisalAsync(appraisal, userid);

                return Ok(new
                {
                    Message = "Employee appraisal added successfully",
                    EmpId = appraisal.empid,
                    AppraisalDate = appraisal.appraisal_date,
                    AppraisalType = appraisal.appraisal_type
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding employee appraisal");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("history/{empid}")]
        public async Task<IActionResult> GetEmploymentHistory(string empid)
        {
            try
            {
                var history = await _employeeRepository.GetEmploymentHistoryAsync(empid);

                return Ok(new
                {
                    Message = "Employment history retrieved successfully",
                    EmpId = empid,
                    History = history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employment history");
                return StatusCode(500, "Error retrieving employment history");
            }
        }

        [HttpGet("history/{empid}/type/{recordType}")]
        public async Task<IActionResult> GetEmploymentHistoryByType(string empid, int recordType)
        {
            try
            {
                var history = await _employeeRepository.GetEmploymentHistoryByTypeAsync(empid, recordType);

                return Ok(new
                {
                    Message = "Employment history retrieved successfully",
                    EmpId = empid,
                    RecordType = recordType,
                    History = history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employment history by type");
                return StatusCode(500, "Error retrieving employment history");
            }
        }







        //// Regularization
        ///

        [HttpGet("regularization/nearing")]
        public async Task<IActionResult> GetEmployeesNearingRegularization(
    [FromQuery] int? companyId = null,
    [FromQuery] int daysBeforeDeadline = 30)
        {
            try
            {
                var employees = await _employeeRepository.GetEmployeesNearingRegularizationAsync(companyId, daysBeforeDeadline);
                var statistics = await _employeeRepository.GetRegularizationStatisticsAsync(companyId);

                return Ok(new
                {
                    success = true,
                    message = "Employees nearing regularization retrieved successfully",
                    data = new
                    {
                        employees = employees,
                        statistics = statistics,
                        filters = new
                        {
                            companyId = companyId,
                            daysBeforeDeadline = daysBeforeDeadline
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees nearing regularization");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving employees nearing regularization",
                    error = ex.Message
                });
            }
        }

        [HttpGet("regularization/statistics")]
        public async Task<IActionResult> GetRegularizationStatistics([FromQuery] int? companyId = null)
        {
            try
            {
                var statistics = await _employeeRepository.GetRegularizationStatisticsAsync(companyId);

                return Ok(new
                {
                    success = true,
                    message = "Regularization statistics retrieved successfully",
                    data = statistics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving regularization statistics");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving regularization statistics",
                    error = ex.Message
                });
            }
        }

        [HttpGet("regularization/by-status")]
        public async Task<IActionResult> GetEmployeesByRegularizationStatus(
            [FromQuery] int? companyId = null,
            [FromQuery] string status = null)
        {
            try
            {
                // Validate status parameter
                var validStatuses = new[] { "OVERDUE", "DUE_SOON", "APPROACHING", "EARLY" };
                if (!string.IsNullOrEmpty(status) && !validStatuses.Contains(status.ToUpper()))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid status. Valid values are: OVERDUE, DUE_SOON, APPROACHING, EARLY"
                    });
                }

                var employees = await _employeeRepository.GetEmployeesByRegularizationStatusAsync(companyId, status?.ToUpper());

                return Ok(new
                {
                    success = true,
                    message = "Employees by regularization status retrieved successfully",
                    data = new
                    {
                        employees = employees,
                        filters = new
                        {
                            companyId = companyId,
                            status = status
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees by regularization status");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving employees by regularization status",
                    error = ex.Message
                });
            }
        }

        [HttpGet("regularization/dashboard")]
        public async Task<IActionResult> GetRegularizationDashboard([FromQuery] int? companyId = null)
        {
            try
            {
                var statistics = await _employeeRepository.GetRegularizationStatisticsAsync(companyId);
                var overdueEmployees = await _employeeRepository.GetEmployeesByRegularizationStatusAsync(companyId, "OVERDUE");
                var dueSoonEmployees = await _employeeRepository.GetEmployeesByRegularizationStatusAsync(companyId, "DUE_SOON");

                return Ok(new
                {
                    success = true,
                    message = "Regularization dashboard data retrieved successfully",
                    data = new
                    {
                        statistics = statistics,
                        overdue_employees = overdueEmployees.Take(10), // Limit to 10 for dashboard
                        due_soon_employees = dueSoonEmployees.Take(10), // Limit to 10 for dashboard
                        summary = new
                        {
                            total_probationary = statistics?.total_probationary ?? 0,
                            overdue_count = statistics?.overdue_count ?? 0,
                            due_soon_count = statistics?.due_soon_count ?? 0,
                            approaching_count = statistics?.approaching_count ?? 0,
                            early_count = statistics?.early_count ?? 0
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving regularization dashboard");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving regularization dashboard",
                    error = ex.Message
                });
            }
        }
    }
}