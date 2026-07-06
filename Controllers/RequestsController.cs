using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VSMSWebServer.Models;
using VSMSWebServer.Services;

namespace VSMSWebServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // /api/Requests
    [AllowAnonymous]
    public class RequestsController : ControllerBase
    {
        private readonly RequestLoggerService _requestLogger;
        private readonly RequestRepositoryService _requestRepository;
        private readonly ClientSyncStateService _clientStateService;
        private readonly ILogger<RequestsController> _logger;

        public RequestsController(RequestRepositoryService requestRepository, RequestLoggerService requestLogger, ClientSyncStateService clientStateService, ILogger<RequestsController> logger)
        {
            _requestRepository = requestRepository;
            _requestLogger = requestLogger;
            _clientStateService = clientStateService;
            _logger = logger;
        }

        [HttpGet("downloadAll")]
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
                HttpContext.Response.ContentLength = ex.ToString().Length;
                return StatusCode(500, ex.ToString());
            }

            HttpContext.Response.ContentType = "text/plain";
            HttpContext.Response.ContentLength = 2;
            return Ok("OK");
        }

        [HttpGet("sync")]
        public async Task<IActionResult> Sync()
        {
            try
            {
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logger.LogInformation("Sync called from IP: {Ip}", clientIp);
                if (string.IsNullOrEmpty(clientIp)) return BadRequest();

                var state = _clientStateService.GetOrCreate(clientIp);
                var needFull = (DateTime.UtcNow - state.LastFullSyncTime).TotalHours >= 24;

                List<Request> result;
                if (needFull)
                {
                    result = await _requestRepository.GetAllRequestsAsync();
                    state.LastFullSyncTime = DateTime.UtcNow;
                }
                else
                {
                    var sinceTimestamp = new DateTimeOffset(state.LastSyncTime).ToUnixTimeMilliseconds();
                    _logger.LogInformation("Fetching updates since {Timestamp}", sinceTimestamp);
                    result = await _requestRepository.GetRequestsUpdatedSinceAsync(sinceTimestamp);
                }
                state.LastSyncTime = DateTime.UtcNow;

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Sync method");
                throw;
            }
        }
    }
}