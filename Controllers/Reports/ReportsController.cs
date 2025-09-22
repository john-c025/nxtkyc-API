using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ExcelDataReader;
using System.Data;
using System.Text;
using CoreHRAPI.Models.Reports;
using static CoreHRAPI.Models.Reports.ReportsModel;
using CoreHRAPI.Data;
using System.Data.SqlTypes;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using CoreHRAPI.Helpers;
using CoreHRAPI.Models.BIS;
using CoreHRAPI.Utilities;
using CoreHRAPI.Models.Global;
using System.Text.RegularExpressions;
using System.Globalization;
using CoreHRAPI.Models.Configuration;

namespace CoreHRAPI.Controllers.Reports
{
    [ApiController]
    [Route("api/v1/reports")]


    public class ReportsController : ControllerBase
    {
        const string m = "api/v1/reports";
        private readonly ILogger<ReportsController> _logger;
        private readonly CIRSMasterListRepository _reportsRepository;
        private readonly GlobalRepository _globalRepository;
        private readonly Custom _c = new();
        public ReportsController(ILogger<ReportsController> logger, CIRSMasterListRepository reportsRepository, GlobalRepository globalRepository)
        {
            _logger = logger;
            _reportsRepository = reportsRepository;
            _globalRepository = globalRepository;

        }

        [HttpPost("upload-employee-masterfile")]
        public async Task<IActionResult> UploadEmployeeMasterFile(IFormFile file, [FromQuery] long companyid, [FromQuery] string userid, int page = 1, int pageSize = 10)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid file type. Must be Excel");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var reader = ExcelReaderFactory.CreateReader(stream);
            var result = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });

            var table = result.Tables.Cast<DataTable>().FirstOrDefault(t => t.TableName.ToLower().Contains("employee database"));
            if (table == null)
                return BadRequest("No sheet named 'Employee Database' found.");

            var employees = new List<EmployeeMasterFileModel>();
            foreach (DataRow row in table.Rows)
            {
                if (row.ItemArray.All(c => c == DBNull.Value || string.IsNullOrWhiteSpace(c?.ToString())))
                    continue;

                employees.Add(new EmployeeMasterFileModel
                {
                    full_name = SafeGetString(row["FULL NAME"]),
                    sname = SafeGetString(row["LAST NAME"]),
                    fname = SafeGetString(row["FIRST NAME"]),
                    mname = SafeGetString(row["MIDDLE NAME"]),
                    active_status = SafeGetString(row["STATUS"]),
                    age = SafeGetInt32(row["AGE"]),
                    birthdate = SafeGetDateTime(row["BIRTHDATE"]),
                    civil_status = SafeGetString(row["CIVIL STATUS"]),
                    gender = SafeGetString(row["GENDER"]),
                    address = SafeGetString(row["ADDRESS"]),
                    contact_no = SafeGetString(row["CONTACT NO."]),
                    email = SafeGetString(row["EMAIL"]),
                    start_date = SafeGetDateTime(row["START DATE"]),
                    service_length_months = SafeGetInt32(row["LENGTH OF SERVICE (in Months)"]),
                    employee_status_type = SafeGetString(row["EMPLOYMENT STATUS"]),
                    position_name = SafeGetString(row["JOB POSITION"]),
                    department_id = SafeGetString(row["DEPARTMENT"]),
                    sss = SafeGetString(row["SSS"]),
                    tin = SafeGetString(row["TIN"]),
                    philhealth = SafeGetString(row["PHILHEALTH"]),
                    pagibig = SafeGetString(row["PAG-IBIG"]),
                    coc_attendance = SafeGetString(row["Code of Conduct Attendance"]),
                    coc_acknowledgement = SafeGetString(row["Code of Conduct Acknowledgment"]),
                    disciplinary_action_description = SafeGetString(row["Description of Disciplinary Management Case/s"]),
                    disciplinary_action = SafeGetString(row["Disciplinary Action"]),
                    disciplinary_action_effectivity = SafeGetDateTime(row["Effectivity Date of Disciplinary Action"]),
                    discipinary_action_status = SafeGetString(row["Status of Disciplinary Action"]),
                    labor_case = SafeGetString(row["Labor Case"]),
                    labor_case_status = SafeGetString(row["Reason for Labor Case"]),
                    work_location = SafeGetString(row["WORK LOCATION"])
                });
            }

            HttpContext.Session.SetObjectAsJson("EmployeeData", employees);

            // log attempt
            await _globalRepository.LogUploadAttemptAsync(userid, companyid, employees);

            return Ok(new
            {
                Message = "Employee masterfile processed successfully",
                TotalRecords = employees.Count,
                Data = employees.Skip((page - 1) * pageSize).Take(pageSize)
            });
        }

        [HttpPost("upload-employee-masterfile/accept")]
        public async Task<IActionResult> InsertEmployeeMasterFile([FromQuery] string userid, [FromQuery] long companyid, [FromQuery] int? targetYear = null, [FromQuery] int? targetMonth = null)
        {
            var employees = HttpContext.Session.GetObjectFromJson<List<EmployeeMasterFileModel>>("EmployeeData");
            if (employees == null || !employees.Any())
                return BadRequest("No data available for insertion");

            var inserted = await _reportsRepository.UpsertEmployeeMasterFileAsync(employees, userid, companyid, targetYear, targetMonth);

            // log confirm
            await _globalRepository.LogUploadConfirmAsync(userid, companyid, employees);

            return Ok(new { Message = "Data inserted", Inserted = inserted });
        }

        [HttpPost("upload-employee-masterfile/process")]
        public async Task<IActionResult> ProcessEmployeeMasterFile([FromQuery] string userid, [FromQuery] long companyid)
        {
            if (string.IsNullOrWhiteSpace(userid))
                return BadRequest("UserId is required");

            if (companyid <= 0)
                return BadRequest("CompanyId is required and must be greater than 0");

            try
            {
                var result = await _reportsRepository.ProcessEmployeeMasterFileMigrationAsync(userid, companyid);

                // log process
                await _globalRepository.LogUploadProcessAsync(userid, companyid, new { Result = result });

                return Ok(new { Message = "Employee Masterfile successfully migrated", Result = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing employee masterfile migration for company {CompanyId}", companyid);
                return StatusCode(500, "An error occurred while processing migration.");
            }
        }







        [HttpGet("load-all-companies")]
        public async Task<IActionResult> LoadAllCompanies()
        {
            try
            {
                // Assuming you have a repository to fetch the master list

                var companies = await _reportsRepository.GetAllCompanies();

                if (companies == null || !companies.Any())
                {
                    return NotFound("No companies data found");
                }


                return Ok(new
                {
                    Message = "Master list loaded successfully",
                    Status = 200,
                    Data = companies
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading companies list");
                return StatusCode(500, "Error loading the companies list");
            }
        }


        private bool IsSubTableStart(DataRow row)
        {
            return row["MONTH"] == DBNull.Value || string.IsNullOrWhiteSpace(row["MONTH"].ToString());
        }

        // ==== PATCH: Helper to parse Age column ====
        private int ParseAge(string ageValue)
        {
            if (string.IsNullOrWhiteSpace(ageValue))
                return 0;

            // e.g. "4 - 6th month up" ? "4", "7th month up" ? "7"
            var match = Regex.Match(ageValue, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int result))
                return result;

            return 0;
        }
        private bool IsRowEmpty(DataRow row)
        {
            foreach (var item in row.ItemArray)
            {
                if (item != DBNull.Value && !string.IsNullOrWhiteSpace(item.ToString()))
                {
                    return false;
                }
            }
            return true;
        }





        private string SafeGetString(object value)
        {
            return value == DBNull.Value ? null : value?.ToString();
        }

        private int SafeGetInt32(object value)
        {
            if (value == DBNull.Value || value == null)
                return 0;

            if (int.TryParse(value.ToString(), out int result))
            {
                return result;
            }
            else
            {
                _logger.LogWarning($"Unable to convert '{value}' to int.");
                return 0;
            }
        }
        private decimal SafeGetDecimal(object value)
        {
            if (value == DBNull.Value || value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return 0;

            // Remove any non-numeric characters
            var cleanedValue = new string(value.ToString().Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());

            if (decimal.TryParse(cleanedValue, out decimal result))
            {
                return result;
            }
            else
            {
                _logger.LogWarning($"Unable to convert '{value}' to decimal.");
                return 0;
            }
        }

        private DateTime? SafeGetDateTime(object value)
        {
            if (value == DBNull.Value || string.IsNullOrWhiteSpace(value.ToString()))
                return null;

            // Excel numeric date format
            if (double.TryParse(value.ToString(), out double oaDate))
            {
                try
                {
                    DateTime excelDate = DateTime.FromOADate(oaDate);
                    return excelDate < (DateTime)SqlDateTime.MinValue ? (DateTime?)null : excelDate;
                }
                catch
                {
                    // continue to try string parsing
                }
            }

            string[] formats = { "MM/dd/yyyy", "M/d/yyyy", "yyyy-MM-dd", "dd/MM/yyyy" };
            var culture = CultureInfo.InvariantCulture;

            if (DateTime.TryParseExact(value.ToString(), formats, culture, DateTimeStyles.None, out DateTime dateValue))
            {
                return dateValue < (DateTime)SqlDateTime.MinValue ? (DateTime?)null : dateValue;
            }
            else if (DateTime.TryParse(value.ToString(), out dateValue))
            {
                return dateValue < (DateTime)SqlDateTime.MinValue ? (DateTime?)null : dateValue;
            }
            else
            {
                _logger.LogWarning($"Unable to convert '{value}' to DateTime.");
                return null;
            }
        }

        // Add these methods to your ReportsController

        // Updated controller methods to use empid instead of autoid
        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployees(
    [FromQuery] int? companyId = null,
    [FromQuery] bool? isActive = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string search = null)
        {
            try
            {
                // Validate pagination parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var employees = await _reportsRepository.GetAllEmployeesAsync(companyId, isActive, page, pageSize, search);
                var totalCount = await _reportsRepository.GetEmployeeCountAsync(companyId, isActive, search);
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                return Ok(new
                {
                    success = true,
                    message = "Employees loaded successfully",
                    totalRecords = totalCount,
                    currentPage = page,
                    pageSize = pageSize,
                    totalPages = totalPages,
                    data = employees,
                    filters = new
                    {
                        companyId = companyId,
                        isActive = isActive,
                        search = search
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employees for company {CompanyId}, page {Page}", companyId, page);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error loading employees",
                    error = ex.Message
                });
            }
        }

        [HttpGet("employees/specific")]
        public async Task<IActionResult> GetEmployeeByEmpId([FromQuery] string empid)
        {
            try
            {
                var employee = await _reportsRepository.GetEmployeeByEmpIdAsync(empid);

                if (employee == null)
                {
                    return NotFound("Employee not found");
                }

                return Ok(new
                {
                    Message = "Employee loaded successfully",
                    Data = employee
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employee");
                return StatusCode(500, "Error loading employee");
            }
        }

        [HttpPost("employees/manual-add")]
        public async Task<IActionResult> CreateEmployee([FromBody] EmployeeHRDetailsCreateModel employee, [FromQuery] string userid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userid))
                {
                    return BadRequest("User ID is required");
                }

                // No need to check for existing employee ID since it's generated automatically
                var newEmployeeId = await _reportsRepository.CreateEmployeeAsync(employee, userid);

                // Log the manual employee addition
                await _globalRepository.LogManualEmployeeAddAsync(userid, newEmployeeId, employee);

                return Ok(new
                {
                    Message = "Employee created successfully",
                    EmployeeId = newEmployeeId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee");
                return StatusCode(500, "Error creating employee");
            }
        }
        [HttpPut("employees/edit")]
        public async Task<IActionResult> UpdateEmployee([FromQuery] string empid, [FromBody] EmployeeHRDetailsUpdateModel employee, [FromQuery] string userid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userid))
                {
                    return BadRequest("User ID is required");
                }

                if (string.IsNullOrWhiteSpace(empid))
                {
                    return BadRequest("Employee ID is required");
                }

                // Set the empid from the route parameter
                employee.empid = empid;

                var success = await _reportsRepository.UpdateEmployeeByEmpIdAsync(employee, userid);

                if (!success)
                {
                    return NotFound("Employee not found or could not be updated");
                }

                return Ok(new
                {
                    Message = "Employee updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee");
                return StatusCode(500, "Error updating employee");
            }
        }

        [HttpDelete("employees/disable")]
        public async Task<IActionResult> DeleteEmployee([FromQuery] string empid, [FromQuery] string userid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userid))
                {
                    return BadRequest("User ID is required");
                }

                if (string.IsNullOrWhiteSpace(empid))
                {
                    return BadRequest("Employee ID is required");
                }

                var success = await _reportsRepository.DeleteEmployeeByEmpIdAsync(empid, userid);

                if (!success)
                {
                    return NotFound("Employee not found or could not be deleted");
                }

                return Ok(new
                {
                    Message = "Employee deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee");
                return StatusCode(500, "Error deleting employee");
            }
        }





        // Reports

        [HttpGet("city-distribution")]
        public async Task<IActionResult> GetCityDistribution(
            [FromQuery] int? companyId = null,
            [FromQuery] DateTime? asOfDate = null)
        {
            try
            {
                var cityDistribution = await _reportsRepository.GetHeadcountByCityDistributionAsync(companyId, asOfDate);

                return Ok(new
                {
                    success = true,
                    message = "City distribution retrieved successfully",
                    data = cityDistribution,
                    report_date = asOfDate ?? DateTime.UtcNow,
                    company_id = companyId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving city distribution for company {CompanyId}", companyId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving city distribution",
                    error = ex.Message
                });
            }
        }

        [HttpGet("city-distribution-summary")]
        public async Task<IActionResult> GetCityDistributionSummary(
            [FromQuery] int? companyId = null,
            [FromQuery] DateTime? asOfDate = null)
        {
            try
            {
                var summary = await _reportsRepository.GetCityDistributionSummaryAsync(companyId, asOfDate);

                return Ok(new
                {
                    success = true,
                    message = "City distribution summary retrieved successfully",
                    data = summary,
                    report_date = asOfDate ?? DateTime.UtcNow,
                    company_id = companyId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving city distribution summary for company {CompanyId}", companyId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving city distribution summary",
                    error = ex.Message
                });
            }
        }

        [HttpGet("top-cities")]
        public async Task<IActionResult> GetTopCitiesByEmployeeCount(
            [FromQuery] int? companyId = null,
            [FromQuery] int topCount = 10,
            [FromQuery] DateTime? asOfDate = null)
        {
            try
            {
                var topCities = await _reportsRepository.GetTopCitiesByEmployeeCountAsync(companyId, topCount, asOfDate);

                return Ok(new
                {
                    success = true,
                    message = "Top cities by employee count retrieved successfully",
                    data = topCities,
                    report_date = asOfDate ?? DateTime.UtcNow,
                    company_id = companyId,
                    top_count = topCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top cities for company {CompanyId}", companyId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving top cities",
                    error = ex.Message
                });
            }
        }

        [HttpGet("city-statistics")]
        public async Task<IActionResult> GetCityStatistics(
            [FromQuery] int? companyId = null,
            [FromQuery] DateTime? asOfDate = null)
        {
            try
            {
                var cityDistribution = await _reportsRepository.GetHeadcountByCityDistributionAsync(companyId, asOfDate);
                var topCities = await _reportsRepository.GetTopCitiesByEmployeeCountAsync(companyId, 5, asOfDate);

                // Prepare data for charts
                var cityChart = cityDistribution.Select(c => new
                {
                    city = c.city_name,
                    employee_count = c.employee_count,
                    company = c.company_name
                }).ToList();

                var topCitiesChart = topCities.Select(tc => new
                {
                    city = tc.city_name,
                    employee_count = tc.employee_count,
                    companies_count = tc.companies_count
                }).ToList();

                return Ok(new
                {
                    success = true,
                    message = "City statistics retrieved successfully",
                    data = new
                    {
                        city_distribution = cityChart,
                        top_cities = topCitiesChart,
                        total_cities = cityDistribution.Select(c => c.city_name).Distinct().Count(),
                        total_employees = cityDistribution.Sum(c => c.employee_count)
                    },
                    report_date = asOfDate ?? DateTime.UtcNow,
                    company_id = companyId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving city statistics for company {CompanyId}", companyId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving city statistics",
                    error = ex.Message
                });
            }
        }

        [HttpGet("active-headcount")]
        public async Task<IActionResult> GetActiveHeadcountReport(
    [FromQuery] int? companyId = null,
    [FromQuery] DateTime? asOfDate = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    [FromQuery] string search = null,
    [FromQuery] int? employmentStatus = null,
    [FromQuery] int? departmentId = null,
    [FromQuery] bool includeDepartments = false) // New parameter
        {
            try
            {
                var employees = await _reportsRepository.GetActiveHeadcountReportAsync(companyId, asOfDate, includeDepartments);

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    employees = employees.Where(e =>
                        (e.full_name?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                        (e.empid?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                        (e.email?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                    );
                }

                if (employmentStatus.HasValue)
                {
                    employees = employees.Where(e => e.employment_status == employmentStatus.Value);
                }

                if (departmentId.HasValue && includeDepartments)
                {
                    employees = employees.Where(e => e.department_id == departmentId.Value);
                }

                var totalCount = employees.Count();
                var pagedEmployees = employees
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new
                    {
                        e.autoid,
                        e.empid,
                        e.full_name,
                        e.fname,
                        e.mname,
                        e.sname,
                        e.age,
                        e.birth_date,
                        e.contact_no,
                        e.email,
                        e.address,
                        e.company_name,
                        e.employment_status_name,
                        e.position_name,
                        e.department_name,
                        e.start_date,
                        e.sss,
                        e.tin,
                        e.philhealth,
                        e.pagibig,
                        e.city_name,
                        gender = e.gender == 1 ? "Male" : e.gender == 2 ? "Female" : "Unknown",
                        service_length = new
                        {
                            years = e.service_length_years,
                            months = e.service_length_months,
                            days = e.service_length_days,
                            formatted = e.service_length_formatted
                        }
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = "Active headcount report retrieved successfully",
                    data = pagedEmployees,
                    pagination = new
                    {
                        current_page = page,
                        page_size = pageSize,
                        total_count = totalCount,
                        total_pages = (int)Math.Ceiling((double)totalCount / pageSize),
                        has_next = page < (int)Math.Ceiling((double)totalCount / pageSize),
                        has_previous = page > 1
                    },
                    filters = new
                    {
                        company_id = companyId,
                        as_of_date = asOfDate ?? DateTime.UtcNow,
                        search = search,
                        employment_status = employmentStatus,
                        department_id = departmentId,
                        include_departments = includeDepartments
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active headcount report for company {CompanyId}", companyId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving active headcount report",
                    error = ex.Message
                });
            }
        }

        [HttpGet("positions")]
        public async Task<IActionResult> GetPositionsByCompanyId(
            [FromQuery] long companyId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] bool? status = null)
        {
            try
            {
                var positions = await _reportsRepository.GetPositionsByCompanyIdAsync(companyId);

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    positions = positions.Where(p =>
                        p.position_desc.Contains(search, StringComparison.OrdinalIgnoreCase)
                    );
                }

                if (status.HasValue)
                {
                    positions = positions.Where(p => p.status == status.Value);
                }

                var totalCount = positions.Count();
                var pagedPositions = positions
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = "Positions retrieved successfully",
                    data = pagedPositions,
                    pagination = new
                    {
                        current_page = page,
                        page_size = pageSize,
                        total_count = totalCount,
                        total_pages = (int)Math.Ceiling((double)totalCount / pageSize),
                        has_next = page < (int)Math.Ceiling((double)totalCount / pageSize),
                        has_previous = page > 1
                    },
                    filters = new
                    {
                        company_id = companyId,
                        search = search,
                        status = status
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving positions for company {CompanyId}", companyId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving positions",
                    error = ex.Message
                });
            }
        }

        [HttpGet("headcount-summary")]
        public async Task<IActionResult> GetHeadcountSummary(
            [FromQuery] int? companyId = null,
            [FromQuery] DateTime? asOfDate = null)
        {
            try
            {
                var summary = await _reportsRepository.GetHeadcountSummaryAsync(companyId, asOfDate);

                return Ok(new
                {
                    success = true,
                    message = "Headcount summary retrieved successfully",
                    data = summary.Select(s => new
                    {
                        s.company_id,
                        s.company_name,
                        s.total_headcount,
                        employment_breakdown = new
                        {
                            regular = s.regular_employees,
                            probationary = s.probationary_employees,
                            contractual = s.contractual_employees
                        },
                        gender_breakdown = new
                        {
                            male = s.male_employees,
                            female = s.female_employees
                        },
                        service_length_breakdown = new
                        {
                            less_than_1_year = s.employees_less_than_1_year,
                            one_to_five_years = s.employees_1_to_5_years,
                            five_to_ten_years = s.employees_5_to_10_years,
                            more_than_ten_years = s.employees_more_than_10_years
                        },
                        average_service_length_years = s.average_service_length_years
                    }),
                    report_date = asOfDate ?? DateTime.UtcNow,
                    company_id = companyId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving headcount summary for company {CompanyId}", companyId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving headcount summary",
                    error = ex.Message
                });
            }
        }

        [HttpGet("headcount-statistics")]
        public async Task<IActionResult> GetHeadcountStatistics(
            [FromQuery] int? companyId = null,
            [FromQuery] DateTime? asOfDate = null)
        {
            try
            {
                var employees = await _reportsRepository.GetActiveHeadcountReportAsync(companyId, asOfDate);
                var summary = await _reportsRepository.GetHeadcountSummaryAsync(companyId, asOfDate);

                // Prepare data for charts
                var employmentStatusChart = summary.SelectMany(s => new[]
                {
            new { label = "Regular", value = s.regular_employees, company = s.company_name },
            new { label = "Probationary", value = s.probationary_employees, company = s.company_name },
            new { label = "Contractual", value = s.contractual_employees, company = s.company_name }
        }).GroupBy(x => x.label)
                  .Select(g => new { label = g.Key, value = g.Sum(x => x.value) })
                  .ToList();

                var genderChart = summary.SelectMany(s => new[]
                {
            new { label = "Male", value = s.male_employees, company = s.company_name },
            new { label = "Female", value = s.female_employees, company = s.company_name }
        }).GroupBy(x => x.label)
                  .Select(g => new { label = g.Key, value = g.Sum(x => x.value) })
                  .ToList();

                var serviceLengthChart = summary.SelectMany(s => new[]
                {
            new { label = "< 1 Year", value = s.employees_less_than_1_year, company = s.company_name },
            new { label = "1-5 Years", value = s.employees_1_to_5_years, company = s.company_name },
            new { label = "5-10 Years", value = s.employees_5_to_10_years, company = s.company_name },
            new { label = "> 10 Years", value = s.employees_more_than_10_years, company = s.company_name }
        }).GroupBy(x => x.label)
                  .Select(g => new { label = g.Key, value = g.Sum(x => x.value) })
                  .ToList();

                return Ok(new
                {
                    success = true,
                    message = "Headcount statistics retrieved successfully",
                    data = new
                    {
                        employment_status_chart = employmentStatusChart,
                        gender_chart = genderChart,
                        service_length_chart = serviceLengthChart,
                        total_employees = employees.Count(),
                        companies_count = summary.Count(),
                        average_service_length = summary.Any() ? summary.Average(s => s.average_service_length_years) : 0
                    },
                    report_date = asOfDate ?? DateTime.UtcNow,
                    company_id = companyId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving headcount statistics for company {CompanyId}", companyId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving headcount statistics",
                    error = ex.Message
                });
            }
        }

        [HttpGet("filter-options")]
        public async Task<IActionResult> GetFilterOptions([FromQuery] int? companyId = null)
        {
            try
            {
                var employmentStatusOptions = new[]
                {
            new { value = 1, label = "Regular" },
            new { value = 2, label = "Probationary" },
            new { value = 3, label = "Contractual" },
            new { value = 4, label = "Part-time" }
        };

                var genderOptions = new[]
                {
            new { value = 1, label = "Male" },
            new { value = 2, label = "Female" }
        };

                var companies = await _reportsRepository.GetCompaniesAsync();
                var departments = await _reportsRepository.GetDepartmentsAsync(companyId);

                return Ok(new
                {
                    success = true,
                    message = "Filter options retrieved successfully",
                    data = new
                    {
                        employment_status = employmentStatusOptions,
                        gender = genderOptions,
                        companies = companies.Select(c => new { value = c.company_id, label = c.company_name }),
                        departments = departments.Select(d => new { value = d.department_id, label = d.department_name, company_id = d.companyid })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving filter options for company {CompanyId}", companyId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving filter options",
                    error = ex.Message
                });
            }
        }

        // Add new endpoint for position-department mappings management
        [HttpGet("position-department-mappings")]
        public async Task<IActionResult> GetPositionDepartmentMappings([FromQuery] int? companyId = null)
        {
            try
            {
                var mappings = await _reportsRepository.GetPositionDepartmentMappingsAsync(companyId);

                return Ok(new
                {
                    success = true,
                    message = "Position-department mappings retrieved successfully",
                    data = mappings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving position-department mappings for company {CompanyId}", companyId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving position-department mappings",
                    error = ex.Message
                });
            }
        }

        // Add endpoint to create position-department mapping
        [HttpPost("position-department-mappings")]
        public async Task<IActionResult> CreatePositionDepartmentMapping([FromBody] CreatePositionDepartmentMappingRequest request)
        {
            try
            {
                if (request == null || request.PositionId <= 0 || request.DepartmentId <= 0 || request.CompanyId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid request data. PositionId, DepartmentId, and CompanyId are required."
                    });
                }

                var mappingId = await _reportsRepository.CreatePositionDepartmentMappingAsync(
                    request.PositionId,
                    request.DepartmentId,
                    request.CompanyId,
                    request.EffectiveDate,
                    request.CreatedBy
                );

                return Ok(new
                {
                    success = true,
                    message = "Position-department mapping created successfully",
                    data = new { mapping_id = mappingId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating position-department mapping");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error creating position-department mapping",
                    error = ex.Message
                });
            }
        }

        // Add endpoint to update position-department mapping
        [HttpPut("position-department-mappings/{mappingId}")]
        public async Task<IActionResult> UpdatePositionDepartmentMapping(long mappingId, [FromBody] UpdatePositionDepartmentMappingRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid request data."
                    });
                }

                var success = await _reportsRepository.UpdatePositionDepartmentMappingAsync(
                    mappingId,
                    request.DepartmentId,
                    request.EffectiveDate,
                    request.UpdatedBy
                );

                if (!success)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Position-department mapping not found or could not be updated."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Position-department mapping updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating position-department mapping {MappingId}", mappingId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating position-department mapping",
                    error = ex.Message
                });
            }
        }


        /// [ANALYTICS ]
        ///  <summary>
        /// [ANALYTICS ]
        /// 
        /// 
        /// 
        /// 
        /// 
        /// 
        /// 
        /// 
        /// 
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        [HttpGet("analytics/monthly-statistics")]
        public async Task<IActionResult> GetMonthlyEmployeeStatistics(
    [FromQuery] int? companyId = null,
    [FromQuery] int year = 0,
    [FromQuery] int month = 0)
        {
            try
            {
                var statistics = await _reportsRepository.GetMonthlyEmployeeStatisticsAsync(companyId, year, month);

                var targetYear = year > 0 ? year : DateTime.UtcNow.Year;
                var targetMonth = month > 0 ? month : DateTime.UtcNow.Month;

                return Ok(new
                {
                    success = true,
                    message = "Monthly employee statistics retrieved successfully",
                    data = statistics,
                    period = new
                    {
                        year = targetYear,
                        month = targetMonth,
                        month_name = new DateTime(targetYear, targetMonth, 1).ToString("MMMM")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monthly statistics for company {CompanyId}", companyId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving monthly statistics",
                    error = ex.Message
                });
            }
        }

        [HttpGet("analytics/company-monthly-statistics")]
        public async Task<IActionResult> GetCompanyMonthlyStatistics(
            [FromQuery] int companyId,
            [FromQuery] int year = 0,
            [FromQuery] int month = 0)
        {
            try
            {
                if (companyId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Company ID is required"
                    });
                }

                var statistics = await _reportsRepository.GetCompanyMonthlyStatisticsAsync(companyId, year, month);

                var targetYear = year > 0 ? year : DateTime.UtcNow.Year;
                var targetMonth = month > 0 ? month : DateTime.UtcNow.Month;

                if (statistics == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "No data found for the specified company and period"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Company monthly statistics retrieved successfully",
                    data = statistics,
                    period = new
                    {
                        year = targetYear,
                        month = targetMonth,
                        month_name = new DateTime(targetYear, targetMonth, 1).ToString("MMMM")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company monthly statistics for company {CompanyId}", companyId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving company monthly statistics",
                    error = ex.Message
                });
            }
        }

        [HttpGet("analytics/attrition-trends")]
        public async Task<IActionResult> GetAttritionTrends(
            [FromQuery] int? companyId = null,
            [FromQuery] int years = 2)
        {
            try
            {
                var trends = await _reportsRepository.GetAttritionTrendsAsync(companyId, years);

                return Ok(new
                {
                    success = true,
                    message = "Attrition trends retrieved successfully",
                    data = trends,
                    period_years = years
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving attrition trends for company {CompanyId}", companyId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving attrition trends",
                    error = ex.Message
                });
            }
        }

        [HttpGet("analytics/monthly-resignations")]
        public async Task<IActionResult> GetMonthlyResignations(
            [FromQuery] int? companyId = null,
            [FromQuery] int year = 0,
            [FromQuery] int month = 0)
        {
            try
            {
                var resignations = await _reportsRepository.GetMonthlyResignationsAsync(companyId, year, month);

                var targetYear = year > 0 ? year : DateTime.UtcNow.Year;
                var targetMonth = month > 0 ? month : DateTime.UtcNow.Month;

                return Ok(new
                {
                    success = true,
                    message = "Monthly resignations retrieved successfully",
                    data = resignations,
                    period = new
                    {
                        year = targetYear,
                        month = targetMonth,
                        month_name = new DateTime(targetYear, targetMonth, 1).ToString("MMMM")
                    },
                    total_resignations = resignations.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monthly resignations for company {CompanyId}", companyId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving monthly resignations",
                    error = ex.Message
                });
            }
        }
        [HttpGet("analytics/attrition-summary")]
        public async Task<IActionResult> GetAttritionSummary(
    [FromQuery] int? companyId = null,
    [FromQuery] int year = 0,
    [FromQuery] int month = 0)
        {
            try
            {
                var statistics = await _reportsRepository.GetMonthlyEmployeeStatisticsAsync(companyId, year, month);
                var resignations = await _reportsRepository.GetMonthlyResignationsAsync(companyId, year, month);

                var targetYear = year > 0 ? year : DateTime.UtcNow.Year;
                var targetMonth = month > 0 ? month : DateTime.UtcNow.Month;

                // Calculate summary metrics
                var totalActiveAtStart = statistics.Sum(s => s.active_at_start);
                var totalActiveAtEnd = statistics.Sum(s => s.active_at_end);
                var totalResigned = statistics.Sum(s => s.resigned_during_month);
                var totalNewHires = statistics.Sum(s => s.new_hires_during_month);
                var averageEmployees = statistics.Sum(s => s.average_employees);

                var overallAttritionRate = averageEmployees > 0 ?
                    Math.Round((double)(totalResigned / averageEmployees) * 100, 2) : 0;

                var overallHiringRate = averageEmployees > 0 ?
                    Math.Round((double)(totalNewHires / averageEmployees) * 100, 2) : 0;

                return Ok(new
                {
                    success = true,
                    message = "Attrition summary retrieved successfully",
                    data = new
                    {
                        summary = new
                        {
                            total_active_at_start = totalActiveAtStart,
                            total_active_at_end = totalActiveAtEnd,
                            total_resigned = totalResigned,
                            total_new_hires = totalNewHires,
                            average_employees = Math.Round((double)averageEmployees, 2),
                            overall_attrition_rate = overallAttritionRate,
                            overall_hiring_rate = overallHiringRate,
                            net_change = totalActiveAtEnd - totalActiveAtStart
                        },
                        by_company = statistics,
                        resignations_detail = resignations
                    },
                    period = new
                    {
                        year = targetYear,
                        month = targetMonth,
                        month_name = new DateTime(targetYear, targetMonth, 1).ToString("MMMM")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving attrition summary for company {CompanyId}", companyId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving attrition summary",
                    error = ex.Message
                });
            }
        }

    }
}







