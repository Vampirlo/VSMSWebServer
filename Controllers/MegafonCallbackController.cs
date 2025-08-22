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

        public MegafonCallbackController(RequestLoggerService requestLogger)
        {
            _requestLogger = requestLogger;
        }

        [HttpPost]
        public IActionResult Post([FromBody] DeliveryReport deliveryReport)
        {
            _requestLogger.LogIncomingRequest(HttpContext, deliveryReport);


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