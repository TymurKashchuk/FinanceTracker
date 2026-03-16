using System.Collections.Generic;
using System.IO;
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
        public async Task<List<Transaction>> GetTransactionsAsync(DateTime? from = null, DateTime? to =null)
        {
            using var db = new AppDbContext();
            var query = db.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
            as IQueryable<Transaction>;

            if (from.HasValue) query = query.Where(t => t.Date >= from.Value);
            if (to.HasValue) query = query.Where(t => t.Date <= to.Value);

            return await query
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            using var db = new AppDbContext();
            transaction.Account = null!;
            transaction.Category = null;

            if (!await db.Accounts.AnyAsync())
            {
                db.Accounts.AddRange(
                    new Account { Name = "Cash" },
                    new Account { Name = "Monobank" },
                    new Account { Name = "Revolut" }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.Categories.AnyAsync())
            {
                db.Categories.AddRange(
                    new Category { Name = "General", IsIncome = false },
                    new Category { Name = "Food", IsIncome = false },
                    new Category { Name = "Transport", IsIncome = false },
                    new Category { Name = "Salary", IsIncome = true }
                );
                await db.SaveChangesAsync();
            }

            if (transaction.AccountId == 0)
            {
                var defaultAccount = await db.Accounts.OrderBy(a => a.Id).FirstAsync();
                transaction.AccountId = defaultAccount.Id;
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

        public async Task<List<Account>> GetAccountsAsync()
        {
            using var db = new AppDbContext();
            return await db.Accounts
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            using var db = new AppDbContext();
            return await db.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public class AccountBalanceDto
        {
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

        public class CategorySummaryDto
        {
            public int CategoryId { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsIncome { get; set; }
            public decimal TotalAmount { get; set; }
        }

        public async Task<List<CategorySummaryDto>> GetCategorySummariesAsync(DateTime? from = null, DateTime? to = null)
        {
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
                .Where(s => s.TotalAmount != 0)
                .OrderByDescending(s => Math.Abs(s.TotalAmount))
                .ToList();

            return summaries;
        }

        public async Task ExportTransactionsToCsvAsync(string filePath, DateTime? from = null, DateTime? to = null) {
            using var db = new AppDbContext();
            var transactions = await db.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                .ToListAsync();

            if (from.HasValue) transactions = transactions.Where(t=>t.Date >= from.Value).ToList();
            if(to.HasValue) transactions = transactions.Where(t => t.Date <= to.Value).ToList();

            var csv = new List<string> { "Date,Description,Amount,Type,Account,Category" };

            foreach (var t in transactions.OrderBy(t => t.Date))
            {
                var type = t.IsIncome ? "Income" : "Expense";
                var amount = t.IsIncome ? $"+{t.Amount:F2}" : $"-{Math.Abs(t.Amount):F2}";
                var account = t.Account?.Name ?? "Unknown";
                var category = t.Category?.Name ?? "No category";

                csv.Add($"{t.Date:yyyy-MM-dd HH:mm},\"{t.Description}\",{amount},\"{type}\",\"{account}\",\"{category}\"");
            }

            await File.WriteAllLinesAsync(filePath, csv, System.Text.Encoding.UTF8);
        }
    }
}
