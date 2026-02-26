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

            if(!await db.Accounts.AnyAsync())
            {
                db.Accounts.AddRange(
                    new Account { Name = "Cash" },
                    new Account { Name = "Monobank" },
                    new Account { Name = "Revolut" }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.Categories.AnyAsync()) {
                db.Categories.AddRange(
                    new Category { Name = "General", IsIncome = false},
                    new Category { Name = "Food", IsIncome = false},
                    new Category { Name = "Transport", IsIncome = false },
                    new Category { Name = "Salary", IsIncome = true}
                );
                await db.SaveChangesAsync();
            }

            if (transaction.AccountId == 0)
            {
                var defaultAccount = await db.Accounts.OrderBy(a => a.Id).FirstAsync();
                transaction.AccountId = defaultAccount.Id;
            }

            if (transaction.CategoryId == 0)
            {
                var defaultCategory = await db.Categories.OrderBy(c => c.Id).FirstAsync();
                transaction.CategoryId = defaultCategory.Id;
            }

            db.Transactions.Add(transaction);
            await db.SaveChangesAsync();
        }

        public async Task SeedAsync()
        {
            using var db = new AppDbContext();

            if (!db.Accounts.Any())
            {
                db.Accounts.AddRange(
                    new Account { Name = "Cash" },
                    new Account { Name = "Monobank" },
                    new Account { Name = "Revolut" }
                );
            }

            if (!db.Categories.Any())
            {
                db.Categories.AddRange(
                    new Category { Name = "General" },
                    new Category { Name = "Food" },
                    new Category { Name = "Transport" },
                    new Category { Name = "Salary" }
                );
            }

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
