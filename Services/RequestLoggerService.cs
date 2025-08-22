using System.Text.Json;

namespace VCallbackServer.Services
{
    public class RequestLoggerService
    {
        private readonly ILogger<RequestLoggerService> _logger;

        // Получаем логгер через конструктор (Dependency Injection)
        public RequestLoggerService(ILogger<RequestLoggerService> logger)
        {
            _logger = logger;
        }

        // Главный метод, который будет логировать весь запрос
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