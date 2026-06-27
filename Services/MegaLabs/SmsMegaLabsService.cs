using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VSMSWebServer.Models;
using VSMSWebServer.Services.MegaLabs.Interfaces;

namespace VSMSWebServer.Services.MegaLabs
{
    public class SmsMegaLabsService : ISmsMegaLabsService
    {
        private readonly RequestLoggerService _logger;
        private readonly HttpClient _client;

        public SmsMegaLabsService(RequestLoggerService logger, HttpClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task<int> SendSmsAsync(SendSmsMegaLabsRequest request)
        {
            var url = "https://a2p-api.megalabs.ru/sms/v1/sms";

            var callbackUrl =
                $"https://{request.CallbackServerURL}:{request.CallbackServerPort}/api/MegafonCallback";

            // Basic Auth
            var auth = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{request.Login}:{request.Password}")
            );

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", auth);

            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var body = new
            {
                from = request.SenderName,
                to = long.Parse(request.PhoneNumber),
                message = request.Message,
                msg_id = request.Uuid,
                callback_url = callbackUrl
            };

            var json = JsonSerializer.Serialize(body);

            var response = await _client.PostAsync(
                url,
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"SEND MegaLabs SMS request -> Url: {url}, From: {request.SenderName}, To: {request.PhoneNumber}, Msg: {request.Message}, UUID: {request.Uuid}, CallbackUrl: {callbackUrl}");
            _logger.LogInformation($"MegaLabs response HTTP: {(int)response.StatusCode}");
            _logger.LogInformation($"MegaLabs response body: {responseBody}");

            return (int)response.StatusCode;
        }
    }
}