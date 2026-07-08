using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VSMSWebServer.Models;
using VSMSWebServer.Services;
using VSMSWebServer.Services.MegaLabs.Interfaces;

namespace VSMSWebServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MegaLabsController : ControllerBase
    {
        private readonly ISmsMegaLabsService _smsService;

        public MegaLabsController(ISmsMegaLabsService smsService, RequestLoggerService requestLogger)
        {
            _smsService = smsService;
        }

        [HttpPost("sendSMS")]
        public async Task<IActionResult> SendSms([FromBody] SendSmsMegaLabsRequest request)
        {
            var result = await _smsService.SendSmsAsync(request);

            return Ok(new { statusCode = result });
        }
    }
}
