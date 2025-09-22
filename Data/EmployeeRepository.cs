// Data/EmployeeRepository.cs
using System.Data;
using System.Threading.Tasks;
using Dapper;
using CoreHRAPI.Models.Reports;
using CoreHRAPI.Models.Global;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CoreHRAPI.Models.BIS;

namespace CoreHRAPI.Data
{
    public class EmployeeRepository
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<EmployeeRepository> _logger;

        public EmployeeRepository(DatabaseContext dbContext, ILogger<EmployeeRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // Helper method to set session variable for audit trail
        private async Task SetAuditUserIdAsync(IDbConnection connection, string userId)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                try
                {
                    await connection.ExecuteAsync("SELECT set_config('app.userid', @UserId, true);", new { UserId = userId.Trim() });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set session userid, will use database user for audit trail");
                }
            }
        }

        // Helper method to insert employment history
        private async Task<long> InsertEmploymentHistoryAsync(IDbConnection connection, string empid, int recordType, object jsonPayload, string userId)
        {
            try
            {
                await SetAuditUserIdAsync(connection, userId);

                var query = @"
                    INSERT INTO core.main_employee_hr_employment_history (
                        empid, record_type, json_payload, date_updated, status
                    ) VALUES (
                        @EmpId, @RecordType, @JsonPayload::jsonb, @DateUpdated, @Status
                    ) RETURNING autoid";

                var parameters = new
                {
                    EmpId = empid,
                    RecordType = recordType,
                    JsonPayload = JsonConvert.SerializeObject(jsonPayload),
                    DateUpdated = DateTime.UtcNow,
                    Status = true
                };

                return await connection.QuerySingleAsync<long>(query, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting employment history for empid: {EmpId}", empid);
                throw;
            }
        }

        // Helper method for logging audit trails - FIXED VERSION
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
                // Safely serialize the data using Newtonsoft.Json (same as working method)
                string oldDataJson = null;
                string newDataJson = null;

                if (oldData != null)
                {
                    try
                    {
                        oldDataJson = JsonConvert.SerializeObject(oldData, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            DateFormatHandling = DateFormatHandling.IsoDateFormat,
                            DateTimeZoneHandling = DateTimeZoneHandling.Utc
                        });
                    }
                    catch (Exception serializeEx)
                    {
                        _logger.LogWarning(serializeEx, "Failed to serialize old data for record {RecordId}, using fallback", recordId);
                        oldDataJson = JsonConvert.SerializeObject(new { error = "Serialization failed - partial data only" });
                    }
                }

                if (newData != null)
                {
                    try
                    {
                        newDataJson = JsonConvert.SerializeObject(newData, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            DateFormatHandling = DateFormatHandling.IsoDateFormat,
                            DateTimeZoneHandling = DateTimeZoneHandling.Utc
                        });
                    }
                    catch (Exception serializeEx)
                    {
                        _logger.LogWarning(serializeEx, "Failed to serialize new data for record {RecordId}, using fallback", recordId);
                        newDataJson = JsonConvert.SerializeObject(new { error = "Serialization failed - partial data only" });
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

        // Transfer Employee
        public async Task<bool> TransferEmployeeAsync(EmployeeTransferModel transfer, string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                await SetAuditUserIdAsync(connection, userId);

                // Get current employee data before update
                var currentEmployee = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT * FROM core.main_employee_hr_details WHERE empid = @EmpId AND record_status = true",
                    new { EmpId = transfer.empid });

                if (currentEmployee == null)
                {
                    return false;
                }

                var newCompanyId = transfer.to_company;

               

                // Update employee company_id
                var updateQuery = @"
                    UPDATE core.main_employee_hr_details 
                    SET company_id = @NewCompanyId, date_registered = @EffectiveDate
                    WHERE empid = @EmpId AND record_status = true";

                var updateParameters = new
                {
                    EmpId = transfer.empid,
                    NewCompanyId = newCompanyId,
                    EffectiveDate = transfer.effective_date
                };

                var rowsAffected = await connection.ExecuteAsync(updateQuery, updateParameters);

                if (rowsAffected > 0)
                {
                    // Get updated employee data for audit trail
                    var updatedEmployee = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT * FROM core.main_employee_hr_details WHERE empid = @EmpId AND record_status = true",
                        new { EmpId = transfer.empid });

                    // Log audit trail - FIXED CALL
                    await LogAuditTrailAsync(connection, "main_employee_hr_details",
                        Convert.ToInt64(currentEmployee.autoid), "TRANSFER EMPLOYEE", userId,
                        currentEmployee, updatedEmployee,
                        $"Employee {transfer.empid} transferred from {transfer.from_company} to {transfer.to_company}");

                    // Insert employment history
                    await InsertEmploymentHistoryAsync(connection, transfer.empid, 4, new
                    {
                        from_company = transfer.from_company,
                        to_company = transfer.to_company,
                        effective_date = transfer.effective_date.ToString("yyyy-MM-dd"),
                        remarks = transfer.remarks
                    }, userId);
                }

                return rowsAffected > 0;
            });
        }

        // Promote Employee
        public async Task<bool> PromoteEmployeeAsync(EmployeePromotionModel promotion, string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                await SetAuditUserIdAsync(connection, userId);

                // Get current employee data before update
                var currentEmployee = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT * FROM core.main_employee_hr_details WHERE empid = @EmpId AND record_status = true",
                    new { EmpId = promotion.empid });

                if (currentEmployee == null)
                {
                    return false;
                }

                // Get new position_id
                var newPositionId = promotion.new_position;

               
                // Update employee position_id
                var updateQuery = @"
                    UPDATE core.main_employee_hr_details 
                    SET position_id = @NewPositionId, date_registered = @EffectiveDate
                    WHERE empid = @EmpId AND record_status = true";

                var updateParameters = new
                {
                    EmpId = promotion.empid,
                    NewPositionId = newPositionId,
                    EffectiveDate = promotion.effective_date
                };

                var rowsAffected = await connection.ExecuteAsync(updateQuery, updateParameters);

                if (rowsAffected > 0)
                {
                    // Get updated employee data for audit trail
                    var updatedEmployee = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT * FROM core.main_employee_hr_details WHERE empid = @EmpId AND record_status = true",
                        new { EmpId = promotion.empid });

                    // Log audit trail - FIXED CALL
                    await LogAuditTrailAsync(connection, "main_employee_hr_details",
                        Convert.ToInt64(currentEmployee.autoid), "PROMOTE EMPLOYEE", userId,
                        currentEmployee, updatedEmployee,
                        $"Employee {promotion.empid} promoted from {promotion.old_position} to {promotion.new_position}");

                    // Insert employment history
                    await InsertEmploymentHistoryAsync(connection, promotion.empid, 2, new
                    {
                        old_position = promotion.old_position,
                        new_position = promotion.new_position,
                        effective_date = promotion.effective_date.ToString("yyyy-MM-dd"),
                        remarks = promotion.remarks
                    }, userId);
                }

                return rowsAffected > 0;
            });
        }
        // Regularize Employee
        public async Task<bool> RegularizeEmployeeAsync(EmployeeRegularizationModel regularization, string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                await SetAuditUserIdAsync(connection, userId);

                // Get current employee data before update
                var currentEmployee = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT * FROM core.main_employee_hr_details WHERE empid = @EmpId AND record_status = true",
                    new { EmpId = regularization.empid });

                if (currentEmployee == null)
                {
                    return false;
                }

                var regularizationDate = DateTime.UtcNow; // Use current date for regularization

                // Update employee employment_status to 1 (regular)
                var updateQuery = @"
            UPDATE core.main_employee_hr_details 
            SET employment_status = 1, date_registered = @RegularizationDate
            WHERE empid = @EmpId AND record_status = true";

                var updateParameters = new
                {
                    EmpId = regularization.empid,
                    RegularizationDate = regularizationDate
                };

                var rowsAffected = await connection.ExecuteAsync(updateQuery, updateParameters);

                if (rowsAffected > 0)
                {
                    // Get updated employee data for audit trail
                    var updatedEmployee = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT * FROM core.main_employee_hr_details WHERE empid = @EmpId AND record_status = true",
                        new { EmpId = regularization.empid });

                    // Log audit trail
                    await LogAuditTrailAsync(connection, "main_employee_hr_details",
                        Convert.ToInt64(currentEmployee.autoid), "REGULARIZE EMPLOYEE", userId,
                        currentEmployee, updatedEmployee,
                        $"Employee {regularization.empid} regularized from probationary to regular status");

                    // Insert employment history using the employee's actual start_date
                    await InsertEmploymentHistoryAsync(connection, regularization.empid, 3, new
                    {
                        start_date = currentEmployee.start_date?.ToString("yyyy-MM-dd"), // Use actual start_date from employee record
                        regularization_date = regularizationDate.ToString("yyyy-MM-dd"), // Use current date
                        remarks = regularization.remarks
                    }, userId);
                }

                return rowsAffected > 0;
            });
        }
        // Offboard Employee
        public async Task<bool> OffboardEmployeeAsync(EmployeeOffboardingModel offboarding, string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                await SetAuditUserIdAsync(connection, userId);

                // Get current employee data before update
                var currentEmployee = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT * FROM core.main_employee_hr_details WHERE empid = @EmpId AND record_status = true",
                    new { EmpId = offboarding.empid });

                if (currentEmployee == null)
                {
                    return false;
                }

                // Update employee record_status to false (soft delete) and set end_date
                var updateQuery = @"
                    UPDATE core.main_employee_hr_details 
                    SET record_status = false, 
                        active_status = false, 
                        end_date = @EndDate,
                        date_registered = @LastDay
                    WHERE empid = @EmpId AND record_status = true";

                var updateParameters = new
                {
                    EmpId = offboarding.empid,
                    EndDate = DateTime.UtcNow, // Set end_date to current date
                    LastDay = offboarding.last_day
                };

                var rowsAffected = await connection.ExecuteAsync(updateQuery, updateParameters);

                if (rowsAffected > 0)
                {
                    // Get updated employee data for audit trail
                    var updatedEmployee = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT * FROM core.main_employee_hr_details WHERE empid = @EmpId AND record_status = false",
                        new { EmpId = offboarding.empid });

                    // Log audit trail - FIXED CALL
                    await LogAuditTrailAsync(connection, "main_employee_hr_details",
                        Convert.ToInt64(currentEmployee.autoid), "OFFBOARD EMPLOYEE", userId,
                        currentEmployee, updatedEmployee,
                        $"Employee {offboarding.empid} offboarded - Reason: {offboarding.reason}");

                    // Insert employment history
                    await InsertEmploymentHistoryAsync(connection, offboarding.empid, 5, new
                    {
                        last_day = offboarding.last_day.ToString("yyyy-MM-dd"),
                        end_date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        reason = offboarding.reason,
                        exit_interview_done = offboarding.exit_interview_done
                    }, userId);
                }

                return rowsAffected > 0;
            });
        }

        // Add Appraisal
        public async Task<bool> AddEmployeeAppraisalAsync(EmployeeAppraisalModel appraisal, string userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                await SetAuditUserIdAsync(connection, userId);

                // Get current employee data for audit trail
                var currentEmployee = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT * FROM core.main_employee_hr_details WHERE empid = @EmpId AND record_status = true",
                    new { EmpId = appraisal.empid });

                if (currentEmployee == null)
                {
                    return false;
                }

                // Insert employment history for appraisal
                var historyId = await InsertEmploymentHistoryAsync(connection, appraisal.empid, 2, new
                {
                    appraisal_date = appraisal.appraisal_date.ToString("yyyy-MM-dd"),
                    appraisal_type = appraisal.appraisal_type,
                    remarks = appraisal.remarks
                }, userId);

                // Log audit trail for appraisal - FIXED CALL
                await LogAuditTrailAsync(connection, "main_employee_hr_employment_history",
                    historyId, "ADD APPRAISAL", userId, null, new
                    {
                        empid = appraisal.empid,
                        appraisal_date = appraisal.appraisal_date,
                        appraisal_type = appraisal.appraisal_type,
                        remarks = appraisal.remarks
                    },
                    $"Appraisal added for employee {appraisal.empid} - Type: {appraisal.appraisal_type}");

                return true;
            });
        }

        // Get Employment History
        public async Task<IEnumerable<EmploymentHistoryModel>> GetEmploymentHistoryAsync(string empid)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                    SELECT 
                        autoid, empid, record_type, json_payload, date_updated, status
                    FROM core.main_employee_hr_employment_history 
                    WHERE empid = @EmpId AND status = true
                    ORDER BY date_updated DESC";

                return await connection.QueryAsync<EmploymentHistoryModel>(query, new { EmpId = empid });
            });
        }

        // Get Employment History by Type
        public async Task<IEnumerable<EmploymentHistoryModel>> GetEmploymentHistoryByTypeAsync(string empid, int recordType)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                    SELECT 
                        autoid, empid, record_type, json_payload, date_updated, status
                    FROM core.main_employee_hr_employment_history 
                    WHERE empid = @EmpId AND record_type = @RecordType AND status = true
                    ORDER BY date_updated DESC";

                return await connection.QueryAsync<EmploymentHistoryModel>(query, new { EmpId = empid, RecordType = recordType });
            });
        }







        // REGULARIZATION CONTROLS

        // Get employees nearing regularization (within 30 days of 180-day mark)
        public async Task<IEnumerable<dynamic>> GetEmployeesNearingRegularizationAsync(int? companyId = null, int daysBeforeDeadline = 30)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
            SELECT 
                e.autoid,
                e.empid,
                CONCAT(COALESCE(e.fname, ''), ' ', COALESCE(e.mname, ''), ' ', COALESCE(e.sname, '')) as full_name,
                e.fname,
                e.mname,
                e.sname,
                e.start_date,
                e.employment_status,
                e.company_id,
                c.company_name,
                p.position_desc as position_name,
                -- Calculate days since start using simple date arithmetic
                (CURRENT_DATE - e.start_date) as days_since_start,
                -- Calculate days until regularization deadline (180 days)
                180 - (CURRENT_DATE - e.start_date) as days_until_regularization,
                -- Calculate regularization deadline date
                e.start_date + 180 as regularization_deadline,
                -- Check if overdue
                CASE 
                    WHEN (CURRENT_DATE - e.start_date) >= 180 THEN 'OVERDUE'
                    WHEN (CURRENT_DATE - e.start_date) >= 150 THEN 'DUE_SOON'
                    WHEN (CURRENT_DATE - e.start_date) >= 120 THEN 'APPROACHING'
                    ELSE 'EARLY'
                END as regularization_status,
                -- Employment status description
                CASE 
                    WHEN e.employment_status = 1 THEN 'Regular'
                    WHEN e.employment_status = 2 THEN 'Probationary'
                    WHEN e.employment_status = 3 THEN 'Contractual'
                    WHEN e.employment_status = 4 THEN 'Part-time'
                    ELSE 'Unknown'
                END as employment_status_name
            FROM core.main_employee_hr_details e
            LEFT JOIN core.sys_affiliate_companies c ON e.company_id = c.autoid
            LEFT JOIN core.main_employee_positions_definitions p ON e.position_id = p.position_id AND p.status = true
            WHERE e.record_status = true 
            AND e.employment_status = 2  -- Only probationary employees
            AND (@CompanyId IS NULL OR e.company_id = @CompanyId)
            AND (CURRENT_DATE - e.start_date) >= (180 - @DaysBeforeDeadline)
            ORDER BY 
                CASE 
                    WHEN (CURRENT_DATE - e.start_date) >= 180 THEN 1  -- Overdue first
                    WHEN (CURRENT_DATE - e.start_date) >= 150 THEN 2  -- Due soon
                    WHEN (CURRENT_DATE - e.start_date) >= 120 THEN 3  -- Approaching
                    ELSE 4  -- Early
                END,
                e.start_date ASC";

                var parameters = new
                {
                    CompanyId = companyId,
                    DaysBeforeDeadline = daysBeforeDeadline
                };

                return await connection.QueryAsync(query, parameters);
            });
        }

        // Get regularization statistics
        public async Task<dynamic> GetRegularizationStatisticsAsync(int? companyId = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
            WITH probationary_employees AS (
                SELECT 
                    e.autoid,
                    e.empid,
                    e.start_date,
                    e.company_id,
                    c.company_name,
                    (CURRENT_DATE - e.start_date) as days_since_start,
                    CASE 
                        WHEN (CURRENT_DATE - e.start_date) >= 180 THEN 'OVERDUE'
                        WHEN (CURRENT_DATE - e.start_date) >= 150 THEN 'DUE_SOON'
                        WHEN (CURRENT_DATE - e.start_date) >= 120 THEN 'APPROACHING'
                        ELSE 'EARLY'
                    END as regularization_status
                FROM core.main_employee_hr_details e
                LEFT JOIN core.sys_affiliate_companies c ON e.company_id = c.autoid
                WHERE e.record_status = true 
                AND e.employment_status = 2  -- Only probationary employees
                AND (@CompanyId IS NULL OR e.company_id = @CompanyId)
            )
            SELECT 
                COUNT(*) as total_probationary,
                COUNT(CASE WHEN regularization_status = 'OVERDUE' THEN 1 END) as overdue_count,
                COUNT(CASE WHEN regularization_status = 'DUE_SOON' THEN 1 END) as due_soon_count,
                COUNT(CASE WHEN regularization_status = 'APPROACHING' THEN 1 END) as approaching_count,
                COUNT(CASE WHEN regularization_status = 'EARLY' THEN 1 END) as early_count,
                ROUND(
                    COUNT(CASE WHEN regularization_status = 'OVERDUE' THEN 1 END) * 100.0 / NULLIF(COUNT(*), 0), 2
                ) as overdue_percentage
            FROM probationary_employees";

                var parameters = new
                {
                    CompanyId = companyId
                };

                return await connection.QueryFirstOrDefaultAsync(query, parameters);
            });
        }

        // Get employees by regularization status
        public async Task<IEnumerable<dynamic>> GetEmployeesByRegularizationStatusAsync(int? companyId = null, string status = null)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
            SELECT 
                e.autoid,
                e.empid,
                CONCAT(COALESCE(e.fname, ''), ' ', COALESCE(e.mname, ''), ' ', COALESCE(e.sname, '')) as full_name,
                e.fname,
                e.mname,
                e.sname,
                e.start_date,
                e.company_id,
                c.company_name,
                p.position_desc as position_name,
                (CURRENT_DATE - e.start_date) as days_since_start,
                180 - (CURRENT_DATE - e.start_date) as days_until_regularization,
                e.start_date + 180 as regularization_deadline,
                CASE 
                    WHEN (CURRENT_DATE - e.start_date) >= 180 THEN 'OVERDUE'
                    WHEN (CURRENT_DATE - e.start_date) >= 150 THEN 'DUE_SOON'
                    WHEN (CURRENT_DATE - e.start_date) >= 120 THEN 'APPROACHING'
                    ELSE 'EARLY'
                END as regularization_status
            FROM core.main_employee_hr_details e
            LEFT JOIN core.sys_affiliate_companies c ON e.company_id = c.autoid
            LEFT JOIN core.main_employee_positions_definitions p ON e.position_id = p.position_id AND p.status = true
            WHERE e.record_status = true 
            AND e.employment_status = 2  -- Only probationary employees
            AND (@CompanyId IS NULL OR e.company_id = @CompanyId)
            AND (@Status IS NULL OR 
                CASE 
                    WHEN (CURRENT_DATE - e.start_date) >= 180 THEN 'OVERDUE'
                    WHEN (CURRENT_DATE - e.start_date) >= 150 THEN 'DUE_SOON'
                    WHEN (CURRENT_DATE - e.start_date) >= 120 THEN 'APPROACHING'
                    ELSE 'EARLY'
                END = @Status)
            ORDER BY 
                CASE 
                    WHEN (CURRENT_DATE - e.start_date) >= 180 THEN 1
                    WHEN (CURRENT_DATE - e.start_date) >= 150 THEN 2
                    WHEN (CURRENT_DATE - e.start_date) >= 120 THEN 3
                    ELSE 4
                END,
                e.start_date ASC";

                var parameters = new
                {
                    CompanyId = companyId,
                    Status = status
                };

                return await connection.QueryAsync(query, parameters);
            });
        }

    }





}