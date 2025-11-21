using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SK_AccountingMoney.Data;

namespace SK_AccountingMoney.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] TelegramLoginRequest request)
        {
            // Checking the existence of a user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.TelegramId == request.TelegramId);

            if (user == null)
            {
                return Unauthorized(new { error = "User is not registered in the system" });
            }

            // In a production application, Telegram data validation should be performed here using the hash and the bot token.

            // Setting cookies for authentication
            Response.Cookies.Append("telegram_id", request.TelegramId.ToString(), new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            Response.Cookies.Append("telegram_hash", request.Hash ?? "validated", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            return Ok(new
            {
                success = true,
                user = new
                {
                    id = user.Id,
                    telegramId = user.TelegramId,
                    userName = user.UserName                                     
                }
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("telegram_id");
            Response.Cookies.Delete("telegram_hash");

            return Ok(new { success = true, message = "Exit from system" });
        }

        [HttpGet("check")]
        public async Task<IActionResult> CheckAuth()
        {
            var telegramId = Request.Cookies["telegram_id"];

            if (string.IsNullOrEmpty(telegramId))
                return Unauthorized(new { authenticated = false });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.TelegramId == long.Parse(telegramId));

            if (user == null)
                return Unauthorized(new { authenticated = false });

            return Ok(new
            {
                authenticated = true,
                user = new
                {
                    id = user.Id,
                    telegramId = user.TelegramId,
                    userName = user.UserName                                      
                }
            });
        }
    }

    public class TelegramLoginRequest
    {
        public long TelegramId { get; set; }
        public string UserName { get; set; }
        public string Hash { get; set; }
    }
}