using SK_AccountingMoney.Models;

namespace SK_AccountingMoney.Services
{
    public interface IBalanceService
    {
        Task<User> GetUserByTelegramIdAsync(long telegramId);
        Task<decimal> GetSharedBalanceAsync(long telegramId);
        Task<bool> DepositAsync(long telegramId, decimal amount, string description = null);
        Task<bool> WithdrawAsync(long telegramId, decimal amount, string description = null);
        Task<List<Transaction>> GetAllTransactionAsync(int limit = 150);
    }
}
