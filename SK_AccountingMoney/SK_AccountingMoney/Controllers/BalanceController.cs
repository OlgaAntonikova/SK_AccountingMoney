using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SK_AccountingMoney.Services;

namespace SK_AccountingMoney.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class BalanceController : ControllerBase
    {
        private readonly IBalanceService _balanceService;

        public BalanceController(IBalanceService balanceService)
        {
            _balanceService = balanceService;
        }

        private long GetTelegramId()
        {
            var telegramIdClaim = User.Claims.FirstOrDefault(c => c.Type == "TelegramId")?.Value;

            if (long.TryParse(telegramIdClaim, out long telegramId))
            {
                return telegramId;
            }

            throw new UnauthorizedAccessException("Invalid Telegram ID");
        }

        private long GetTelegramIdFromCookie()
        {
            var telegramId = Request.Cookies["telegram_id"];

            if (string.IsNullOrEmpty(telegramId))
                return 0;

            return long.TryParse(telegramId, out var id) ? id : 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetBalance()
        {
            var telegramId = GetTelegramId();
            var balance = await _balanceService.GetSharedBalanceAsync(telegramId);

            return Ok(new { balance });
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUser()
        {
            var telegramId = GetTelegramId();
            var user = await _balanceService.GetUserByTelegramIdAsync(telegramId);

            if (user == null)
                return NotFound();

            var sharedBalance = await _balanceService.GetSharedBalanceAsync(telegramId);

            return Ok(new
            {
                id = user.Id,
                telegramId = user.TelegramId,
                userName = user.UserName,
                balance = sharedBalance
            });
        }

        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] TransactionRequest request)
        {
            if (request.Amount <= 0)
                return BadRequest(new { error = "Amount must be greater than 0" });

            var telegramId = GetTelegramId();
            var success = await _balanceService.DepositAsync(telegramId, request.Amount, request.Description);

            if (!success)
                return BadRequest(new { error = "Error while replenishing balance" });

            var newBalance = await _balanceService.GetSharedBalanceAsync(telegramId);
            return Ok(new { success = true, balance = newBalance, message = "Balance has been replenished" });
        }

        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] TransactionRequest request)
        {
            if (request.Amount <= 0)
                return BadRequest(new { error = "Amount must be greater than 0" });

            var telegramId = GetTelegramId();
            var success = await _balanceService.WithdrawAsync(telegramId, request.Amount, request.Description);

            if (!success)
                return BadRequest(new { error = "Insufficient funds or withdrawal error" });

            var newBalance = await _balanceService.GetSharedBalanceAsync(telegramId);
            return Ok(new { success = true, balance = newBalance, message = "Funds withdrawn" });
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int limit = 150)
        {
            var telegramId = GetTelegramId();
            var transactions = await _balanceService.GetAllTransactionAsync(limit);

            return Ok(transactions.Select(t => new
            {
                id = t.Id,
                amount = t.Amount,
                type = t.Type,
                description = t.Description,
                createdAt = t.CreatedAt,
                userName = t.User?.UserName
            }));
        }

        [HttpGet("monthly-expenses")]
        public async Task<IActionResult> GetMonthlyExpenses([FromQuery] int year, int month)
        {
            try
            {
                var telegramId = GetTelegramId();
                var user = await _balanceService.GetUserByTelegramIdAsync(telegramId);

                if (user == null)
                    return NotFound();

                if (year < 2000 || year > 2100)
                    return BadRequest(new { error = "Incorrect year" });

                if (month < 1 || month > 12)
                    return BadRequest(new { error = "Incorrect month" });

                var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
                var endDate = startDate.AddMonths(1).AddSeconds(-1);

                // Get monthly withdraw transaction
                var transactions = await _balanceService.GetMonthlyTransactionsAsync(startDate, endDate);

                // Calculate total expenses
                var totalExpenses = transactions.Sum(t => t.Amount);

                // Get month name 
                var monthName = new DateTime(year, month, 1).ToString("MMMM",
                    System.Globalization.CultureInfo.InvariantCulture);


                return Ok(new
                {
                    year,
                    month,
                    monthName, 
                    totalExpenses,                    
                    transactions = transactions.Select(t => new
                    {
                        id = t.Id,
                        amount = t.Amount,
                        description = t.Description,
                        userName = t.User?.UserName,
                        createdAt = t.CreatedAt
                    })
                });
            }

            catch (Exception ex)
            {                
                return StatusCode(500, new { error = "Server error", details = ex.Message });
            }

        }
    }

    public class TransactionRequest
    {
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}

