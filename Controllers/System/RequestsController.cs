using System.Text.Json;
using CoreHRAPI.Controllers.Dashboard;
using CoreHRAPI.Data;
using CoreHRAPI.Handlers;
using CoreHRAPI.Models.Configuration;
using CoreHRAPI.Models.Global;
using CoreHRAPI.Models.User;
using CoreHRAPI.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace CoreHRAPI.Controllers.Global
{
    [ApiController]
    [Route("api/v1/requests")]
    public class RequestsController : ControllerBase
    {
        private readonly ILogger<UserDetailsController> _logger;
        private readonly GlobalRepository _globalRepository;
        private readonly EmailService _emailService;
        private readonly RequestsHandler _requestsHandler;

        public RequestsController(ILogger<UserDetailsController> logger, EmailService emailService, GlobalRepository globalRepository, RequestsHandler requestsHandler)
        {
            _logger = logger;
            _globalRepository = globalRepository;
            _emailService = emailService;
            _requestsHandler = requestsHandler;
        }


        // --------------------------------------------------------------- SUBMIT REQUEST API HERE ----------------------------------------- //
        [HttpPost("submit-request")]
        public async Task<IActionResult> SubmitRequest(
        [FromQuery] string initiatorId,
        [FromQuery] long requestType,
        [FromBody] JsonElement payload)  // Use JsonElement from System.Text.Json
        {
            try
            {
                // Convert JsonElement to JObject
                var jObject = JObject.Parse(payload.ToString());
                var messageJson = jObject.ToString(Formatting.None);
                var refNo = await _globalRepository.InsertRequestAsync(initiatorId, requestType, messageJson);

                if (string.IsNullOrEmpty(refNo))
                    return StatusCode(500, "Failed to submit request");

                return Ok(new { Message = "Request submitted for approval", RefNo = refNo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting request");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("admin/approve")]
        public async Task<IActionResult> ApproveRequest([FromQuery] string refno)
        {
            try
            {
                var request = await _globalRepository.GetRequestByRefNoAsync(refno);
                if (request == null)
                    return NotFound("Request not found");

                if (request.is_approved)
                    return BadRequest("Request already approved");

                if (string.IsNullOrEmpty(request.request_message))
                    return BadRequest("Request data or intent is empty");

                try
                {
                    var jsonObject = JObject.Parse(request.request_message);

                    // Delegate approval logic to handler
                    (var success, string errorMessage) = await _requestsHandler.HandleAsync(request.request_type,refno, jsonObject);


                    if (!success)
                        return StatusCode(500, errorMessage);

                    var marked = await _globalRepository.MarkRequestAsApprovedAsync(refno);
                    if (!marked)
                        return StatusCode(500, "Failed to mark request as approved");

                    return Ok(new { Message = "Request approved and changes applied", RefNo = refno });
                }
                catch (JsonReaderException ex)
                {
                    _logger.LogError(ex, "Invalid JSON in request message: {Message}", request.request_message);
                    return BadRequest("Invalid request message format");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing request message: {Message}", request.request_message);
                    return StatusCode(500, "Error processing request");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving request");
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpPost("admin/reject")]
        public async Task<IActionResult> RejectRequest([FromQuery] string refno)
        {
            try
            {
                var request = await _globalRepository.GetRequestByRefNoAsync(refno);
                if (request == null) return NotFound("Request not found");

                if (request.is_approved) return BadRequest("Request already approved");
                if (request.is_rejected) return BadRequest("Request already rejected");

                var result = await _globalRepository.MarkRequestAsRejectedAsync(refno);
                return result ? Ok("Request rejected successfully") : StatusCode(500, "Failed to reject request");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting request");
                return StatusCode(500, "Internal server error");
            }
        }



        [HttpGet("admin/get-pending-requests")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var requests = await _globalRepository.GetPendingRequestsAsync();
            return Ok(requests);
        }

        [HttpGet("admin/get-all-requests")]
        public async Task<IActionResult> GetAllRequests()
        {
            var requests = await _globalRepository.GetAllRequestsAsync();
            return Ok(requests);
        }







    }
}
