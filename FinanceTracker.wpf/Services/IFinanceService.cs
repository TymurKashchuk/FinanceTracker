using System.Collections.Generic;
using System.Threading.Tasks;
using FinanceTracker.wpf.Models;

namespace FinanceTracker.wpf.Services
{
    public interface IFinanceService
    {
        Task<List<Transaction>> GetTransactionsAsync();
        Task AddTransactionAsync(Transaction transaction);
        Task DeleteTransactionAsync(int id);
    }
}
