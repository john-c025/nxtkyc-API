using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KYCAPI.Models.KYC;
using System.Linq; 

namespace KYCAPI.Helpers
{
    public static class KYCHelper
    {
        /// <summary>
        /// Generate secure access token
        /// </summary>
        public static string GenerateSecureToken(int length = 32)
        {
            var tokenBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        /// <summary>
        /// Generate hash from token for storage
        /// </summary>
        public static string GenerateTokenHash(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }

               /// <summary>
        /// Generate KYC request ID: KYC + 12 random numbers
        /// </summary>
        public static string GenerateKYCRequestId()
        {
            var random = new Random();
            var randomNumbers = string.Join("", Enumerable.Range(0, 12).Select(_ => random.Next(0, 10)));
            return $"KYC{randomNumbers}";
        }

        /// <summary>
        /// Generate account code: company_code + 10 random digits
        /// </summary>
        public static string GenerateAccountCode(string companyCode)
        {
            var random = new Random();
            var randomNumbers = string.Join("", Enumerable.Range(0, 10).Select(_ => random.Next(0, 10)));
            return $"{companyCode}{randomNumbers}";
        }

        /// <summary>
        /// Generate account ID: Random numbers
        /// </summary>
        public static string GenerateAccountId()
        {
            var random = new Random();
            return string.Join("", Enumerable.Range(0, 15).Select(_ => random.Next(0, 10)));
        }

        /// <summary>
        /// Generate system user key: 10 random alphanumeric characters (different from user_id)
        /// </summary>
        public static string GenerateSystemUserKey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Generate user ID: Random numbers
        /// </summary>
        public static string GenerateUserId()
        {
            var random = new Random();
            return string.Join("", Enumerable.Range(0, 10).Select(_ => random.Next(0, 10)));
        }

        /// <summary>
        /// Validate file extension for KYC uploads
        /// </summary>
        public static bool IsValidFileExtension(string fileName)
        {
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".docx", ".doc", ".xlsx", ".xls" };
            var extension = Path.GetExtension(fileName).ToLower();
            return allowedExtensions.Contains(extension);
        }

        /// <summary>
        /// Get file type based on extension
        /// </summary>
        public static byte GetFileType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".pdf" => 1, // PDF Document
                ".jpg" or ".jpeg" or ".png" => 2, // Image
                ".docx" or ".doc" => 3, // Word Document
                ".xlsx" or ".xls" => 4, // Excel Document
                _ => 99 // Other
            };
        }

        /// <summary>
        /// Get file category based on extension and purpose
        /// </summary>
        public static byte GetFileCategory(string fileName, string? description = null)
        {
            var lowerDescription = description?.ToLower() ?? "";
            
            if (lowerDescription.Contains("id") || lowerDescription.Contains("identification"))
                return 1; // ID Documents
            
            if (lowerDescription.Contains("address") || lowerDescription.Contains("proof"))
                return 2; // Address Proof
            
            if (lowerDescription.Contains("income") || lowerDescription.Contains("salary") || lowerDescription.Contains("bank"))
                return 3; // Financial Documents
            
            if (lowerDescription.Contains("signature") || lowerDescription.Contains("authorization"))
                return 4; // Authorization Documents
            
            return 99; // General/Other
        }

        /// <summary>
        /// Calculate estimated processing time based on priority
        /// </summary>
        public static TimeSpan GetEstimatedProcessingTime(byte priorityLevel)
        {
            return priorityLevel switch
            {
                1 => TimeSpan.FromDays(7), // Low priority
                2 => TimeSpan.FromDays(5), // Medium priority
                3 => TimeSpan.FromDays(3), // High priority
                4 => TimeSpan.FromHours(24), // Urgent priority
                _ => TimeSpan.FromDays(5) // Default
            };
        }

        /// <summary>
        /// Get human-readable processing time
        /// </summary>
        public static string GetProcessingTimeDescription(byte priorityLevel)
        {
            return priorityLevel switch
            {
                1 => "5-7 business days",
                2 => "3-5 business days",
                3 => "1-3 business days",
                4 => "Within 24 hours",
                _ => "3-5 business days"
            };
        }

        /// <summary>
        /// Validate KYC request status transition
        /// </summary>
        public static bool IsValidStatusTransition(byte currentStatus, byte newStatus)
        {
            // Define valid status transitions
            var validTransitions = new Dictionary<byte, byte[]>
            {
                { 1, new byte[] { 2, 4, 5 } }, // Pending -> In Review, Rejected, Archived
                { 2, new byte[] { 3, 4, 5 } }, // In Review -> Approved, Rejected, Archived
                { 3, new byte[] { 5 } },       // Approved -> Archived
                { 4, new byte[] { 2, 5 } },    // Rejected -> In Review (reopen), Archived
                { 5, new byte[] { } }          // Archived -> No transitions (final state)
            };

            return validTransitions.ContainsKey(currentStatus) && 
                   validTransitions[currentStatus].Contains(newStatus);
        }

        /// <summary>
        /// Generate audit trail entry
        /// </summary>
        public static KYCAuditTrailModel CreateAuditEntry(
            string kycRequestId, 
            byte actionType, 
            string actionBy, 
            byte? oldStatus = null, 
            byte? newStatus = null, 
            string? actionDetails = null)
        {
            return new KYCAuditTrailModel
            {
                kyc_request_id = kycRequestId,
                action_type = actionType,
                action_by = actionBy,
                action_timestamp = DateTime.UtcNow,
                old_status = oldStatus,
                new_status = newStatus,
                action_details = actionDetails
            };
        }

        /// <summary>
        /// Serialize object to JSON for metadata storage
        /// </summary>
        public static string SerializeToJson(object obj)
        {
            try
            {
                return JsonSerializer.Serialize(obj, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });
            }
            catch
            {
                return "{}";
            }
        }

        /// <summary>
        /// Deserialize JSON to object
        /// </summary>
        public static T? DeserializeFromJson<T>(string json) where T : class
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Validate privilege level upgrade request
        /// </summary>
        public static bool IsValidPrivilegeLevelUpgrade(byte currentLevel, byte requestedLevel)
        {
            // Can only upgrade to the next level or stay at current level
            return requestedLevel > currentLevel && requestedLevel <= (currentLevel + 2);
        }

        /// <summary>
        /// Get privilege level name
        /// </summary>
        public static string GetPrivilegeLevelName(byte level)
        {
            return level switch
            {
                0 => "Basic",
                1 => "Bronze",
                2 => "Silver",
                3 => "Gold",
                4 => "Platinum",
                5 => "Diamond",
                _ => $"Level {level}"
            };
        }

        /// <summary>
        /// Calculate business days between two dates (excluding weekends)
        /// </summary>
        public static int CalculateBusinessDays(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
                return 0;

            var businessDays = 0;
            var currentDate = startDate.Date;

            while (currentDate <= endDate.Date)
            {
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    businessDays++;
                }
                currentDate = currentDate.AddDays(1);
            }

            return businessDays;
        }

        /// <summary>
        /// Get next business day (excluding weekends)
        /// </summary>
        public static DateTime GetNextBusinessDay(DateTime date, int businessDaysToAdd = 1)
        {
            var currentDate = date.Date;
            var addedDays = 0;

            while (addedDays < businessDaysToAdd)
            {
                currentDate = currentDate.AddDays(1);
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    addedDays++;
                }
            }

            return currentDate;
        }

        /// <summary>
        /// Mask sensitive data for logging
        /// </summary>
        public static string MaskSensitiveData(string data, int visibleChars = 4)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;

            if (data.Length <= visibleChars)
                return new string('*', data.Length);

            return data[..visibleChars] + new string('*', data.Length - visibleChars);
        }

        /// <summary>
        /// Generate summary statistics from KYC requests
        /// </summary>
        public static object GenerateKYCSummary(IEnumerable<KYCRequestModel> requests)
        {
            var requestsList = requests.ToList();
            
            return new
            {
                total_requests = requestsList.Count,
                pending_requests = requestsList.Count(r => r.request_status == 1),
                in_review_requests = requestsList.Count(r => r.request_status == 2),
                approved_requests = requestsList.Count(r => r.request_status == 3),
                rejected_requests = requestsList.Count(r => r.request_status == 4),
                archived_requests = requestsList.Count(r => r.request_status == 5),
                high_priority_requests = requestsList.Count(r => r.priority_level >= 3),
                average_processing_time_hours = requestsList
                    .Where(r => r.completed_at.HasValue)
                    .Select(r => (r.completed_at!.Value - r.submitted_at).TotalHours)
                    .DefaultIfEmpty(0)
                    .Average(),
                approval_rate = requestsList.Count(r => r.request_status == 3 || r.request_status == 4) > 0
                    ? (double)requestsList.Count(r => r.request_status == 3) / requestsList.Count(r => r.request_status == 3 || r.request_status == 4) * 100
                    : 0
            };
        }

        /// <summary>
        /// Format file size for display
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Create notification message for KYC status change
        /// </summary>
        public static string CreateStatusChangeMessage(string clientName, string kycRequestId, byte oldStatus, byte newStatus, string? remarks = null)
        {
            var oldStatusName = GetStatusName(oldStatus);
            var newStatusName = GetStatusName(newStatus);
            
            var message = $"KYC request {kycRequestId} for {clientName} has been updated from {oldStatusName} to {newStatusName}.";
            
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                message += $" Remarks: {remarks}";
            }
            
            return message;
        }

        /// <summary>
        /// Get status name from status code
        /// </summary>
        public static string GetStatusName(byte status)
        {
            return status switch
            {
                1 => "Pending",
                2 => "In Review",
                3 => "Approved",
                4 => "Rejected",
                5 => "Archived",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get action type name from action code
        /// </summary>
        public static string GetActionTypeName(byte actionType)
        {
            return actionType switch
            {
                1 => "Created",
                2 => "Approved",
                3 => "Rejected",
                4 => "Archived",
                5 => "Escalated",
                6 => "Updated",
                7 => "File Uploaded",
                8 => "File Verified",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Validate email format
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sanitize filename for storage
        /// </summary>
        public static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Length > 100 ? sanitized[..100] : sanitized;
        }
    }
}
