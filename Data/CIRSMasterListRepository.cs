using System.Data;
using System.Data.Common;
using System.Net.Http;
using Dapper;
using CoreHRAPI.Controllers.Reports;
using CoreHRAPI.Handlers;
using CoreHRAPI.Models.BIS;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static CoreHRAPI.Models.Reports.ReportsModel;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CoreHRAPI.Data
{
    public class CIRSMasterListRepository
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<ReportsController> _logger;
        private readonly HttpClient _httpClient;
        private readonly GeminiHelper _geminiHelper;
        private readonly IConfiguration _configuration;

        public CIRSMasterListRepository(ILogger<ReportsController> logger, DatabaseContext dbContext, HttpClient httpClient, GeminiHelper geminiHelper, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _logger = logger;
            _httpClient = httpClient;
            _geminiHelper = geminiHelper;
            _configuration = configuration;
        }

        // Helper method to set session variable for audit trail
        private async Task SetAuditUserIdAsync(IDbConnection connection, string userId)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                try
                {
                    await connection.ExecuteAsync("SELECT set_config('app.userid', @UserId, true);", new { UserId = userId.Trim() });

                    // Verify the setting was applied
                    var verifyUserId = await connection.QueryFirstOrDefaultAsync<string>("SELECT current_setting('app.userid', true);");
                    _logger.LogInformation("Session userid set to: {UserId}, verified as: {VerifiedUserId}", userId, verifyUserId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set session userid, will use database user for audit trail");
                }
            }
        }

        // Helper method for logging audit trails - following the working pattern
        private async Task LogAuditTrailAsync(
            IDbConnection connection,
            string tableName,
            long recordId,
            string action,
            string userid,
            object oldData = null,
            object newData = null,
            string remarks = null)
        {
            try
            {
                // Safely serialize the data using the same pattern as your working method
                string oldDataJson = null;
                string newDataJson = null;

                if (oldData != null)
                {
                    try
                    {
                        oldDataJson = System.Text.Json.JsonSerializer.Serialize(oldData);
                    }
                    catch (Exception serializeEx)
                    {
                        _logger.LogWarning(serializeEx, "Failed to serialize old data for record {RecordId}, using fallback", recordId);
                        oldDataJson = System.Text.Json.JsonSerializer.Serialize(new { error = "Serialization failed - partial data only" });
                    }
                }

                if (newData != null)
                {
                    try
                    {
                        newDataJson = System.Text.Json.JsonSerializer.Serialize(newData);
                    }
                    catch (Exception serializeEx)
                    {
                        _logger.LogWarning(serializeEx, "Failed to serialize new data for record {RecordId}, using fallback", recordId);
                        newDataJson = System.Text.Json.JsonSerializer.Serialize(new { error = "Serialization failed - partial data only" });
                    }
                }

                var query = @"
                    INSERT INTO core.sys_audit_trails (
                        table_name, 
                        record_id, 
                        action, 
                        userid, 
                        old_data, 
                        new_data, 
                        action_date, 
                        remarks
                    ) VALUES (
                        @table_name, 
                        @record_id, 
                        @action, 
                        @userid, 
                        @old_data::jsonb, 
                        @new_data::jsonb, 
                        NOW(), 
                        @remarks
                    )";

                var parameters = new
                {
                    table_name = tableName,
                    record_id = recordId,
                    action = action,
                    userid = userid ?? "unknown",
                    old_data = oldDataJson,
                    new_data = newDataJson,
                    remarks = remarks
                };

                await connection.ExecuteAsync(query, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging audit trail for {Action} on {TableName} record {RecordId}", action, tableName, recordId);
            }
        }

        public async Task<IEnumerable<CIRSCompanies>> GetAllCompanies()
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = "SELECT * FROM core.sys_affiliate_companies";
                return await connection.QueryAsync<CIRSCompanies>(query);
            });
        }

        public async Task<int> UpsertEmployeeMasterFileAsync(IEnumerable<EmployeeMasterFileModel> employees, string userid, long companyId, int? targetYear = null, int? targetMonth = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                // Set session variable for audit trail
                await SetAuditUserIdAsync(connection, userid);

                var query = @"
                INSERT INTO core.employee_masterfile (
                    full_name, fname, mname, sname, active_status, address, contact_no, email,
                    start_date, service_length_months, employee_status_type, department,
                    position_name, -- Added position_name
                    sss, tin, philhealth, pagibig, coc_attendance, coc_acknowledgement,
                    disciplinary_action, disciplinary_action_description, disciplinary_action_effectivity, discipinary_action_status,
                    labor_case, labor_case_reason, labor_case_status, dateupdated, snapshot_month, companyid, work_location, age, birthdate, civil_status, gender
                )
                VALUES (
                    @full_name, @fname, @mname, @sname, @active_status, @address, @contact_no, @email,
                    @start_date, @service_length_months, @employee_status_type, @department_id,
                    @position_name, -- Added position_name
                    @sss, @tin, @philhealth, @pagibig, @coc_attendance, @coc_acknowledgement,
                    @disciplinary_action, @disciplinary_action_description, @disciplinary_action_effectivity, @discipinary_action_status,
                    @labor_case, @labor_case_reason, @labor_case_status, @dateupdated, @snapshot_month, @companyid, @work_location, @age, @birthdate, @civil_status, @gender
                )
                ON CONFLICT (full_name, snapshot_month, companyid) DO UPDATE SET
                    fname = EXCLUDED.fname,
                    mname = EXCLUDED.mname,
                    sname = EXCLUDED.sname,
                    active_status = EXCLUDED.active_status,
                    address = EXCLUDED.address,
                    contact_no = EXCLUDED.contact_no,
                    email = EXCLUDED.email,
                    start_date = EXCLUDED.start_date,
                    service_length_months = EXCLUDED.service_length_months,
                    employee_status_type = EXCLUDED.employee_status_type,
                    department = EXCLUDED.department,
                    position_name = EXCLUDED.position_name, -- Added position_name update
                    sss = EXCLUDED.sss,
                    tin = EXCLUDED.tin,
                    philhealth = EXCLUDED.philhealth,
                    pagibig = EXCLUDED.pagibig,
                    coc_attendance = EXCLUDED.coc_attendance,
                    coc_acknowledgement = EXCLUDED.coc_acknowledgement,
                    disciplinary_action = EXCLUDED.disciplinary_action,
                    disciplinary_action_description = EXCLUDED.disciplinary_action_description,
                    disciplinary_action_effectivity = EXCLUDED.disciplinary_action_effectivity,
                    discipinary_action_status = EXCLUDED.discipinary_action_status,
                    labor_case = EXCLUDED.labor_case,
                    labor_case_reason = EXCLUDED.labor_case_reason,
                    labor_case_status = EXCLUDED.labor_case_status,
                    dateupdated = EXCLUDED.dateupdated;";

                var seaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Singapore");
                var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, seaTimeZone);

                DateTime targetDate = (targetYear.HasValue && targetMonth.HasValue)
                    ? new DateTime(targetYear.Value, targetMonth.Value, 1)
                    : new DateTime(now.Year, now.Month, 1);

                foreach (var item in employees)
                {
                    var parameters = new
                    {
                        item.full_name,
                        item.fname,
                        item.mname,
                        item.sname,
                        item.active_status,
                        item.address,
                        item.contact_no,
                        item.email,
                        item.start_date,
                        item.service_length_months,
                        item.employee_status_type,
                        item.department_id,
                        item.position_name, // Added position_name
                        item.sss,
                        item.tin,
                        item.philhealth,
                        item.pagibig,
                        item.coc_attendance,
                        item.coc_acknowledgement,
                        item.disciplinary_action,
                        item.disciplinary_action_description,
                        item.disciplinary_action_effectivity,
                        item.discipinary_action_status,
                        item.labor_case,
                        item.labor_case_reason,
                        item.labor_case_status,
                        item.work_location,
                        item.age,
                        item.birthdate,
                        item.civil_status,
                        item.gender,
                        dateupdated = now,
                        snapshot_month = targetDate,
                        companyid = companyId
                    };

                    try
                    {
                        await connection.ExecuteAsync(query, parameters);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error upserting employee {item.fname}");
                    }
                }

                return employees.Count();
            });
        }

        public async Task<object> ProcessEmployeeMasterFileMigrationAsync(string userid, long companyid)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                // Set session variable for audit trail
                await SetAuditUserIdAsync(connection, userid);

                // Call stored procedure (function) with both companyid and userid parameters
                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT * FROM core.migrate_employee_masterfile_to_hr_details_v2(@CompanyId, @UserId);",
                    new { CompanyId = companyid, UserId = userid.Trim() }
                );

                return result;
            });
        }

        public async Task<IEnumerable<EmployeeMasterFileModel>> GetMasterListAsync(int companyId, DateTime? snapshotMonth = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var baseQuery = @"
            SELECT 
                autoid,
                full_name,
                fname,
                mname,
                sname,
                active_status,
                address,
                contact_no,
                email,
                start_date,
                service_length_months,
                employee_status_type,
                department,
                sss,
                tin,
                philhealth,
                pagibig,
                coc_attendance,
                coc_acknowledgement,
                disciplinary_action,
                disciplinary_action_description,
                disciplinary_action_effectivity,
                discipinary_action_status,
                labor_case,
                labor_case_reason,
                labor_case_status,
                dateupdated,
                snapshot_month,
                work_location,
                age,
                birthdate,
                civil_status,
                gender,
                companyid
            FROM core.employee_masterfile
            WHERE companyid = @CompanyId
        ";

                if (snapshotMonth.HasValue)
                {
                    baseQuery += " AND snapshot_month = @SnapshotMonth";
                    return await connection.QueryAsync<EmployeeMasterFileModel>(baseQuery, new { CompanyId = companyId, SnapshotMonth = snapshotMonth });
                }

                return await connection.QueryAsync<EmployeeMasterFileModel>(baseQuery, new { CompanyId = companyId });
            });
        }

        public async Task<bool> UpdateEmployeeByEmpIdAsync(EmployeeHRDetailsUpdateModel employee, string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                try
                {
                    // Set session variable for audit trail
                    await SetAuditUserIdAsync(connection, userId);

                    // Get the current employee data before update for audit trail
                    var currentEmployee = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT * FROM core.main_employee_hr_details WHERE empid = @EmpId AND record_status = true",
                        new { EmpId = employee.empid });

                    if (currentEmployee == null)
                    {
                        return false;
                    }

                    // Build dynamic query based on provided fields (excluding empid)
                    var updateFields = new List<string>();
                    var parameters = new Dictionary<string, object> { { "empid", employee.empid } };

                    // Only add fields to update if they are not null/empty (excluding empid)
                    if (!string.IsNullOrEmpty(employee.fname))
                    {
                        updateFields.Add("fname = @fname");
                        parameters["fname"] = employee.fname;
                    }

                    if (!string.IsNullOrEmpty(employee.mname))
                    {
                        updateFields.Add("mname = @mname");
                        parameters["mname"] = employee.mname;
                    }

                    if (!string.IsNullOrEmpty(employee.sname))
                    {
                        updateFields.Add("sname = @sname");
                        parameters["sname"] = employee.sname;
                    }

                    if (employee.age.HasValue)
                    {
                        updateFields.Add("age = @age");
                        parameters["age"] = employee.age.Value;
                    }

                    if (employee.birth_date.HasValue)
                    {
                        updateFields.Add("birth_date = @birth_date");
                        parameters["birth_date"] = employee.birth_date.Value;
                    }

                    if (employee.civil_status.HasValue)
                    {
                        updateFields.Add("civil_status = @civil_status");
                        parameters["civil_status"] = employee.civil_status.Value;
                    }

                    if (employee.gender.HasValue)
                    {
                        updateFields.Add("gender = @gender");
                        parameters["gender"] = employee.gender.Value;
                    }

                    if (!string.IsNullOrEmpty(employee.contact_no))
                    {
                        updateFields.Add("contact_no = @contact_no");
                        parameters["contact_no"] = employee.contact_no;
                    }

                    if (!string.IsNullOrEmpty(employee.email))
                    {
                        updateFields.Add("email = @email");
                        parameters["email"] = employee.email;
                    }

                    if (!string.IsNullOrEmpty(employee.address))
                    {
                        updateFields.Add("address = @address");
                        parameters["address"] = employee.address;
                    }

                    if (employee.company_id.HasValue)
                    {
                        updateFields.Add("company_id = @company_id");
                        parameters["company_id"] = employee.company_id.Value;
                    }

                    if (employee.employment_status.HasValue)
                    {
                        updateFields.Add("employment_status = @employment_status");
                        parameters["employment_status"] = employee.employment_status.Value;
                    }

                    if (employee.position_id.HasValue)
                    {
                        updateFields.Add("position_id = @position_id");
                        parameters["position_id"] = employee.position_id.Value;
                    }

                    if (employee.department_id.HasValue)
                    {
                        updateFields.Add("department_id = @department_id");
                        parameters["department_id"] = employee.department_id.Value;
                    }

                    if (employee.start_date.HasValue)
                    {
                        updateFields.Add("start_date = @start_date");
                        parameters["start_date"] = employee.start_date.Value;
                    }

                    if (!string.IsNullOrEmpty(employee.sss))
                    {
                        updateFields.Add("sss = @sss");
                        parameters["sss"] = employee.sss;
                    }

                    if (!string.IsNullOrEmpty(employee.tin))
                    {
                        updateFields.Add("tin = @tin");
                        parameters["tin"] = employee.tin;
                    }

                    if (!string.IsNullOrEmpty(employee.philhealth))
                    {
                        updateFields.Add("philhealth = @philhealth");
                        parameters["philhealth"] = employee.philhealth;
                    }

                    if (!string.IsNullOrEmpty(employee.pagibig))
                    {
                        updateFields.Add("pagibig = @pagibig");
                        parameters["pagibig"] = employee.pagibig;
                    }

                    if (employee.active_status.HasValue)
                    {
                        updateFields.Add("active_status = @active_status");
                        parameters["active_status"] = employee.active_status.Value;
                    }

                    if (employee.city_code != null)
                    {
                        updateFields.Add("city_code = @city_code");
                        parameters["city_code"] = JsonConvert.SerializeObject(employee.city_code);
                    }

                    // If no fields to update, return true (no changes needed)
                    if (!updateFields.Any())
                    {
                        return true;
                    }

                    var query = $@"
                UPDATE core.main_employee_hr_details SET
                    {string.Join(", ", updateFields)}
                WHERE empid = @empid AND record_status = true";

                    var rowsAffected = await connection.ExecuteAsync(query, parameters);

                    if (rowsAffected > 0)
                    {
                        // Log the update operation using the unified audit method
                        await LogAuditTrailAsync(
                            connection,
                            "main_employee_hr_details",
                            Convert.ToInt64(currentEmployee.autoid),
                            "UPDATE EMPLOYEE DETAILS",
                            userId,
                            currentEmployee,
                            employee,
                            $"Employee {employee.empid} updated successfully"
                        );
                    }

                    return rowsAffected > 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in UpdateEmployeeByEmpIdAsync: {Message}", ex.Message);
                    throw;
                }
            });
        }

        public async Task<bool> DeleteEmployeeByEmpIdAsync(string empid, string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                try
                {
                    // Set session variable for audit trail
                    await SetAuditUserIdAsync(connection, userId);

                    // Get the current employee data before delete for audit trail
                    var currentEmployee = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT * FROM core.main_employee_hr_details WHERE empid = @EmpId AND record_status = true",
                        new { EmpId = empid });

                    if (currentEmployee == null)
                    {
                        return false;
                    }

                    var query = @"
                UPDATE core.main_employee_hr_details 
                SET record_status = false 
                WHERE empid = @EmpId AND record_status = true";

                    var rowsAffected = await connection.ExecuteAsync(query, new { EmpId = empid });

                    if (rowsAffected > 0)
                    {
                        // Log the delete operation using the unified audit method
                        await LogAuditTrailAsync(
                            connection,
                            "main_employee_hr_details",
                            Convert.ToInt64(currentEmployee.autoid),
                            "DISABLE EMPLOYEE",
                            userId,
                            currentEmployee,
                            null,
                            $"Employee {empid} deleted (soft delete) via API"
                        );
                    }

                    return rowsAffected > 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in DeleteEmployeeByEmpIdAsync: {Message}", ex.Message);
                    throw;
                }
            });
        }

        public async Task<EmployeeHRDetailsModel> GetEmployeeByEmpIdAsync(string empid)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
            SELECT 
                e.autoid, e.date_registered, e.empid, e.fname, e.mname, e.sname, e.age, e.birth_date,
                e.civil_status, e.gender, e.contact_no, e.email, e.address, e.company_id,
                e.employment_status, e.position_id, e.department_id, e.start_date,
                e.sss, e.tin, e.philhealth, e.pagibig, e.active_status, e.record_status, 
                e.city_code,
                CASE 
                    WHEN e.city_code IS NULL THEN NULL
                    WHEN jsonb_typeof(e.city_code) = 'array' THEN
                        (SELECT string_agg(c.city_name, ', ')
                         FROM core.sys_city_table c
                         WHERE c.city_code = ANY(
                             SELECT jsonb_array_elements_text(e.city_code)::integer
                         ))
                    WHEN jsonb_typeof(e.city_code) = 'number' THEN
                        (SELECT c.city_name 
                         FROM core.sys_city_table c 
                         WHERE c.city_code = e.city_code::integer)
                    ELSE NULL
                END as city_name
            FROM core.main_employee_hr_details e
            WHERE e.empid = @EmpId AND e.record_status = true";

                return await connection.QueryFirstOrDefaultAsync<EmployeeHRDetailsModel>(query, new { EmpId = empid });
            });
        }
        public async Task<IEnumerable<EmployeeHRDetailsModel>> GetAllEmployeesAsync(
    int? companyId = null,
    bool? isActive = null,
    int page = 1,
    int pageSize = 10,
    string search = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var baseQuery = @"
        SELECT 
        e.autoid, e.date_registered, e.empid, e.fname, e.mname, e.sname, e.age, e.birth_date,
        e.civil_status, e.gender, e.contact_no, e.email, e.address, e.company_id,
        e.employment_status, e.position_id, p.position_desc, e.department_id, e.start_date,
        e.sss, e.tin, e.philhealth, e.pagibig, e.active_status, e.record_status, 
        e.city_code,
        CASE 
            WHEN e.city_code IS NULL THEN NULL
            WHEN jsonb_typeof(e.city_code) = 'array' THEN
                (SELECT string_agg(c.city_name, ', ')
                 FROM core.sys_city_table c
                 WHERE c.city_code = ANY(
                     SELECT jsonb_array_elements_text(e.city_code)::integer
                 ))
            WHEN jsonb_typeof(e.city_code) = 'number' THEN
                (SELECT c.city_name 
                 FROM core.sys_city_table c 
                 WHERE c.city_code = e.city_code::integer)
            ELSE NULL
        END as city_name
    FROM core.main_employee_hr_details e
    LEFT JOIN core.main_employee_positions_definitions p ON e.position_id = p.position_id
    WHERE 1=1";

                var parameters = new DynamicParameters();

                // Add company filter
                if (companyId.HasValue)
                {
                    baseQuery += " AND e.company_id = @CompanyId";
                    parameters.Add("CompanyId", companyId.Value);
                }

                // Add active status filter - maps to record_status
                if (isActive.HasValue)
                {
                    baseQuery += " AND e.record_status = @IsActive";
                    parameters.Add("IsActive", isActive.Value);
                }

                // Add search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    baseQuery += @" AND (
                LOWER(CONCAT(e.fname, ' ', COALESCE(e.mname, ''), ' ', e.sname)) LIKE LOWER(@Search) OR
                LOWER(e.empid) LIKE LOWER(@Search) OR
                LOWER(COALESCE(e.email, '')) LIKE LOWER(@Search) OR
                LOWER(COALESCE(p.position_desc, '')) LIKE LOWER(@Search)
            )";
                    parameters.Add("Search", $"%{search.Trim()}%");
                }

                // FIXED: Use consistent ordering with unique identifier to prevent pagination issues
                baseQuery += " ORDER BY e.autoid DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                parameters.Add("Offset", (page - 1) * pageSize);
                parameters.Add("PageSize", pageSize);

                return await connection.QueryAsync<EmployeeHRDetailsModel>(baseQuery, parameters);
            });
        }
        public async Task<int> GetEmployeeCountAsync(
    int? companyId = null,
    bool? isActive = null,
    string search = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var baseQuery = @"
            SELECT COUNT(*)
            FROM core.main_employee_hr_details e
            LEFT JOIN core.main_employee_positions_definitions p ON e.position_id = p.position_id
            WHERE 1=1";

                var parameters = new DynamicParameters();

                if (companyId.HasValue)
                {
                    baseQuery += " AND e.company_id = @CompanyId";
                    parameters.Add("CompanyId", companyId.Value);
                }

                if (isActive.HasValue)
                {
                    baseQuery += " AND e.record_status = @IsActive";
                    parameters.Add("IsActive", isActive.Value);
                }

                // Add search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    baseQuery += @" AND (
                LOWER(CONCAT(e.fname, ' ', COALESCE(e.mname, ''), ' ', e.sname)) LIKE LOWER(@Search) OR
                LOWER(e.empid) LIKE LOWER(@Search) OR
                LOWER(COALESCE(e.email, '')) LIKE LOWER(@Search) OR
                LOWER(COALESCE(p.position_desc, '')) LIKE LOWER(@Search)
            )";
                    parameters.Add("Search", $"%{search.Trim()}%");
                }

                return await connection.QuerySingleAsync<int>(baseQuery, parameters);
            });
        }
        public async Task<EmployeeHRDetailsModel> GetEmployeeByIdAsync(long autoid)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
            SELECT 
                e.autoid, e.date_registered, e.empid, e.fname, e.mname, e.sname, e.age, e.birth_date,
                e.civil_status, e.gender, e.contact_no, e.email, e.address, e.company_id,
                e.employment_status, e.position_id, e.department_id, e.start_date,
                e.sss, e.tin, e.philhealth, e.pagibig, e.active_status, e.record_status, 
                e.city_code,
                CASE 
                    WHEN e.city_code IS NULL THEN NULL
                    WHEN jsonb_typeof(e.city_code) = 'array' THEN
                        (SELECT string_agg(c.city_name, ', ')
                         FROM core.sys_city_table c
                         WHERE c.city_code = ANY(
                             SELECT jsonb_array_elements_text(e.city_code)::integer
                         ))
                    WHEN jsonb_typeof(e.city_code) = 'number' THEN
                        (SELECT c.city_name 
                         FROM core.sys_city_table c 
                         WHERE c.city_code = e.city_code::integer)
                    ELSE NULL
                END as city_name
            FROM core.main_employee_hr_details e
            WHERE e.autoid = @AutoId AND e.record_status = true";

                return await connection.QueryFirstOrDefaultAsync<EmployeeHRDetailsModel>(query, new { AutoId = autoid });
            });
        }

        public async Task<long> CreateEmployeeAsync(EmployeeHRDetailsCreateModel employee, string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                try
                {
                    // Set session variable for audit trail
                    await SetAuditUserIdAsync(connection, userId);

                    // Generate unique 15-digit employee ID using database function
                    var generatedEmpId = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT core.generate_unique_empid()");

                    // Parse city_code to JSONB format
                    object cityCodeJsonb = null;
                    if (employee.city_code.HasValue)
                    {
                        var jsonElement = employee.city_code.Value;

                        if (jsonElement.ValueKind == JsonValueKind.Array)
                        {
                            var cityCodesList = new List<long>();
                            foreach (var element in jsonElement.EnumerateArray())
                            {
                                if (element.TryGetInt64(out var cityCode))
                                {
                                    cityCodesList.Add(cityCode);
                                }
                                else if (element.TryGetInt32(out var intCityCode))
                                {
                                    cityCodesList.Add((long)intCityCode);
                                }
                            }
                            // Convert to JSON string for PostgreSQL JSONB
                            cityCodeJsonb = JsonConvert.SerializeObject(cityCodesList.ToArray());
                        }
                        else if (jsonElement.ValueKind == JsonValueKind.Number)
                        {
                            if (jsonElement.TryGetInt64(out var singleCode))
                            {
                                // Convert single value to JSON array string
                                cityCodeJsonb = JsonConvert.SerializeObject(new[] { singleCode });
                            }
                            else if (jsonElement.TryGetInt32(out var singleIntCode))
                            {
                                // Convert single value to JSON array string
                                cityCodeJsonb = JsonConvert.SerializeObject(new[] { (long)singleIntCode });
                            }
                        }
                        else if (jsonElement.ValueKind == JsonValueKind.String)
                        {
                            var cityCodeString = jsonElement.GetString();
                            try
                            {
                                var cityCodesArray = JsonConvert.DeserializeObject<long[]>(cityCodeString);
                                // Convert back to JSON string for PostgreSQL
                                cityCodeJsonb = JsonConvert.SerializeObject(cityCodesArray);
                            }
                            catch
                            {
                                if (long.TryParse(cityCodeString, out var singleCityCode))
                                {
                                    // Convert single value to JSON array string
                                    cityCodeJsonb = JsonConvert.SerializeObject(new[] { singleCityCode });
                                }
                            }
                        }
                    }

                    var query = @"
                INSERT INTO core.main_employee_hr_details (
                    empid, fname, mname, sname, age, birth_date, civil_status, gender,
                    contact_no, email, address, company_id, employment_status, position_id,
                    department_id, start_date, sss, tin, philhealth, pagibig, active_status, city_code
                )
                VALUES (
                    @empid, @fname, @mname, @sname, @age, @birth_date, @civil_status, @gender,
                    @contact_no, @email, @address, @company_id, @employment_status, @position_id,
                    @department_id, @start_date, @sss, @tin, @philhealth, @pagibig, @active_status, @city_code::jsonb
                )
                RETURNING autoid";

                    var parameters = new
                    {
                        empid = generatedEmpId,
                        employee.fname,
                        employee.mname,
                        employee.sname,
                        employee.age,
                        employee.birth_date,
                        employee.civil_status,
                        employee.gender,
                        employee.contact_no,
                        employee.email,
                        employee.address,
                        employee.company_id,
                        employee.employment_status,
                        employee.position_id,
                        employee.department_id,
                        employee.start_date,
                        employee.sss,
                        employee.tin,
                        employee.philhealth,
                        employee.pagibig,
                        employee.active_status,
                        city_code = cityCodeJsonb
                    };

                    var newEmployeeId = await connection.QuerySingleAsync<long>(query, parameters);

                    // Log the create operation using the unified audit method
                    await LogAuditTrailAsync(
                        connection,
                        "main_employee_hr_details",
                        newEmployeeId,
                        "CREATE EMPLOYEE",
                        userId,
                        null,
                        employee,
                        $"Employee {generatedEmpId} created successfully"
                    );

                    return newEmployeeId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in CreateEmployeeAsync: {Message}", ex.Message);
                    throw;
                }
            });
        }

        //////////////////////////////////////////////////////
        /// REPORTS [ REP ]
        /// 
        public async Task<IEnumerable<EmployeePositionModel>> GetPositionsByCompanyIdAsync(long companyId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                    SELECT 
                        autoid,
                        position_id,
                        position_desc,
                        companyid,
                        status
                    FROM core.main_employee_positions_definitions 
                    WHERE companyid = @CompanyId
                    ORDER BY position_desc";

                var parameters = new { CompanyId = companyId };

                return await connection.QueryAsync<EmployeePositionModel>(query, parameters);
            });
        }

        // Add these methods to CIRSMasterListRepository.cs
        public async Task<IEnumerable<HeadcountReportModel>> GetActiveHeadcountReportAsync(int? companyId = null, DateTime? asOfDate = null, bool includeDepartments = false)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var reportDate = asOfDate ?? DateTime.UtcNow;

                string query;

                if (includeDepartments)
                {
                    // Query with departments (when data is available)
                    query = @"
                SELECT 
                    e.autoid,
                    e.empid,
                    CONCAT(COALESCE(e.fname, ''), ' ', COALESCE(e.mname, ''), ' ', COALESCE(e.sname, '')) as full_name,
                    e.fname,
                    e.mname,
                    e.sname,
                    e.age,
                    e.birth_date,
                    e.civil_status,
                    e.gender,
                    e.contact_no,
                    e.email,
                    e.address,
                    e.company_id,
                    c.company_name,
                    e.employment_status,
                    CASE 
                        WHEN e.employment_status = 1 THEN 'Regular'
                        WHEN e.employment_status = 2 THEN 'Probationary'
                        WHEN e.employment_status = 3 THEN 'Contractual'
                        WHEN e.employment_status = 4 THEN 'Part-time'
                        ELSE 'Unknown'
                    END as employment_status_name,
                    e.position_id,
                    p.position_desc as position_name,
                    pdm.department_id,
                    COALESCE(d.department_desc, 'Department ' || COALESCE(pdm.department_id, e.department_id)::text) as department_name,
                    e.start_date,
                    e.sss,
                    e.tin,
                    e.philhealth,
                    e.pagibig,
                    e.active_status,
                    e.record_status,
                    e.date_registered,
                    e.city_code,
                    CASE 
                        WHEN e.city_code IS NULL THEN NULL
                        WHEN jsonb_typeof(e.city_code) = 'array' THEN
                            (SELECT string_agg(city.city_name, ', ')
                             FROM core.sys_city_table city
                             WHERE city.city_code = ANY(
                                 SELECT jsonb_array_elements_text(e.city_code)::bigint
                             ))
                        WHEN jsonb_typeof(e.city_code) = 'number' THEN
                            (SELECT city.city_name 
                             FROM core.sys_city_table city 
                             WHERE city.city_code = e.city_code::bigint)
                        ELSE NULL
                    END as city_name,
                    -- Calculate service length
                    EXTRACT(YEAR FROM AGE(@ReportDate, e.start_date))::int as service_length_years,
                    EXTRACT(MONTH FROM AGE(@ReportDate, e.start_date))::int as service_length_months,
                    EXTRACT(DAY FROM AGE(@ReportDate, e.start_date))::int as service_length_days,
                    CONCAT(
                        EXTRACT(YEAR FROM AGE(@ReportDate, e.start_date))::int, ' years, ',
                        EXTRACT(MONTH FROM AGE(@ReportDate, e.start_date))::int, ' months, ',
                        EXTRACT(DAY FROM AGE(@ReportDate, e.start_date))::int, ' days'
                    ) as service_length_formatted
                FROM core.main_employee_hr_details e
                LEFT JOIN core.sys_affiliate_companies c ON e.company_id = c.autoid
                LEFT JOIN core.main_employee_positions_definitions p ON e.position_id = p.position_id
                LEFT JOIN core.main_position_department_mappings pdm ON (
                    p.position_id = pdm.position_id 
                    AND p.companyid = pdm.companyid
                    AND pdm.status = true
                    AND pdm.effective_date <= @ReportDate
                )
                LEFT JOIN core.main_employee_departments_definitions d ON pdm.department_id = d.department_id
                WHERE e.record_status = true
                AND (@CompanyId IS NULL OR e.company_id = @CompanyId)
                ORDER BY e.company_id, e.start_date DESC";
                }
                else
                {
                    // Query without departments (default)
                    query = @"
                SELECT 
                    e.autoid,
                    e.empid,
                    CONCAT(COALESCE(e.fname, ''), ' ', COALESCE(e.mname, ''), ' ', COALESCE(e.sname, '')) as full_name,
                    e.fname,
                    e.mname,
                    e.sname,
                    e.age,
                    e.birth_date,
                    e.civil_status,
                    e.gender,
                    e.contact_no,
                    e.email,
                    e.address,
                    e.company_id,
                    c.company_name,
                    e.employment_status,
                    CASE 
                        WHEN e.employment_status = 1 THEN 'Regular'
                        WHEN e.employment_status = 2 THEN 'Probationary'
                        WHEN e.employment_status = 3 THEN 'Contractual'
                        WHEN e.employment_status = 4 THEN 'Part-time'
                        ELSE 'Unknown'
                    END as employment_status_name,
                    e.position_id,
                    p.position_desc as position_name,
                    e.department_id,
                    'Department ' || COALESCE(e.department_id, 0)::text as department_name,
                    e.start_date,
                    e.sss,
                    e.tin,
                    e.philhealth,
                    e.pagibig,
                    e.active_status,
                    e.record_status,
                    e.date_registered,
                    e.city_code,
                    CASE 
                        WHEN e.city_code IS NULL THEN NULL
                        WHEN jsonb_typeof(e.city_code) = 'array' THEN
                            (SELECT string_agg(city.city_name, ', ')
                             FROM core.sys_city_table city
                             WHERE city.city_code = ANY(
                                 SELECT jsonb_array_elements_text(e.city_code)::bigint
                             ))
                        WHEN jsonb_typeof(e.city_code) = 'number' THEN
                            (SELECT city.city_name 
                             FROM core.sys_city_table city 
                             WHERE city.city_code = e.city_code::bigint)
                        ELSE NULL
                    END as city_name,
                    -- Calculate service length
                    EXTRACT(YEAR FROM AGE(@ReportDate, e.start_date))::int as service_length_years,
                    EXTRACT(MONTH FROM AGE(@ReportDate, e.start_date))::int as service_length_months,
                    EXTRACT(DAY FROM AGE(@ReportDate, e.start_date))::int as service_length_days,
                    CONCAT(
                        EXTRACT(YEAR FROM AGE(@ReportDate, e.start_date))::int, ' years, ',
                        EXTRACT(MONTH FROM AGE(@ReportDate, e.start_date))::int, ' months, ',
                        EXTRACT(DAY FROM AGE(@ReportDate, e.start_date))::int, ' days'
                    ) as service_length_formatted
                FROM core.main_employee_hr_details e
                LEFT JOIN core.sys_affiliate_companies c ON e.company_id = c.autoid
                LEFT JOIN core.main_employee_positions_definitions p ON e.position_id = p.position_id
                WHERE e.record_status = true
                AND (@CompanyId IS NULL OR e.company_id = @CompanyId)
                ORDER BY e.company_id, e.start_date DESC";
                }

                var parameters = new
                {
                    CompanyId = companyId,
                    ReportDate = reportDate
                };

                return await connection.QueryAsync<HeadcountReportModel>(query, parameters);
            });
        }
        public async Task<IEnumerable<HeadcountSummaryModel>> GetHeadcountSummaryAsync(int? companyId = null, DateTime? asOfDate = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var reportDate = asOfDate ?? DateTime.UtcNow;

                var query = @"
            SELECT 
                e.company_id,
                c.company_name,
                COUNT(*) as total_headcount,
                COUNT(CASE WHEN e.employment_status = 1 THEN 1 END) as regular_employees,
                COUNT(CASE WHEN e.employment_status = 2 THEN 1 END) as probationary_employees,
                COUNT(CASE WHEN e.employment_status = 3 THEN 1 END) as contractual_employees,
                COUNT(CASE WHEN e.gender = 1 THEN 1 END) as male_employees,
                COUNT(CASE WHEN e.gender = 2 THEN 1 END) as female_employees,
                ROUND(AVG(EXTRACT(YEAR FROM AGE(@ReportDate, e.start_date))), 2) as average_service_length_years,
                COUNT(CASE WHEN EXTRACT(YEAR FROM AGE(@ReportDate, e.start_date)) < 1 THEN 1 END) as employees_less_than_1_year,
                COUNT(CASE WHEN EXTRACT(YEAR FROM AGE(@ReportDate, e.start_date)) BETWEEN 1 AND 5 THEN 1 END) as employees_1_to_5_years,
                COUNT(CASE WHEN EXTRACT(YEAR FROM AGE(@ReportDate, e.start_date)) BETWEEN 5 AND 10 THEN 1 END) as employees_5_to_10_years,
                COUNT(CASE WHEN EXTRACT(YEAR FROM AGE(@ReportDate, e.start_date)) > 10 THEN 1 END) as employees_more_than_10_years
            FROM core.main_employee_hr_details e
            LEFT JOIN core.sys_affiliate_companies c ON e.company_id = c.autoid
            WHERE e.record_status = true
            AND (@CompanyId IS NULL OR e.company_id = @CompanyId)
            GROUP BY e.company_id, c.company_name
            ORDER BY e.company_id";

                var parameters = new
                {
                    CompanyId = companyId,
                    ReportDate = reportDate
                };

                return await connection.QueryAsync<HeadcountSummaryModel>(query, parameters);
            });
        }

        public async Task<IEnumerable<dynamic>> GetCompaniesAsync()
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
            SELECT 
                autoid as company_id,
                company_name,
                company_desc
            FROM core.sys_affiliate_companies
            WHERE status = true
            ORDER BY company_name";

                return await connection.QueryAsync(query);
            });
        }

        public async Task<IEnumerable<dynamic>> GetDepartmentsAsync(int? companyId = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
            SELECT DISTINCT 
                d.department_id,
                d.department_desc as department_name,
                d.companyid
            FROM core.main_employee_departments_definitions d
            WHERE d.status = true
            AND (@CompanyId IS NULL OR d.companyid = @CompanyId)
            ORDER BY d.department_desc";

                return await connection.QueryAsync(query, new { CompanyId = companyId });
            });
        }

        public async Task<IEnumerable<dynamic>> GetPositionDepartmentMappingsAsync(int? companyId = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
            SELECT 
                pdm.autoid,
                pdm.position_id,
                p.position_desc,
                pdm.department_id,
                d.department_desc,
                pdm.companyid,
                c.company_name,
                pdm.effective_date,
                pdm.status
            FROM core.main_position_department_mappings pdm
            LEFT JOIN core.main_employee_positions_definitions p ON pdm.position_id = p.position_id
            LEFT JOIN core.main_employee_departments_definitions d ON pdm.department_id = d.department_id
            LEFT JOIN core.sys_affiliate_companies c ON pdm.companyid = c.autoid
            WHERE pdm.status = true
            AND (@CompanyId IS NULL OR pdm.companyid = @CompanyId)
            ORDER BY p.position_desc, d.department_desc";

                return await connection.QueryAsync(query, new { CompanyId = companyId });
            });
        }

        public async Task<long> CreatePositionDepartmentMappingAsync(int positionId, int departmentId, int companyId, DateTime? effectiveDate = null, string createdBy = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
            INSERT INTO core.main_position_department_mappings (
                position_id, department_id, companyid, effective_date, created_by
            )
            VALUES (
                @PositionId, @DepartmentId, @CompanyId, @EffectiveDate, @CreatedBy
            )
            RETURNING autoid";

                var parameters = new
                {
                    PositionId = positionId,
                    DepartmentId = departmentId,
                    CompanyId = companyId,
                    EffectiveDate = effectiveDate ?? DateTime.UtcNow.Date,
                    CreatedBy = createdBy
                };

                return await connection.QuerySingleAsync<long>(query, parameters);
            });
        }

        public async Task<bool> UpdatePositionDepartmentMappingAsync(long mappingId, int? departmentId = null, DateTime? effectiveDate = null, string updatedBy = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var updateFields = new List<string>();
                var parameters = new Dictionary<string, object> { { "MappingId", mappingId } };

                if (departmentId.HasValue)
                {
                    updateFields.Add("department_id = @DepartmentId");
                    parameters["DepartmentId"] = departmentId.Value;
                }

                if (effectiveDate.HasValue)
                {
                    updateFields.Add("effective_date = @EffectiveDate");
                    parameters["EffectiveDate"] = effectiveDate.Value;
                }

                if (!string.IsNullOrEmpty(updatedBy))
                {
                    updateFields.Add("updated_by = @UpdatedBy");
                    parameters["UpdatedBy"] = updatedBy;
                }

                if (!updateFields.Any())
                {
                    return false;
                }

                var query = $@"
            UPDATE core.main_position_department_mappings 
            SET {string.Join(", ", updateFields)}
            WHERE autoid = @MappingId AND status = true";

                var rowsAffected = await connection.ExecuteAsync(query, parameters);
                return rowsAffected > 0;
            });
        }

        public async Task<bool> DeletePositionDepartmentMappingAsync(long mappingId, string deletedBy = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
            UPDATE core.main_position_department_mappings 
            SET status = false, updated_by = @DeletedBy
            WHERE autoid = @MappingId AND status = true";

                var parameters = new
                {
                    MappingId = mappingId,
                    DeletedBy = deletedBy
                };

                var rowsAffected = await connection.ExecuteAsync(query, parameters);
                return rowsAffected > 0;
            });
        }

        // Method to get positions for a specific company
        public async Task<IEnumerable<dynamic>> GetPositionsAsync(int? companyId = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
            SELECT 
                p.position_id,
                p.position_desc,
                p.companyid,
                c.company_name
            FROM core.main_employee_positions_definitions p
            LEFT JOIN core.sys_affiliate_companies c ON p.companyid = c.autoid
            WHERE p.status = true
            AND (@CompanyId IS NULL OR p.companyid = @CompanyId)
            ORDER BY p.position_desc";

                return await connection.QueryAsync(query, new { CompanyId = companyId });
            });
        }

        // Method to get headcount by department
        public async Task<IEnumerable<dynamic>> GetHeadcountByDepartmentAsync(int? companyId = null, DateTime? asOfDate = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var reportDate = asOfDate ?? DateTime.UtcNow;

                var query = @"
            SELECT 
                d.department_id,
                d.department_desc as department_name,
                c.company_name,
                COUNT(e.autoid) as employee_count,
                COUNT(CASE WHEN e.employment_status = 1 THEN 1 END) as regular_count,
                COUNT(CASE WHEN e.employment_status = 2 THEN 1 END) as probationary_count,
                COUNT(CASE WHEN e.employment_status = 3 THEN 1 END) as contractual_count,
                COUNT(CASE WHEN e.gender = 1 THEN 1 END) as male_count,
                COUNT(CASE WHEN e.gender = 2 THEN 1 END) as female_count
            FROM core.main_employee_departments_definitions d
            LEFT JOIN core.main_position_department_mappings pdm ON d.department_id = pdm.department_id
            LEFT JOIN core.main_employee_positions_definitions p ON pdm.position_id = p.position_id
            LEFT JOIN core.main_employee_hr_details e ON (
                e.position_id = p.position_id 
                AND e.record_status = true
                AND (@CompanyId IS NULL OR e.company_id = @CompanyId)
            )
            LEFT JOIN core.sys_affiliate_companies c ON d.companyid = c.autoid
            WHERE d.status = true
            AND (@CompanyId IS NULL OR d.companyid = @CompanyId)
            GROUP BY d.department_id, d.department_desc, c.company_name
            ORDER BY d.department_desc";

                var parameters = new
                {
                    CompanyId = companyId,
                    ReportDate = reportDate
                };

                return await connection.QueryAsync(query, parameters);
            });
        }

        // Method to get headcount by position
        public async Task<IEnumerable<dynamic>> GetHeadcountByPositionAsync(int? companyId = null, DateTime? asOfDate = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var reportDate = asOfDate ?? DateTime.UtcNow;

                var query = @"
            SELECT 
                p.position_id,
                p.position_desc as position_name,
                c.company_name,
                COUNT(e.autoid) as employee_count,
                COUNT(CASE WHEN e.employment_status = 1 THEN 1 END) as regular_count,
                COUNT(CASE WHEN e.employment_status = 2 THEN 1 END) as probationary_count,
                COUNT(CASE WHEN e.employment_status = 3 THEN 1 END) as contractual_count,
                COUNT(CASE WHEN e.gender = 1 THEN 1 END) as male_count,
                COUNT(CASE WHEN e.gender = 2 THEN 1 END) as female_count
            FROM core.main_employee_positions_definitions p
            LEFT JOIN core.main_employee_hr_details e ON (
                e.position_id = p.position_id 
                AND e.record_status = true
                AND (@CompanyId IS NULL OR e.company_id = @CompanyId)
            )
            LEFT JOIN core.sys_affiliate_companies c ON p.companyid = c.autoid
            WHERE p.status = true
            AND (@CompanyId IS NULL OR p.companyid = @CompanyId)
            GROUP BY p.position_id, p.position_desc, c.company_name
            ORDER BY p.position_desc";

                var parameters = new
                {
                    CompanyId = companyId,
                    ReportDate = reportDate
                };

                return await connection.QueryAsync(query, parameters);
            });
        }



        // Fixed method to get city distribution per company
        public async Task<IEnumerable<dynamic>> GetHeadcountByCityDistributionAsync(int? companyId = null, DateTime? asOfDate = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var reportDate = asOfDate ?? DateTime.UtcNow;

                var query = @"
            WITH city_employees AS (
                SELECT 
                    e.autoid,
                    e.company_id,
                    c.company_name,
                    e.employment_status,
                    e.gender,
                    e.start_date,
                    CASE 
                        WHEN e.city_code IS NULL THEN 
                            ARRAY[0::bigint]
                        WHEN jsonb_typeof(e.city_code) = 'array' THEN
                            ARRAY(SELECT jsonb_array_elements_text(e.city_code)::bigint)
                        WHEN jsonb_typeof(e.city_code) = 'number' THEN
                            ARRAY[e.city_code::bigint]
                        ELSE 
                            ARRAY[0::bigint]
                    END as city_codes
                FROM core.main_employee_hr_details e
                LEFT JOIN core.sys_affiliate_companies c ON e.company_id = c.autoid
                WHERE e.record_status = true
                AND (@CompanyId IS NULL OR e.company_id = @CompanyId)
            ),
            expanded_cities AS (
                SELECT 
                    ce.autoid,
                    ce.company_id,
                    ce.company_name,
                    ce.employment_status,
                    ce.gender,
                    ce.start_date,
                    unnest(ce.city_codes) as city_code
                FROM city_employees ce
            )
            SELECT 
                ec.company_id,
                ec.company_name,
                ec.city_code as primary_city_code,
                COALESCE(ct.city_name, 'Unknown Location') as city_name,
                COUNT(*)::int as employee_count,
                COUNT(CASE WHEN ec.employment_status = 1 THEN 1 END)::int as regular_count,
                COUNT(CASE WHEN ec.employment_status = 2 THEN 1 END)::int as probationary_count,
                COUNT(CASE WHEN ec.employment_status = 3 THEN 1 END)::int as contractual_count,
                COUNT(CASE WHEN ec.gender = 1 THEN 1 END)::int as male_count,
                COUNT(CASE WHEN ec.gender = 2 THEN 1 END)::int as female_count,
                ROUND(AVG(EXTRACT(YEAR FROM AGE(@ReportDate, ec.start_date))), 2) as average_service_length_years
            FROM expanded_cities ec
            LEFT JOIN core.sys_city_table ct ON ec.city_code = ct.city_code
            GROUP BY ec.company_id, ec.company_name, ec.city_code, ct.city_name
            ORDER BY ec.company_name, employee_count DESC";

                var parameters = new
                {
                    CompanyId = companyId,
                    ReportDate = reportDate
                };

                return await connection.QueryAsync(query, parameters);
            });
        }

        // Fixed method to get city distribution summary
        public async Task<IEnumerable<dynamic>> GetCityDistributionSummaryAsync(int? companyId = null, DateTime? asOfDate = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var reportDate = asOfDate ?? DateTime.UtcNow;

                var query = @"
            WITH city_employees AS (
                SELECT 
                    e.company_id,
                    c.company_name,
                    CASE 
                        WHEN e.city_code IS NULL THEN 
                            ARRAY[0::bigint]
                        WHEN jsonb_typeof(e.city_code) = 'array' THEN
                            ARRAY(SELECT jsonb_array_elements_text(e.city_code)::bigint)
                        WHEN jsonb_typeof(e.city_code) = 'number' THEN
                            ARRAY[e.city_code::bigint]
                        ELSE 
                            ARRAY[0::bigint]
                    END as city_codes
                FROM core.main_employee_hr_details e
                LEFT JOIN core.sys_affiliate_companies c ON e.company_id = c.autoid
                WHERE e.record_status = true
                AND (@CompanyId IS NULL OR e.company_id = @CompanyId)
            ),
            expanded_cities AS (
                SELECT 
                    ce.company_id,
                    ce.company_name,
                    unnest(ce.city_codes) as city_code
                FROM city_employees ce
            )
            SELECT 
                ec.company_id,
                ec.company_name,
                COUNT(DISTINCT ec.city_code) as total_cities,
                COUNT(*) as total_employees,
                STRING_AGG(DISTINCT ct.city_name, ', ' ORDER BY ct.city_name) as cities_list
            FROM expanded_cities ec
            LEFT JOIN core.sys_city_table ct ON ec.city_code = ct.city_code
            GROUP BY ec.company_id, ec.company_name
            ORDER BY ec.company_name";

                var parameters = new
                {
                    CompanyId = companyId,
                    ReportDate = reportDate
                };

                return await connection.QueryAsync(query, parameters);
            });
        }

        // Fixed method to get top cities
        public async Task<IEnumerable<dynamic>> GetTopCitiesByEmployeeCountAsync(int? companyId = null, int topCount = 10, DateTime? asOfDate = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var reportDate = asOfDate ?? DateTime.UtcNow;

                var query = @"
            WITH city_employees AS (
                SELECT 
                    e.company_id,
                    c.company_name,
                    CASE 
                        WHEN e.city_code IS NULL THEN 
                            ARRAY[0::bigint]
                        WHEN jsonb_typeof(e.city_code) = 'array' THEN
                            ARRAY(SELECT jsonb_array_elements_text(e.city_code)::bigint)
                        WHEN jsonb_typeof(e.city_code) = 'number' THEN
                            ARRAY[e.city_code::bigint]
                        ELSE 
                            ARRAY[0::bigint]
                    END as city_codes
                FROM core.main_employee_hr_details e
                LEFT JOIN core.sys_affiliate_companies c ON e.company_id = c.autoid
                WHERE e.record_status = true
                AND (@CompanyId IS NULL OR e.company_id = @CompanyId)
            ),
            expanded_cities AS (
                SELECT 
                    ce.company_id,
                    ce.company_name,
                    unnest(ce.city_codes) as city_code
                FROM city_employees ce
            )
            SELECT 
                COALESCE(ct.city_name, 'Unknown Location') as city_name,
                COUNT(*)::int as employee_count,
                COUNT(DISTINCT ec.company_id)::int as companies_count,
                STRING_AGG(DISTINCT ec.company_name, ', ' ORDER BY ec.company_name) as companies_list
            FROM expanded_cities ec
            LEFT JOIN core.sys_city_table ct ON ec.city_code = ct.city_code
            GROUP BY ec.city_code, ct.city_name
            ORDER BY employee_count DESC
            LIMIT @TopCount";

                var parameters = new
                {
                    CompanyId = companyId,
                    TopCount = topCount,
                    ReportDate = reportDate
                };

                return await connection.QueryAsync(query, parameters);
            });
        }



        ///








        //// [ ANALYTICS }

        // Method to get monthly employee statistics
        public async Task<IEnumerable<dynamic>> GetMonthlyEmployeeStatisticsAsync(int? companyId = null, int year = 0, int month = 0)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                // If year/month not provided, use current month
                var targetYear = year > 0 ? year : DateTime.UtcNow.Year;
                var targetMonth = month > 0 ? month : DateTime.UtcNow.Month;

                var startOfMonth = new DateTime(targetYear, targetMonth, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var query = @"
            WITH monthly_stats AS (
                SELECT 
                    e.company_id,
                    c.company_name,
                    -- Count active employees at start of month
                    COUNT(CASE 
                        WHEN e.record_status = true 
                        AND e.start_date <= @StartOfMonth 
                        AND (e.end_date IS NULL OR e.end_date > @StartOfMonth)
                        THEN 1 
                    END) as active_at_start,
                    -- Count active employees at end of month
                    COUNT(CASE 
                        WHEN e.record_status = true 
                        AND e.start_date <= @EndOfMonth 
                        AND (e.end_date IS NULL OR e.end_date > @EndOfMonth)
                        THEN 1 
                    END) as active_at_end,
                    -- Count employees who resigned during the month
                    COUNT(CASE 
                        WHEN e.record_status = false 
                        AND e.end_date >= @StartOfMonth 
                        AND e.end_date <= @EndOfMonth
                        THEN 1 
                    END) as resigned_during_month,
                    -- Count new hires during the month
                    COUNT(CASE 
                        WHEN e.start_date >= @StartOfMonth 
                        AND e.start_date <= @EndOfMonth
                        THEN 1 
                    END) as new_hires_during_month
                FROM core.main_employee_hr_details e
                LEFT JOIN core.sys_affiliate_companies c ON e.company_id = c.autoid
                WHERE (@CompanyId IS NULL OR e.company_id = @CompanyId)
                GROUP BY e.company_id, c.company_name
            )
            SELECT 
                company_id,
                company_name,
                active_at_start,
                active_at_end,
                resigned_during_month,
                new_hires_during_month,
                -- Calculate average employees
                ROUND((active_at_start + active_at_end) / 2.0, 2) as average_employees,
                -- Calculate attrition rate
                CASE 
                    WHEN (active_at_start + active_at_end) > 0 THEN
                        ROUND((resigned_during_month::decimal / ((active_at_start + active_at_end) / 2.0)) * 100, 2)
                    ELSE 0
                END as attrition_rate_percent,
                -- Calculate hiring rate
                CASE 
                    WHEN (active_at_start + active_at_end) > 0 THEN
                        ROUND((new_hires_during_month::decimal / ((active_at_start + active_at_end) / 2.0)) * 100, 2)
                    ELSE 0
                END as hiring_rate_percent,
                -- Calculate net change
                (active_at_end - active_at_start) as net_change
            FROM monthly_stats
            ORDER BY company_name";

                var parameters = new
                {
                    CompanyId = companyId,
                    StartOfMonth = startOfMonth,
                    EndOfMonth = endOfMonth
                };

                return await connection.QueryAsync(query, parameters);
            });
        }

        // Method to get monthly statistics for a specific company
        public async Task<dynamic> GetCompanyMonthlyStatisticsAsync(int companyId, int year = 0, int month = 0)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var targetYear = year > 0 ? year : DateTime.UtcNow.Year;
                var targetMonth = month > 0 ? month : DateTime.UtcNow.Month;

                var startOfMonth = new DateTime(targetYear, targetMonth, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var query = @"
            WITH monthly_stats AS (
                SELECT 
                    e.company_id,
                    c.company_name,
                    COUNT(CASE 
                        WHEN e.record_status = true 
                        AND e.start_date <= @StartOfMonth 
                        AND (e.end_date IS NULL OR e.end_date > @StartOfMonth)
                        THEN 1 
                    END) as active_at_start,
                    COUNT(CASE 
                        WHEN e.record_status = true 
                        AND e.start_date <= @EndOfMonth 
                        AND (e.end_date IS NULL OR e.end_date > @EndOfMonth)
                        THEN 1 
                    END) as active_at_end,
                    COUNT(CASE 
                        WHEN e.record_status = false 
                        AND e.end_date >= @StartOfMonth 
                        AND e.end_date <= @EndOfMonth
                        THEN 1 
                    END) as resigned_during_month,
                    COUNT(CASE 
                        WHEN e.start_date >= @StartOfMonth 
                        AND e.start_date <= @EndOfMonth
                        THEN 1 
                    END) as new_hires_during_month
                FROM core.main_employee_hr_details e
                LEFT JOIN core.sys_affiliate_companies c ON e.company_id = c.autoid
                WHERE e.company_id = @CompanyId
                GROUP BY e.company_id, c.company_name
            )
            SELECT 
                company_id,
                company_name,
                active_at_start,
                active_at_end,
                resigned_during_month,
                new_hires_during_month,
                ROUND((active_at_start + active_at_end) / 2.0, 2) as average_employees,
                CASE 
                    WHEN (active_at_start + active_at_end) > 0 THEN
                        ROUND((resigned_during_month::decimal / ((active_at_start + active_at_end) / 2.0)) * 100, 2)
                    ELSE 0
                END as attrition_rate_percent,
                CASE 
                    WHEN (active_at_start + active_at_end) > 0 THEN
                        ROUND((new_hires_during_month::decimal / ((active_at_start + active_at_end) / 2.0)) * 100, 2)
                    ELSE 0
                END as hiring_rate_percent,
                (active_at_end - active_at_start) as net_change
            FROM monthly_stats";

                var parameters = new
                {
                    CompanyId = companyId,
                    StartOfMonth = startOfMonth,
                    EndOfMonth = endOfMonth
                };

                return await connection.QueryFirstOrDefaultAsync(query, parameters);
            });
        }

        // Method to get year-over-year attrition trends
        public async Task<IEnumerable<dynamic>> GetAttritionTrendsAsync(int? companyId = null, int years = 2)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var currentYear = DateTime.UtcNow.Year;
                var startYear = currentYear - years + 1;

                var query = @"
            WITH monthly_data AS (
                SELECT 
                    e.company_id,
                    c.company_name,
                    EXTRACT(YEAR FROM e.end_date) as year,
                    EXTRACT(MONTH FROM e.end_date) as month,
                    COUNT(*) as resigned_count
                FROM core.main_employee_hr_details e
                LEFT JOIN core.sys_affiliate_companies c ON e.company_id = c.autoid
                WHERE e.record_status = false 
                AND e.end_date IS NOT NULL
                AND EXTRACT(YEAR FROM e.end_date) >= @StartYear
                AND (@CompanyId IS NULL OR e.company_id = @CompanyId)
                GROUP BY e.company_id, c.company_name, EXTRACT(YEAR FROM e.end_date), EXTRACT(MONTH FROM e.end_date)
            ),
            company_averages AS (
                SELECT 
                    company_id,
                    company_name,
                    year,
                    AVG(resigned_count) as avg_monthly_resignations
                FROM monthly_data
                GROUP BY company_id, company_name, year
            )
            SELECT 
                company_id,
                company_name,
                year,
                ROUND(avg_monthly_resignations, 2) as avg_monthly_resignations
            FROM company_averages
            ORDER BY company_name, year";

                var parameters = new
                {
                    CompanyId = companyId,
                    StartYear = startYear
                };

                return await connection.QueryAsync(query, parameters);
            });
        }

        // Method to get detailed resignation data for a specific month
        public async Task<IEnumerable<dynamic>> GetMonthlyResignationsAsync(int? companyId = null, int year = 0, int month = 0)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var targetYear = year > 0 ? year : DateTime.UtcNow.Year;
                var targetMonth = month > 0 ? month : DateTime.UtcNow.Month;

                var startOfMonth = new DateTime(targetYear, targetMonth, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var query = @"
            SELECT 
                e.autoid,
                e.empid,
                CONCAT(COALESCE(e.fname, ''), ' ', COALESCE(e.mname, ''), ' ', COALESCE(e.sname, '')) as full_name,
                e.company_id,
                c.company_name,
                e.start_date,
                e.end_date,
                EXTRACT(DAY FROM e.end_date) as resignation_day,
                p.position_desc as position_name,
                CASE 
                    WHEN e.employment_status = 1 THEN 'Regular'
                    WHEN e.employment_status = 2 THEN 'Probationary'
                    WHEN e.employment_status = 3 THEN 'Contractual'
                    WHEN e.employment_status = 4 THEN 'Part-time'
                    ELSE 'Unknown'
                END as employment_status_name,
                -- Calculate tenure at resignation
                EXTRACT(YEAR FROM AGE(e.end_date, e.start_date)) as tenure_years,
                EXTRACT(MONTH FROM AGE(e.end_date, e.start_date)) as tenure_months
            FROM core.main_employee_hr_details e
            LEFT JOIN core.sys_affiliate_companies c ON e.company_id = c.autoid
            LEFT JOIN core.main_employee_positions_definitions p ON e.position_id = p.position_id
            WHERE e.record_status = false 
            AND e.end_date >= @StartOfMonth 
            AND e.end_date <= @EndOfMonth
            AND (@CompanyId IS NULL OR e.company_id = @CompanyId)
            ORDER BY e.end_date DESC";

                var parameters = new
                {
                    CompanyId = companyId,
                    StartOfMonth = startOfMonth,
                    EndOfMonth = endOfMonth
                };

                return await connection.QueryAsync(query, parameters);
            });
        }














        ///
    }


    /// REPORTS
    /// 


}