namespace VSMSWebServer.Services.MegaLabs
{
    public class SmsQueueBackgroundService : BackgroundService
    {
        private readonly SmsQueueManager _queueManager;
        private readonly SmsMegaLabsService _realService;
        private readonly ILogger<SmsQueueBackgroundService> _logger;

        public SmsQueueBackgroundService(SmsQueueManager queueManager, SmsMegaLabsService realService, ILogger<SmsQueueBackgroundService> logger)
        {
            _queueManager = queueManager;
            _realService = realService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _queueManager.WaitForSignalAsync(stoppingToken);

                while (_queueManager.TryDequeue(out var item))
                {
                    try
                    {
                        int statusCode = await _realService.SendSmsAsync(item.Request);
                        _logger.LogInformation("Sent SMS part UUID={Uuid}, Status={StatusCode}", item.Request.Uuid, statusCode);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending SMS part UUID={Uuid}", item.Request.Uuid);
                    }

                    // Задержка после отправки (включая случай ошибки)
                    double delaySeconds = (item.PduCount / _queueManager.PduPerSecond) * 1.1;
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                }
            }
        }
    }
}
