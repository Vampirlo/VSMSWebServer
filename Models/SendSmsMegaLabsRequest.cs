using System.Text.Json.Serialization;

namespace VSMSWebServer.Models
{
    public class SendSmsMegaLabsRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string SenderName { get; set; }
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
        public string CallbackServerURL { get; set; }
        public string CallbackServerPort { get; set; }
        public string Uuid { get; set; }
       
        public bool ProxyEnabled { get; set; }
        public string ProxyAddress { get; set; }
        public string ProxyPort { get; set; }
        public string ProxyLogin { get; set; }
        public string ProxyPassword { get; set; }


        // db extra info
        public string? FirstName { get; set; }
        public string? SecondName { get; set; }
        public string? LastName { get; set; }
        public string? SendTime { get; set; }
        [JsonIgnore]
        public long UpdatedAt { get; set; }
    }
}
