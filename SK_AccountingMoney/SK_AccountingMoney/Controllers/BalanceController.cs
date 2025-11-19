using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SK_AccountingMoney.Services;

namespace SK_AccountingMoney.Controllers
{
    [Route("api/[controller]")]
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
            return (long)(HttpContext.Items["TelegramId"] ?? 0);
        }

        [HttpGet]
        public async Task<IActionResult> GetBalance()
        {
            var telegramId = GetTelegramId();
            var balance = await _balanceService.GetBalanceAsync(telegramId);

            return Ok(new { balance });
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUser()
        {
            var telegramId = GetTelegramId();
            var user = await _balanceService.GetUserByTelegramIdAsync(telegramId);

            if (user == null)
                return NotFound();

            return Ok(new
            {
                id = user.Id,
                telegramId = user.TelegramId,
                userName = user.UserName,
                balance = user.Balance               
            });
        }

        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] TransactionRequest request)
        {
            if (request.Amount <= 0)
                return BadRequest(new { error = "Сумма должна быть больше 0" });

            var telegramId = GetTelegramId();
            var success = await _balanceService.DepositAsync(telegramId, request.Amount, request.Description);

            if (!success)
                return BadRequest(new { error = "Ошибка при пополнении баланса" });

            var newBalance = await _balanceService.GetBalanceAsync(telegramId);
            return Ok(new { success = true, balance = newBalance, message = "Баланс пополнен" });
        }

        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] TransactionRequest request)
        {
            if (request.Amount <= 0)
                return BadRequest(new { error = "Сумма должна быть больше 0" });

            var telegramId = GetTelegramId();
            var success = await _balanceService.WithdrawAsync(telegramId, request.Amount, request.Description);

            if (!success)
                return BadRequest(new { error = "Недостаточно средств или ошибка при снятии" });

            var newBalance = await _balanceService.GetBalanceAsync(telegramId);
            return Ok(new { success = true, balance = newBalance, message = "Средства сняты" });
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int limit = 50)
        {
            var telegramId = GetTelegramId();
            var transactions = await _balanceService.GetTransactionHistoryAsync(telegramId, limit);

            return Ok(transactions.Select(t => new
            {
                id = t.Id,
                amount = t.Amount,                
                description = t.Description,
                createdAt = t.CreatedAt
            }));
        }
    }

    public class TransactionRequest
    {
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}

