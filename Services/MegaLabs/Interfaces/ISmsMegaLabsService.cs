using VSMSWebServer.Models;

namespace VSMSWebServer.Services.MegaLabs.Interfaces
{
    public interface ISmsMegaLabsService
    {
        Task<int> SendSmsAsync(SendSmsMegaLabsRequest request);
    }
}
