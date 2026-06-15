using System.Collections.Concurrent;

namespace VSMSWebServer.Services
{
    public class ClientSyncState
    {
        public string Ip { get; set; }
        public DateTime LastSyncTime { get; set; }      // последнее время отправки дельты
        public DateTime LastFullSyncTime { get; set; }  // последняя полная выгрузка
    }

    public class ClientSyncStateService
    {
        private readonly ConcurrentDictionary<string, ClientSyncState> _states = new();

        public ClientSyncState GetOrCreate(string ip)
        {
            return _states.GetOrAdd(ip, _ => new ClientSyncState
            {
                Ip = ip,
                LastSyncTime = DateTime.UtcNow,
                LastFullSyncTime = DateTime.UtcNow
            });
        }

        public void UpdateSyncTime(string ip, DateTime syncTime, bool isFullSync = false)
        {
            if (_states.TryGetValue(ip, out var state))
            {
                state.LastSyncTime = syncTime;
                if (isFullSync) state.LastFullSyncTime = syncTime;
            }
        }

        public bool NeedsFullSync(string ip)
        {
            if (!_states.TryGetValue(ip, out var state)) return true;
            return (DateTime.UtcNow - state.LastFullSyncTime).TotalHours >= 24;
        }
    }
}
