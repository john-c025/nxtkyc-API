using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using CoreHRAPI.Data;

namespace CoreHRAPI.Handlers
{
    public class RequestsHandler
    {
        private readonly GlobalRepository _globalRepository;

        public RequestsHandler(GlobalRepository globalRepository)
        {
            _globalRepository = globalRepository;
        }

        public async Task<(bool success, string errorMessage)> HandleAsync(long requestType, string? refno, JObject jsonObject)
        {
            switch (requestType)
            {
                case 1: // For UPDATING SPECIFIC AREA
                    string specAreaId = jsonObject["SpecAreaId"]?.ToString();
                    string subAreaId = jsonObject["SubAreaId"]?.ToString();
                    string name = jsonObject["Name"]?.ToString();
                    int? status = jsonObject["Status"]?.ToObject<int?>();

                    if (string.IsNullOrEmpty(specAreaId))
                        return (false, "SpecAreaId is required");
                    if (string.IsNullOrEmpty(subAreaId))
                        return (false, "SubAreaId is required");
                    if (string.IsNullOrEmpty(name))
                        return (false, "Name is required");
                    if (!status.HasValue)
                        return (false, "Status is required");

                    //var updated = await _globalRepository.UpdateSpecAreaFieldsAsync(
                    //    specAreaId, subAreaId, name, status.Value
                    //);

                    //if (!updated)
                    //    return (false, "Failed to apply update to spec area");

                    return (true, string.Empty);

                case 2: // Add New Spec Area
                    return (false, "Not implemented yet");

                case 3: // Update Main and Sub Loan Type
                    return (false, "Not implemented yet");

                case 4: // Reupload Masterlist (overwrite)
                    return (false, "Deprecated");

                case 5: // New Upload Masterlist (first time)
                    return (false, "Deprecated");

                case 6: // Upload Again (Approved re-upload)
                    return await HandleUploadAccessAsync(refno,jsonObject);

                default:
                    return (false, $"Unhandled request type: {requestType}");
            }
        }

        private async Task<(bool success, string errorMessage)> HandleUploadAccessAsync(string refno,JObject jsonObject)
        {
            var userId = jsonObject["UserId"]?.ToString();
            var requestId = refno;

            if (string.IsNullOrEmpty(userId))
                return (false, "UserId is required");

            if (string.IsNullOrEmpty(requestId))
                return (false, "Reference is required");

            var inserted = await _globalRepository.LogUploadRequestAsync(userId, requestId);
            if (!inserted)
                return (false, "Failed to log upload request access");

            return (true, string.Empty);
        }
    }
}
