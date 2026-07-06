namespace VSMSWebServer.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; }
        public string PasswordHash { get; set; } // НЕ хранить пароль как есть!
        public string Role { get; set; }
    }
}
