using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinanceTracker.wpf.Data;
using FinanceTracker.wpf.Models;
using Microsoft.EntityFrameworkCore;
using static FinanceTracker.wpf.Services.FinanceService;

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

        public class AccountBalanceDto { 
            public int AccountId { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Balance { get; set; }
        }

        public async Task<List<AccountBalanceDto>> GetAccountBalancesAsync()
        {
            using var db = new AppDbContext();

            var accounts = await db.Accounts.ToListAsync();
            var transactions = await db.Transactions.ToListAsync();

            var balances = accounts
                .Select(a => new AccountBalanceDto
                {
                    AccountId = a.Id,
                    Name = a.Name,
                    Balance =
                        a.InitialBalance +
                        transactions
                            .Where(t => t.AccountId == a.Id)
                            .Sum(t => t.IsIncome ? t.Amount : -t.Amount)
                })
                .ToList();

            return balances;
        }

        public class CategorySummaryDto { 
            public int CategoryId { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsIncome { get; set; }
            public decimal TotalAmount { get; set; }
        }

        public async Task<List<CategorySummaryDto>> GetCategorySummariesAsync(DateTime? from = null, DateTime? to = null) {

            using var db = new AppDbContext();
            var transactions = await db.Transactions
        .Include(t => t.Category)
        .ToListAsync();

            if (from.HasValue)
                transactions = transactions.Where(t => t.Date >= from.Value).ToList();
            if (to.HasValue)
                transactions = transactions.Where(t => t.Date <= to.Value).ToList();

            var categories = await db.Categories.ToListAsync();

            var summaries = categories
                .Select(c => new CategorySummaryDto
                {
                    CategoryId = c.Id,
                    Name = c.Name,
                    IsIncome = c.IsIncome,
                    TotalAmount = transactions
                        .Where(t => t.CategoryId == c.Id)
                        .Sum(t => t.IsIncome ? t.Amount : -t.Amount)
                })
                .Where(s => s.TotalAmount != 0)  // тільки категорії, де були рухи
                .OrderByDescending(s => Math.Abs(s.TotalAmount))
                .ToList();

            return summaries;
        }

    }
}
