using System.Collections.Generic;
using System.Threading.Tasks;
using FinanceTracker.wpf.Models;
using static FinanceTracker.wpf.Services.FinanceService;

namespace FinanceTracker.wpf.Services
{
    public interface IFinanceService
    {
        Task<List<Transaction>> GetTransactionsAsync();
        Task AddTransactionAsync(Transaction transaction);
        Task DeleteTransactionAsync(int id);
        Task<List<Account>> GetAccountsAsync();
        Task<List<Category>> GetCategoriesAsync();
        Task SeedAsync();
        Task<List<AccountBalanceDto>> GetAccountBalancesAsync();
        Task<List<CategorySummaryDto>> GetCategorySummariesAsync(DateTime? from = null, DateTime? to = null);
    }
}
