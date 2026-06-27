using System.Collections.Concurrent;
using VSMSWebServer.Models;
using VSMSWebServer.Services.MegaLabs.Interfaces;

namespace VSMSWebServer.Services.MegaLabs
{
    public class SmsQueueManager : ISmsMegaLabsService
    {
        private readonly ConcurrentQueue<QueueItem> _queue = new();
        private readonly SemaphoreSlim _signal = new(0);
        private readonly SmsMegaLabsService _realService;
        private readonly ILogger<SmsQueueManager> _logger;
        private readonly double _pduPerSecond;
        private const double COEFFICIENT = 1.1;
        private readonly IServiceScopeFactory _scopeFactory;

        public SmsQueueManager(SmsMegaLabsService realService, ILogger<SmsQueueManager> logger, double pduPerSecond, IServiceScopeFactory scopeFactory)
        {
            _realService = realService;
            _logger = logger;
            _pduPerSecond = pduPerSecond;
            _scopeFactory = scopeFactory;
        }

        public double PduPerSecond => _pduPerSecond;

        public async Task<int> SendSmsAsync(SendSmsMegaLabsRequest request)
        {
            var parts = SplitMessage(request);
            foreach (var part in parts) // здесь запись в бд
            {
                var bdRequest = ConvertToRequest(part);
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<RequestRepositoryService>();
                await repo.AddBulkRequestsIfNotExistAsync(new List<Request> { bdRequest });

                var pduCount = CalculatePduCount(part.Message);
                _queue.Enqueue(new QueueItem { Request = part, PduCount = pduCount });
                _logger.LogInformation("Enqueued SMS part UUID={Uuid}, PDU={PduCount}", part.Uuid, pduCount);
            }
            _signal.Release(parts.Count);
            return 202; // Accepted
        }

        private List<SendSmsMegaLabsRequest> SplitMessage(SendSmsMegaLabsRequest original)
        {
            var parts = new List<SendSmsMegaLabsRequest>();
            string message = original.Message;
            if (string.IsNullOrEmpty(message))
                return parts;

            // Определяем кодировку и размер одного сегмента
            bool isCyrillic = IsCyrillic(message);
            int charsPerSegment = isCyrillic ? 67 : 134; // Максимальная длина сегмента

            // Максимальное количество сегментов в одном запросе (по инструкции)
            int maxSegmentsPerRequest = (int)(2 * _pduPerSecond);
            if (maxSegmentsPerRequest < 1)
                maxSegmentsPerRequest = 1; // Защита от нулевого/отрицательного значения

            // 1. Разбиваем сообщение на сегменты
            List<string> segments = new List<string>();
            int totalLen = message.Length;
            for (int i = 0; i < totalLen; i += charsPerSegment)
            {
                int length = Math.Min(charsPerSegment, totalLen - i);
                segments.Add(message.Substring(i, length));
            }

            // 2. Группируем сегменты по maxSegmentsPerRequest
            for (int i = 0; i < segments.Count; i += maxSegmentsPerRequest)
            {
                var group = segments.Skip(i).Take(maxSegmentsPerRequest).ToList();
                string combinedMessage = string.Join("", group); // Склеиваем сегменты

                var partRequest = new SendSmsMegaLabsRequest
                {
                    Login = original.Login,
                    Password = original.Password,
                    SenderName = original.SenderName,
                    PhoneNumber = original.PhoneNumber,
                    Message = combinedMessage,
                    CallbackServerURL = original.CallbackServerURL,
                    CallbackServerPort = original.CallbackServerPort,
                    Uuid = Guid.NewGuid().ToString("N").Substring(0, 16),
                    ProxyEnabled = original.ProxyEnabled,
                    ProxyAddress = original.ProxyAddress,
                    ProxyPort = original.ProxyPort,
                    ProxyLogin = original.ProxyLogin,
                    ProxyPassword = original.ProxyPassword,
                    FirstName = original.FirstName,
                    SecondName = original.SecondName,
                    LastName = original.LastName,
                    SendTime = original.SendTime,
                    UpdatedAt = original.UpdatedAt
                };
                parts.Add(partRequest);
            }

            return parts;
        }

        private bool IsCyrillic(string text)
        {
            foreach (char c in text)
                if (c > 127) return true;
            return false;
        }

        private int CalculatePduCount(string message)
        {
            if (string.IsNullOrEmpty(message)) return 0;
            bool isCyrillic = IsCyrillic(message);
            int maxSingle = isCyrillic ? 70 : 140;
            if (message.Length <= maxSingle)
                return 1;
            int maxPerSegment = isCyrillic ? 67 : 134;
            return (int)Math.Ceiling((double)message.Length / maxPerSegment);
        }

        public bool TryDequeue(out QueueItem item) => _queue.TryDequeue(out item);
        public Task WaitForSignalAsync(CancellationToken cancellationToken) => _signal.WaitAsync(cancellationToken);

        public class QueueItem
        {
            public SendSmsMegaLabsRequest Request { get; set; }
            public int PduCount { get; set; }
        }

        public Request ConvertToRequest(SendSmsMegaLabsRequest source)
        {
            return new Request
            {
                FirstName = source.FirstName,
                SecondName = source.SecondName,
                LastName = source.LastName,
                PhoneNumber = source.PhoneNumber,
                Uuid = source.Uuid,
                Message = source.Message,
                SendTime = source.SendTime,
                UpdatedAt = source.UpdatedAt,
                Status = ""
            };
        }
    }
}
