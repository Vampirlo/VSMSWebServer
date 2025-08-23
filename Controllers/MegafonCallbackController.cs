using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using VCallbackServer.Services;

namespace VCallbackServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // /api/MegafonCallback
    public class MegafonCallbackController : ControllerBase
    {
        private readonly RequestLoggerService _requestLogger;
        private readonly RequestRepositoryService _requestRepository;

        public MegafonCallbackController(RequestLoggerService requestLogger, RequestRepositoryService requestRepository)
        {
            _requestLogger = requestLogger;
            _requestRepository = requestRepository;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] DeliveryReport deliveryReport)
        {
            _requestLogger.LogIncomingRequest(HttpContext, deliveryReport);

            string msgId = deliveryReport.msg_id;
            string status = deliveryReport.status;

            bool updateSuccess = await _requestRepository.UpdateRequestStatusAsync(msgId, status);

            _requestLogger.LogRequestStatusUpdate(msgId, status, updateSuccess);


            HttpContext.Response.ContentType = "text/plain";
            HttpContext.Response.ContentLength = 2; // "OK" = 2 байта

            return Ok("OK");
        }
    }

    public class DeliveryReport
    {
        public string msg_id { get; set; }
        public string receipted_message_id { get; set; }
        public string status { get; set; }
        public string short_message { get; set; }
    }
}