using Microsoft.EntityFrameworkCore;
using SK_AccountingMoney.Data;
using SK_AccountingMoney.Models;

namespace SK_AccountingMoney.Services
{
    public class BalanceService : IBalanceService
    {
        private readonly AppDbContext _context;
        private const string MAIN_BALANCE_NAME = "Main";

        public BalanceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserByTelegramIdAsync(long telegramId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.TelegramId == telegramId);
        }

        /// <summary>
        ///Get or create shared balance
        /// </summary>
        private async Task<SharedBalance> GetOrCreateSharedBalanceAsync()
        {
            var sharedBalance = await _context.SharedBalances
                .FirstOrDefaultAsync(sb => sb.Name == MAIN_BALANCE_NAME);

            if (sharedBalance == null)
            {
                sharedBalance = new SharedBalance
                {
                    Name = MAIN_BALANCE_NAME,
                    Balance = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.SharedBalances.Add(sharedBalance);
                await _context.SaveChangesAsync();
            }

            return sharedBalance;
        }

        public async Task<decimal> GetSharedBalanceAsync(long telegramId)
        {
            var sharedBalance = await GetOrCreateSharedBalanceAsync();
            return sharedBalance?.Balance ?? 0;
        }

        public async Task<bool> DepositAsync(long telegramId, decimal amount, string? description = null)
        {
            if (amount <= 0)
                return false;

            var user = await GetUserByTelegramIdAsync(telegramId);
            if (user == null)
                return false;


            var sharedBalance = await GetOrCreateSharedBalanceAsync();

            // Update shared balance
            sharedBalance.Balance += amount;
            sharedBalance.UpdatedAt = DateTime.UtcNow;

            var transaction = new Transaction
            {
                UserId = user.Id,
                Amount = amount,
                Type = "deposit",
                Description = description ?? "Balance replenishment"
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> WithdrawAsync(long telegramId, decimal amount, string? description = null)
        {
            if (amount <= 0)
                return false;

            var user = await GetUserByTelegramIdAsync(telegramId);
            
            var sharedBalance = await GetOrCreateSharedBalanceAsync();

            if (user == null || sharedBalance.Balance < amount)
                return false;            

            // Update shared balance
            sharedBalance.Balance -= amount;
            sharedBalance.UpdatedAt = DateTime.UtcNow;

            var transaction = new Transaction
            {
                UserId = user.Id,
                Amount = amount,
                Type = "withdraw",
                Description = description ?? "Withdrawal of funds"
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Transaction>> GetAllTransactionAsync(int limit = 150)
        {
            return await _context.Transactions
                .Include(t => t.User)                
                .OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetMonthlyTransactionsAsync(DateTime startDate, DateTime endDate)
        {
           return await _context.Transactions
            .Include(t => t.User)
            .Where(t => t.Type == "withdraw"
                && t.CreatedAt >= startDate
                && t.CreatedAt <= endDate)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        }
    }
}
