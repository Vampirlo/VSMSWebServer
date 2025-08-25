using Microsoft.AspNetCore.Mvc;
using VSMSWebServer.Models;
using VSMSWebServer.Services;

namespace VSMSWebServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // /api/Requests
    public class RequestsController : ControllerBase
    {
        private readonly RequestLoggerService _requestLogger;
        private readonly RequestRepositoryService _requestRepository;

        public RequestsController(RequestRepositoryService requestRepository, RequestLoggerService requestLogger)
        {
            _requestRepository = requestRepository;
            _requestLogger = requestLogger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRequests()
        {
            var requests = await _requestRepository.GetAllRequestsAsync();
            return Ok(requests);
        }

        [HttpGet("{uuid}")]
        public async Task<IActionResult> GetRequestByUuid(string uuid)
        {
            var request = await _requestRepository.GetRequestByUuidAsync(uuid);
            if (request == null)
                return NotFound();

            return Ok(request);
        }

        [HttpPost("uploadAll")]
        public async Task<IActionResult> PostAsync([FromBody] List<Request> requests)
        {
            _requestLogger.LogIncomingRequest(HttpContext, requests);

            try
            {
                if (requests == null || requests.Count == 0)
                {
                    // in log
                    HttpContext.Response.ContentType = "text/plain";
                    HttpContext.Response.ContentLength = 20;
                    return BadRequest("No requests provided");
                }

                int addedCount = await _requestRepository.AddBulkRequestsIfNotExistAsync(requests);
                int skippedCount = requests.Count - addedCount;

                _requestLogger.LogInformation($"Added {addedCount} new requests, skipped {skippedCount} existing requests");
            }
            catch (Exception ex)
            {
                // ex in log
                HttpContext.Response.ContentType = "text/plain";
                HttpContext.Response.ContentLength = 21;
                return StatusCode(500, "Internal server error");
            }

            HttpContext.Response.ContentType = "text/plain";
            HttpContext.Response.ContentLength = 2;
            return Ok("OK");
        }
    }
}