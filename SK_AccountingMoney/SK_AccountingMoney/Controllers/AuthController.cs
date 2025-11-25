using Microsoft.AspNetCore.Mvc;
using SK_AccountingMoney.Data;
using SK_AccountingMoney.Services;

namespace SK_AccountingMoney.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TelegramAuthService _telegramAuth;
        private readonly JwtService _jwtService;
        private readonly IBalanceService _balanceService;

        public AuthController(AppDbContext context, TelegramAuthService telegramAuth, JwtService jwtService, IBalanceService balanceService)
        {
            _context = context;
            _telegramAuth = telegramAuth;
            _jwtService = jwtService;
            _balanceService = balanceService;
        }

        [HttpPost("telegram")]
        public async Task<IActionResult> AuthenticateTelegram([FromBody] TelegramAuthRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.InitData))
                    return BadRequest(new { error = "InitData is required" });

                var validationResult = _telegramAuth.ValidateFull(request.InitData);

                if (!validationResult.IsValid)
                {
                    Console.WriteLine($"[AUTH] Validation failed: {validationResult.Error}");
                    return Unauthorized(new { error = "Invalid Telegram data", details = validationResult.Error });
                }

                var userData = validationResult.UserData!;                
                
                var telegramId = userData.Id;
                var username = userData.UserName;

                Console.WriteLine($"[AUTH] [TELEGRAM ID] {telegramId}");


                // Check user
                var user = await  _balanceService.GetUserByTelegramIdAsync(telegramId);
                if (user == null)
                {
                    return Unauthorized(new { error = "Invalid Telegram user" });
                }

                // Generating a JWT token
                var token = _jwtService.GenerateToken(user.Id, user.UserName);

                return Ok(new
                {
                    token,
                    user = new
                    {
                        id = user.Id,
                        telegramId = user.TelegramId,
                        userName = user.UserName
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }



        [HttpGet("check")]
        public async Task<IActionResult> CheckAuth()
        {
            try
            {
                var telegramIdClaim = User.Claims.FirstOrDefault(c => c.Type == "TelegramId")?.Value;
                if (!long.TryParse(telegramIdClaim, out long telegramId))
                {
                    return Ok(new { authenticated = false });
                }

                var user = await _balanceService.GetUserByTelegramIdAsync(telegramId);
                if (user == null)
                {
                    return Ok(new { authenticated = false });
                }                

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
            catch
            {
                return Ok(new { authenticated = false });
            }
        }
    }

    public class TelegramAuthRequest
    {
        public string InitData { get; set; } = string.Empty;
    }
}



