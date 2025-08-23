using System.Text.Json;

namespace VSMSWebServer.Services
{
    public class RequestLoggerService
    {
        private readonly ILogger<RequestLoggerService> _logger;

        // /api/MegafonCallback DB status write logging
        public RequestLoggerService(ILogger<RequestLoggerService> logger)
        {
            _logger = logger;
        }

        public void LogRequestStatusUpdate(string uuid, string status, bool success, string? errorMessage = null)
        {
            if (success)
                _logger.LogInformation("Status update SUCCESS: UUID={Uuid}, Status={Status}", uuid, status);
            else
                _logger.LogWarning("Status update FAILED: UUID={Uuid}, Status={Status}, Error={ErrorMessage}", uuid, status, errorMessage ?? "Request not found");
        }

        // /api/MegafonCallback Main logging 
        public void LogIncomingRequest(HttpContext context, object requestBody)
        {
            _logger.LogInformation("=== NEW REQUEST ===");
            _logger.LogInformation("Time: {Time}", DateTime.Now);
            _logger.LogInformation("Client IP-address: {RemoteIpAddress}", context.Connection.RemoteIpAddress);
            _logger.LogInformation("Method: {Method}", context.Request.Method);
            _logger.LogInformation("Path: {Path}", context.Request.Path);

            LogHeaders(context.Request.Headers);
            LogRequestBody(requestBody);

            context.Response.OnCompleted(() =>
            {
                _logger.LogInformation("Response sent: Status {StatusCode}", context.Response.StatusCode);
                return Task.CompletedTask;
            });

            _logger.LogInformation("=== END OF REQUEST ===");
        }

        private void LogHeaders(IHeaderDictionary headers)
        {
            foreach (var header in headers)
            {
                _logger.LogInformation("Header: {Key} = {Value}", header.Key, header.Value);
            }
        }

        private void LogRequestBody(object body)
        {
            try
            {
                var jsonBody = JsonSerializer.Serialize(body);
                _logger.LogInformation("Request body (JSON): {Body}", jsonBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to serialize the request body in JSON");
            }
        }
    }
}