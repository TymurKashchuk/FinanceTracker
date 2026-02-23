using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinanceTracker.wpf.Data;
using FinanceTracker.wpf.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.wpf.Services
{
    public class FinanceService : IFinanceService
    {
        public async Task<List<Transaction>> GetTransactionsAsync()
        {
            using var db = new AppDbContext();
            return await db.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            using var db = new AppDbContext();

            var account = await db.Accounts.FirstOrDefaultAsync();
            if (account == null)
            {
                account = new Account { Name = "Default account", InitialBalance = 0 };
                db.Accounts.Add(account);
                await db.SaveChangesAsync();
            }

            var category = await db.Categories.FirstOrDefaultAsync();
            if (category == null)
            {
                category = new Category { Name = "General" };
                db.Categories.Add(category);
                await db.SaveChangesAsync();
            }

            transaction.AccountId = account.Id;
            transaction.CategoryId = category.Id;

            db.Transactions.Add(transaction);
            await db.SaveChangesAsync();
        }

        public async Task DeleteTransactionAsync(int id)
        {
            using var db = new AppDbContext();
            var entity = await db.Transactions.FindAsync(id);
            if (entity != null)
            {
                db.Transactions.Remove(entity);
                await db.SaveChangesAsync();
            }
        }

        public async Task<List<Account>> GetAccountsAsync() {
            using var db = new AppDbContext();
            return await db.Accounts
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        public async Task<List<Category>> GetCategoriesAsync() {
            using var db = new AppDbContext();
            return await db.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}
