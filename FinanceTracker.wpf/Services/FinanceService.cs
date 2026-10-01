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
            await db.Database.EnsureCreatedAsync();

            transaction.Account = null!;
            transaction.Category = null;

            if (!await db.Accounts.AnyAsync() || !await db.Categories.AnyAsync())
            {
                await SeedInternalAsync(db);
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
            await db.Database.EnsureCreatedAsync();
            await SeedInternalAsync(db);
        }

        private static async Task SeedInternalAsync(AppDbContext db)
        {
            if (!await db.Accounts.AnyAsync())
            {
                db.Accounts.AddRange(
                    new Account { Name = "Готівка" },
                    new Account { Name = "Monobank" },
                    new Account { Name = "Revolut" }
                );
            }

            if (!await db.Categories.AnyAsync())
            {
                db.Categories.AddRange(
                    new Category { Name = "Продукти", IsIncome = false },
                    new Category { Name = "Кафе та ресторани", IsIncome = false },
                    new Category { Name = "Транспорт", IsIncome = false },
                    new Category { Name = "Житло та комуналка", IsIncome = false },
                    new Category { Name = "Розваги", IsIncome = false },
                    new Category { Name = "Здоров'я", IsIncome = false },
                    new Category { Name = "Покупки", IsIncome = false },
                    new Category { Name = "Зарплата", IsIncome = true },
                    new Category { Name = "Фриланс", IsIncome = true },
                    new Category { Name = "Інвестиції", IsIncome = true },
                    new Category { Name = "Подарунок", IsIncome = true },
                    new Category { Name = "Інше", IsIncome = false }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.MonthlyBudgets.AnyAsync())
            {
                var now = DateTime.Now;
                var expenseCats = await db.Categories.Where(c => !c.IsIncome).ToListAsync();
                var budgetMap = new Dictionary<string, decimal>
                {
                    { "Продукти", 8000m },
                    { "Житло та комуналка", 6000m },
                    { "Кафе та ресторани", 3000m },
                    { "Транспорт", 2000m },
                    { "Розваги", 2500m },
                    { "Покупки", 4000m }
                };

                foreach (var cat in expenseCats)
                {
                    if (budgetMap.TryGetValue(cat.Name, out var planned))
                    {
                        db.MonthlyBudgets.Add(new MonthlyBudget
                        {
                            CategoryId = cat.Id,
                            Year = now.Year,
                            Month = now.Month,
                            PlannedAmount = planned
                        });
                    }
                }
                await db.SaveChangesAsync();
            }
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

        public async Task<List<Category>> GetCategoriesAsync(bool? isIncome = null)
        {
            using var db = new AppDbContext();
            var query = db.Categories.AsQueryable();
            if (isIncome.HasValue)
            {
                query = query.Where(c => c.IsIncome == isIncome.Value);
            }
            return await query
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

        public class CategoryBudgetDto
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public decimal PlannedAmount { get; set; }
            public decimal SpentAmount { get; set; }
            public decimal RemainingAmount => Math.Max(0, PlannedAmount - SpentAmount);
            public double ProgressPercentage => PlannedAmount > 0 ? (double)(SpentAmount / PlannedAmount * 100) : 0;
            public bool IsOverBudget => SpentAmount > PlannedAmount;
            public string StatusBadgeText => IsOverBudget 
                ? $"Перевитрата: {(SpentAmount - PlannedAmount):N0} ₴" 
                : $"Залишок: {RemainingAmount:N0} ₴";
        }

        public async Task<List<CategoryBudgetDto>> GetMonthlyBudgetsAsync(int year, int month)
        {
            using var db = new AppDbContext();
            var budgets = await db.MonthlyBudgets
                .Include(b => b.Category)
                .Where(b => b.Year == year && b.Month == month)
                .ToListAsync();

            if (!budgets.Any())
            {
                await SeedInternalAsync(db);
                budgets = await db.MonthlyBudgets
                    .Include(b => b.Category)
                    .Where(b => b.Year == year && b.Month == month)
                    .ToListAsync();
            }

            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1).AddTicks(-1);

            var transactions = await db.Transactions
                .Where(t => !t.IsIncome && t.Date >= start && t.Date <= end)
                .ToListAsync();

            var result = budgets.Select(b =>
            {
                var spent = transactions
                    .Where(t => t.CategoryId == b.CategoryId)
                    .Sum(t => t.Amount);

                return new CategoryBudgetDto
                {
                    CategoryId = b.CategoryId,
                    CategoryName = b.Category.Name,
                    PlannedAmount = b.PlannedAmount,
                    SpentAmount = spent
                };
            })
            .OrderByDescending(b => b.ProgressPercentage)
            .ToList();

            return result;
        }
    }
}
