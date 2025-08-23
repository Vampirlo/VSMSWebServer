using Microsoft.AspNetCore.Mvc;
using VSMSWebServer.Services;

namespace VSMSWebServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // /api/Requests
    public class RequestsController : ControllerBase
    {
        private readonly RequestRepositoryService _requestRepository;

        public RequestsController(RequestRepositoryService requestRepository)
        {
            _requestRepository = requestRepository;
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
    }
}