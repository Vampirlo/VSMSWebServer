using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using VSMSWebServer.Data;
using VSMSWebServer.Services.JWT;

namespace VSMSWebServer.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;
        private readonly AppDbContext _db;

        public AuthController(JwtService jwtService, AppDbContext db)
        {
            _jwtService = jwtService;
            _db = db;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
                return Unauthorized();

            if (!VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized();

            var token = _jwtService.GenerateToken(user.Id.ToString(), user.Username, user.Role);

            return Ok(new { token });
        }

        private bool VerifyPassword(string password, string hash)
        {
            var hashed = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(password)));

            return hashed == hash;
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
