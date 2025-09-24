// Data/GlobalRepository.cs
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using KYCAPI.Models.Global;
using KYCAPI.Models.User;
using Newtonsoft.Json;
using System.Text.Json;


namespace KYCAPI.Data
{
    public class GlobalRepository
    {
        private readonly DatabaseContext _dbContext;

        public GlobalRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Notifications
        public async Task<bool> InsertNewNotification(string? userId, int actionType, bool is_unique, string notificationDesc)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                // First, get the next available notification_id
                int nextId = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COALESCE(MAX(notification_id), 0) + 1 FROM core.sys_notifications");

                var query = @"
                INSERT INTO core.sys_notifications 
                (notification_id, notification_description, action_type_id, is_unread, is_unique, user_id, status, date_notified) 
                VALUES 
                (@NotificationId, @NotificationDesc, @ActionType, true, @IsUnique, @UserId, true, CURRENT_TIMESTAMP)";

                var parameters = new
                {
                    NotificationId = nextId,
                    UserId = userId,
                    ActionType = actionType,
                    IsUnique = is_unique,
                    NotificationDesc = notificationDesc
                };

                var result = await connection.ExecuteAsync(query, parameters);
                return result > 0;
            });
        }

        public async Task<bool> UpdateToReadNotification(string? userId, int? notifId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                UPDATE core.sys_notifications
                SET is_unread = false
                WHERE user_id = @UserId AND notification_id = @notifId";

                var parameters = new
                {
                    UserId = userId,
                    notifId = notifId
                };

                var result = await connection.ExecuteAsync(query, parameters);
                return result > 0;
            });
        }

        public async Task<bool> UpdateToHiddenNotification(string? userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                UPDATE core.sys_notifications
                SET status = false
                WHERE user_id = @UserId";

                var parameters = new
                {
                    UserId = userId
                };

                var result = await connection.ExecuteAsync(query, parameters);
                return result > 0;
            });
        }
        public async Task<IEnumerable<AllNotificationsModel>> GetUserNotifications(string? userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                // Step 1: Check if the user is admin
                var adminCheckQuery = @"
            SELECT ut.user_type_desc, ut.user_type_id
            FROM core.main_user_position mup
            JOIN core.sys_user_positions_definitions upd ON mup.position_id = upd.position_id
            JOIN core.sys_user_types ut ON upd.user_type_id = ut.user_type_id
            WHERE mup.userid = @UserId
            LIMIT 1;";

                var userRole = await connection.QueryFirstOrDefaultAsync<(string user_type_desc, int user_type_id)>(
                    adminCheckQuery, new { UserId = userId });

                bool isAdmin = userRole.user_type_id == 1 || userRole.user_type_id == 2;

                // Step 2: Updated logic to include user, global, and admin-only notifications
                var notificationsQuery = @"
            SELECT n.*
            FROM core.sys_notifications n
            JOIN core.sys_notifications_settings s
                ON n.action_type_id = s.action_id
            WHERE n.status = true
              AND n.is_unread = true
              AND s.is_active = true
              AND (
                    n.user_id = @UserId
                    OR s.is_for_all_users = true
                    OR (@IsAdmin = TRUE AND s.if_for_admin_only = true)
                  );";

                var parameters = new
                {
                    UserId = userId,
                    IsAdmin = isAdmin
                };

                return await connection.QueryAsync<AllNotificationsModel>(notificationsQuery, parameters);
            });
        }



        public async Task<IEnumerable<SystemStatusModel>> GetSystemStatus()
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                SELECT 
                    c.moduleid AS moduleId, 
                    cm.module_desc, 
                    c.under_maintenance 
                FROM core.sys_module_status c 
                JOIN core.sys_modules_definitions cm 
                    ON c.moduleid = cm.moduleid";

                return await connection.QueryAsync<SystemStatusModel>(query);
            });
        }


        public async Task<RequestModel?> GetRequestByIdAsync(long requestId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = "SELECT * FROM core.cirs_requests WHERE autoid = @Id";
                return await connection.QueryFirstOrDefaultAsync<RequestModel>(query, new { Id = requestId });
            });
        }

        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task<string?> InsertRequestAsync(string initiatorId, long requestType, string requestMessage)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var refNo = "R" + DateTime.UtcNow.ToString("yyyyMMdd") + GenerateRandomString(11);
                var query = @"
                INSERT INTO core.cirs_requests (initiator_id, request_type, refno, request_message)
                VALUES (@InitiatorId, @RequestType, @RefNo, @RequestMessage)";
                var rows = await connection.ExecuteAsync(query, new
                {
                    InitiatorId = initiatorId,
                    RequestType = requestType,
                    RefNo = refNo,
                    RequestMessage = requestMessage
                });
                return rows > 0 ? refNo : null;
            });
        }
        public async Task<RequestModel?> GetRequestByRefNoAsync(string refNo)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = "SELECT * FROM core.cirs_requests WHERE refno = @RefNo";
                return await connection.QueryFirstOrDefaultAsync<RequestModel>(query, new { RefNo = refNo });
            });
        }

        public async Task<bool> MarkRequestAsApprovedAsync(string refNo)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = "UPDATE core.cirs_requests SET is_approved = true WHERE refno = @RefNo";
                var rows = await connection.ExecuteAsync(query, new { RefNo = refNo });
                return rows > 0;
            });
        }

        public async Task<IEnumerable<RequestModel>> GetPendingRequestsAsync()
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = "SELECT * FROM core.cirs_requests WHERE is_approved = false";
                return await connection.QueryAsync<RequestModel>(query);
            });
        }

        public async Task<IEnumerable<RequestModel>> GetAllRequestsAsync()
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = "SELECT * FROM core.cirs_requests ORDER BY createdat DESC";
                return await connection.QueryAsync<RequestModel>(query);
            });
        }

        public async Task<IEnumerable<BorrowerStatus>> GetAllBorrowerStatus()
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = "SELECT * FROM core.cirs_main_collection_sub_status_borrower";
                return await connection.QueryAsync<BorrowerStatus>(query);
            });
        }

        public async Task<IEnumerable<UnitStatus>> GetAllUnitStatus()
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = "SELECT * FROM core.cirs_main_collection_sub_status_unit";
                return await connection.QueryAsync<UnitStatus>(query);
            });
        }

        public async Task<IEnumerable<RemarksStatus>> GetAllRemarksStatus()
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = "SELECT * FROM core.cirs_main_collection_remarks";
                return await connection.QueryAsync<RemarksStatus>(query);
            });
        }

        public async Task<IEnumerable<SpecArea>> GetAllTelecollectorRecords(string? userId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                ";

                var parameters = new
                {
                    UserId = userId
                };

                return await connection.QueryAsync<SpecArea>(query, parameters);
            });
        }

        public async Task<bool> AssignStatusToRecord(MainRecordStatusModel model)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
            var query = @"
            INSERT INTO core.cirs_main_collection_status (
                main_remarkid,
                sub_status_borrowerid,
                sub_status_unitid,
                additional_remarks,
                requested_visit,
                pn_number,
                refno,
                updated_by,
                date_updated,
                status
            )
            VALUES (
                @MainRemarkId,
                @SubStatusBorrowerId,
                @SubStatusUnitId,
                @AdditionalRemarks,
                @RequestedVisit,
                @PnNumber,
                @RefNo,
                @UpdatedBy,
                NOW(),
                true
            )
            ON CONFLICT (pn_number, refno) DO UPDATE SET
                main_remarkid = EXCLUDED.main_remarkid,
                sub_status_borrowerid = EXCLUDED.sub_status_borrowerid,
                sub_status_unitid = EXCLUDED.sub_status_unitid,
                additional_remarks = EXCLUDED.additional_remarks,
                requested_visit = EXCLUDED.requested_visit,
                refno = EXCLUDED.refno,
                updated_by = EXCLUDED.updated_by,
                date_updated = NOW(),
                status = true;";

                var parameters = new
                {
                    MainRemarkId = model.main_remarkid,
                    SubStatusBorrowerId = model.sub_status_borrowerid,
                    SubStatusUnitId = model.sub_status_unitid,
                    AdditionalRemarks = model.additional_remarks,
                    RequestedVisit = model.requested_visit,
                    PnNumber = model.pn_number,
                    RefNo = model.refno,
                    UpdatedBy = model.updated_by
                };

                var result = await connection.ExecuteAsync(query, parameters);
                return result > 0;
            });
        }

        public async Task<bool> MarkRequestAsRejectedAsync(string refNo)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
            UPDATE core.cirs_requests
            SET is_rejected = true
            WHERE refno = @RefNo AND is_approved = false AND is_rejected = false";

                var rows = await connection.ExecuteAsync(query, new { RefNo = refNo });
                return rows > 0;
            });
        }

        public async Task<bool> MarkRequestAsUsedAsync(string refno)
        {
            return await _dbContext.ExecuteDapperAsync(async conn =>
            {
                var sql = @"UPDATE core.cirs_requests SET is_used = true WHERE refno = @RefNo";
                var result = await conn.ExecuteAsync(sql, new { RefNo = refno });
                return result > 0;
            });
        }

        public async Task<bool> LogMasterListUploadAsync(string uploaderId, string requestRefNo)
        {
            return await _dbContext.ExecuteDapperAsync(async conn =>
            {
                var sql = @"
            INSERT INTO core.cirs_uploads (uploader_id, request_id, uploaded_at)
            VALUES (@UploaderId, @RefNo, @UploadedAt)";

                var result = await conn.ExecuteAsync(sql, new
                {
                    UploaderId = uploaderId,
                    RefNo = requestRefNo,
                    UploadedAt = DateTime.UtcNow
                });

                return result > 0;
            });
        }


        public async Task<bool> LogUploadRequestAsync(string userId, string requestId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
            INSERT INTO core.cirs_uploads (uploader_id, request_id)
            VALUES (@UserId, @RequestId)";
                var rows = await connection.ExecuteAsync(query, new { UserId = userId, RequestId = requestId });
                return rows > 0;
            });
        }


        public async Task<UploadModel> GetLatestUploadAsync(string userId)
        {
        return await _dbContext.ExecuteDapperAsync(async conn =>
        {
            var sql = "SELECT * FROM core.cirs_uploads ORDER BY uploaded_at DESC LIMIT 1";
            
            return await conn.QueryFirstOrDefaultAsync<UploadModel>(sql, new { UserId = userId });
        });
        }

        public async Task<RequestModel> GetValidApprovedRequestAsync(string userId, DateTime lastUploadDate)
        {
            return await _dbContext.ExecuteDapperAsync(async conn =>
            {
                var sql = @"
                    SELECT * FROM core.cirs_requests
                    WHERE initiator_id = @UserId
                      AND is_approved = true
                      AND request_type = 6
                      AND is_used = false
                    ORDER BY createdat DESC
                    LIMIT 1
";
                return await conn.QueryFirstOrDefaultAsync<RequestModel>(sql, new { UserId = userId, LastUploadDate = lastUploadDate });
            });
        }

        public async Task<bool> SavePromptAsync(string userId, string promptMessage, long tokenCount)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                INSERT INTO core.sys_bot_prompt_table (userid, prompt_message, sent_at, token_count)
                VALUES (@UserId, @PromptMessage, NOW(), @TokenCount);
            ";

                var result = await connection.ExecuteAsync(query, new
                {
                    UserId = userId,
                    PromptMessage = promptMessage,
                    TokenCount = tokenCount
                });

                return result > 0;
            });
        }

        // User Dashboard Repos


        public async Task<DashboardConfig> GetDashboardConfig(string userId, string companyId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                SELECT 
                    id,
                    user_id as UserId,
                    company_id as CompanyId,
                    dashboard_config as DashboardConfigData,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt,
                    created_by as CreatedBy,
                    version as Version
                FROM core.sys_dashboard_config 
                WHERE user_id = @UserId AND company_id = @CompanyId";

                var parameters = new
                {
                    UserId = userId,
                    CompanyId = companyId
                };

                return await connection.QueryFirstOrDefaultAsync<DashboardConfig>(query, parameters);
            });
        }

        public async Task<IEnumerable<DashboardConfig>> GetDashboardConfigsByCompany(string companyId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                SELECT 
                    id,
                    user_id as UserId,
                    company_id as CompanyId,
                    dashboard_config as DashboardConfigData,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt,
                    created_by as CreatedBy,
                    version as Version
                FROM core.sys_dashboard_config 
                WHERE company_id = @CompanyId
                ORDER BY updated_at DESC";

                var parameters = new
                {
                    CompanyId = companyId
                };

                return await connection.QueryAsync<DashboardConfig>(query, parameters);
            });
        }

        public async Task<DashboardConfig> CreateDashboardConfig(DashboardConfig config)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                INSERT INTO core.sys_dashboard_config 
                (user_id, company_id, dashboard_config, created_by, version)
                VALUES (@UserId, @CompanyId, @DashboardConfigData, @CreatedBy, @Version)
                RETURNING 
                    id,
                    user_id as UserId,
                    company_id as CompanyId,
                    dashboard_config as DashboardConfigData,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt,
                    created_by as CreatedBy,
                    version as Version";

                var parameters = new
                {
                    config.UserId,
                    config.CompanyId,
                    DashboardConfigData = JsonConvert.SerializeObject(config.DashboardConfigData),
                    config.CreatedBy,
                    config.Version
                };

                return await connection.QueryFirstOrDefaultAsync<DashboardConfig>(query, parameters);
            });
        }

        public async Task<DashboardConfig> UpdateDashboardConfig(DashboardConfig config)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                UPDATE core.sys_dashboard_config 
                SET 
                    dashboard_config = @DashboardConfigData,
                    version = @Version,
                    updated_at = CURRENT_TIMESTAMP
                WHERE user_id = @UserId AND company_id = @CompanyId
                RETURNING 
                    id,
                    user_id as UserId,
                    company_id as CompanyId,
                    dashboard_config as DashboardConfigData,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt,
                    created_by as CreatedBy,
                    version as Version";

                var parameters = new
                {
                    config.UserId,
                    config.CompanyId,
                    DashboardConfigData = JsonConvert.SerializeObject(config.DashboardConfigData),
                    config.Version
                };

                return await connection.QueryFirstOrDefaultAsync<DashboardConfig>(query, parameters);
            });
        }

        public async Task<bool> DeleteDashboardConfig(string userId, string companyId)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                var query = @"
                DELETE FROM core.sys_dashboard_config 
                WHERE user_id = @UserId AND company_id = @CompanyId";

                var parameters = new
                {
                    UserId = userId,
                    CompanyId = companyId
                };

                var rowsAffected = await connection.ExecuteAsync(query, parameters);
                return rowsAffected > 0;






            });
        }


        public async Task<int> InsertAuditTrailAsync(
    IDbConnection connection,
    string tableName,
    long recordId,
    string action,
    string userid,
    object newData,
    string remarks = null)
        {
            var query = @"
        INSERT INTO core.sys_audit_trails (
            table_name, record_id, action, userid, old_data, new_data, action_date, remarks
        )
        VALUES (
            @table_name, @record_id, @action, @userid, NULL, @new_data::jsonb, NOW(), @remarks
        );";

            var parameters = new
            {
                table_name = tableName,
                record_id = recordId,
                action,
                userid,
                new_data = newData != null ? System.Text.Json.JsonSerializer.Serialize(newData) : null,
                remarks
            };

            return await connection.ExecuteAsync(query, parameters);
        }

        public async Task<int> LogUploadAttemptAsync(string userid, long companyId, object payload)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                return await InsertAuditTrailAsync(
                    connection,
                    tableName: "employee_masterfile",
                    recordId: companyId,              // you can treat companyId as recordId for attempt-level logs
                    action: "UPLOAD ATTEMPT",
                    userid: userid,
                    newData: payload,
                    remarks: "Employee masterfile upload attempt"
                );
            });
        }




        public async Task<int> LogUploadConfirmAsync(string userid, long companyId, object payload)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                return await InsertAuditTrailAsync(
                    connection,
                    tableName: "employee_masterfile",
                    recordId: companyId,
                    action: "UPLOAD CONFIRM",
                    userid: userid,
                    newData: payload,
                    remarks: "Employee masterfile upload confirm"
                );
            });
        }

        public async Task<int> LogUploadProcessAsync(string userid, long companyId, object payload)
        {
            return await _dbContext.ExecuteDapperAsync(async connection =>
            {
                return await InsertAuditTrailAsync(
                    connection,
                    tableName: "employee_masterfile",
                    recordId: companyId,
                    action: "UPLOAD PROCESS",
                    userid: userid,
                    newData: payload,
                    remarks: "Employee masterfile upload process"
                );
            });
        }









        //









    }


}