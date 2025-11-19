using Microsoft.EntityFrameworkCore;
using SK_AccountingMoney.Data;
using SK_AccountingMoney.Models;

namespace SK_AccountingMoney.Services
{
    public class BalanceService : IBalanceService
    {
        private readonly AppDbContext _context;

        public BalanceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserByTelegramIdAsync(long telegramId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.TelegramId == telegramId);
        }

        public async Task<decimal> GetBalanceAsync(long telegramId)
        {
            var user = await GetUserByTelegramIdAsync(telegramId);
            return user?.Balance ?? 0;
        }

        public async Task<bool> DepositAsync(long telegramId, decimal amount, string description = null)
        {
            if (amount <= 0)
                return false;

            var user = await GetUserByTelegramIdAsync(telegramId);
            if (user == null)
                return false;

            user.Balance += amount;

            var transaction = new Transaction
            {
                UserId = user.Id,
                Amount = amount,                
                Description = description ?? "Пополнение баланса"
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> WithdrawAsync(long telegramId, decimal amount, string description = null)
        {
            if (amount <= 0)
                return false;

            var user = await GetUserByTelegramIdAsync(telegramId);
            if (user == null || user.Balance < amount)
                return false;

            user.Balance -= amount;

            var transaction = new Transaction
            {
                UserId = user.Id,
                Amount = amount,                
                Description = description ?? "Снятие средств"
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Transaction>> GetTransactionHistoryAsync(long telegramId, int limit = 50)
        {
            var user = await GetUserByTelegramIdAsync(telegramId);
            if (user == null)
                return new List<Transaction>();

            return await _context.Transactions
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
    }
}
