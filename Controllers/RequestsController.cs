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
                if (requests == null || !requests.Any())
                {
                    // in log
                    HttpContext.Response.ContentType = "text/plain";
                    HttpContext.Response.ContentLength = 20;
                    return BadRequest("No requests provided");
                }

                foreach (var request in requests)
                {
                    // сделать метод в RequestRepositoryService для записи без id первого
                    // request.Id, request.PhoneNumber, request.Status);
                    // проверять uuid, на предмет существования элемента уже в таблице
                    // если его нет, то делать запись без id
                    // если элеменет существует, то идёт он нахуй и не перезаписывает ничего
                }
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