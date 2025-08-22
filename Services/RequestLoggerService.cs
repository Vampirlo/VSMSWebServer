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
            _logger.LogInformation("=== НОВЫЙ ЗАПРОС ===");
            _logger.LogInformation("Время: {Time}", DateTime.Now);
            _logger.LogInformation("IP-адрес клиента: {RemoteIpAddress}", context.Connection.RemoteIpAddress);
            _logger.LogInformation("Метод: {Method}", context.Request.Method);
            _logger.LogInformation("Путь: {Path}", context.Request.Path);

            LogHeaders(context.Request.Headers);
            LogRequestBody(requestBody);

            context.Response.OnCompleted(() =>
            {
                _logger.LogInformation("Отправлен ответ: Status {StatusCode}", context.Response.StatusCode);
                return Task.CompletedTask;
            });

            _logger.LogInformation("=== КОНЕЦ ЗАПРОСА ===");
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
                _logger.LogInformation("Тело запроса (JSON): {Body}", jsonBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось сериализовать тело запроса в JSON");
            }
        }
    }
}