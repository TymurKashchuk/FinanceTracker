using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using FinanceTracker.wpf.Models;
using FinanceTracker.wpf.Services;
using LiveCharts;
using LiveCharts.Wpf;

namespace FinanceTracker.wpf.ViewModels
{
    public partial class MainViewModel : INotifyPropertyChanged
    {
        private readonly IFinanceService _financeService;

        public ObservableCollection<Transaction> Transactions { get; } = new();
        public ObservableCollection<Transaction> FilteredTransactions { get; } = new();
        public ObservableCollection<Account> Accounts { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<Category> FilteredCategories { get; } = new();
        public ObservableCollection<TransactionType> TransactionTypes { get; } = new() { TransactionType.Expense, TransactionType.Income };
        public ObservableCollection<PeriodType> PeriodTypes { get; } = new() { PeriodType.Today, PeriodType.ThisWeek, PeriodType.ThisMonth, PeriodType.AllTime, PeriodType.Custom };

        private string? _statusMessage;
        public string? StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }

        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

        private bool _isStatusError;
        public bool IsStatusError
        {
            get => _isStatusError;
            set
            {
                _isStatusError = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusBackgroundBrush));
                OnPropertyChanged(nameof(StatusForegroundBrush));
            }
        }

        public Brush StatusBackgroundBrush => IsStatusError
            ? new SolidColorBrush(Color.FromRgb(0xFE, 0xE2, 0xE2))
            : new SolidColorBrush(Color.FromRgb(0xD1, 0xFA, 0xE5));

        public Brush StatusForegroundBrush => IsStatusError
            ? new SolidColorBrush(Color.FromRgb(0x99, 0x1B, 0x1B))
            : new SolidColorBrush(Color.FromRgb(0x06, 0x5F, 0x46));

        public void SetStatusMessage(string message, bool isError)
        {
            StatusMessage = message;
            IsStatusError = isError;
        }

        public MainViewModel() : this(new FinanceService())
        {
        }

        public MainViewModel(IFinanceService financeService)
        {
            _financeService = financeService ?? new FinanceService();

            InitializeNavigationCommands();
            InitializePeriodCommands();
            InitializeQuickAddCommands();
            InitializeTransactionCommands();

            ResetForm();
            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            Accounts.Clear();
            var accounts = await _financeService.GetAccountsAsync();
            foreach (var a in accounts) Accounts.Add(a);

            AccountFilterOptions.Clear();
            AccountFilterOptions.Add("Всі рахунки");
            foreach (var a in accounts) AccountFilterOptions.Add(a.Name);

            SelectedAccount = Accounts.FirstOrDefault();

            Categories.Clear();
            var categories = await _financeService.GetCategoriesAsync();
            foreach (var c in categories) Categories.Add(c);
            UpdateFilteredCategories();

            var (from, to) = GetPeriodDates();
            Transactions.Clear();
            var items = await _financeService.GetTransactionsAsync(from, to);
            foreach (var t in items) Transactions.Add(t);
            ApplyTransactionFilter();

            AccountBalances.Clear();
            var balances = await _financeService.GetAccountBalancesAsync();
            foreach (var b in balances) AccountBalances.Add(b);
            TotalBalance = AccountBalances.Sum(b => b.Balance);

            CategorySummaries.Clear();
            var catSummaries = await _financeService.GetCategorySummariesAsync(from, to);
            foreach (var c in catSummaries) CategorySummaries.Add(c);

            TotalIncome = items.Where(t => t.IsIncome).Sum(t => t.Amount);
            TotalExpenses = items.Where(t => !t.IsIncome).Sum(t => t.Amount);
            NetSavings = TotalIncome - TotalExpenses;
            SavingsRate = TotalIncome > 0 ? (double)(NetSavings / TotalIncome * 100) : 0;

            TopExpenseCategories.Clear();
            var expenses = catSummaries.Where(c => !c.IsIncome && c.TotalAmount < 0).ToList();
            var totalExpensesAbs = expenses.Sum(c => Math.Abs(c.TotalAmount));

            if (expenses.Any())
            {
                var biggest = expenses.OrderByDescending(c => Math.Abs(c.TotalAmount)).First();
                TopExpenseCategoryName = biggest.Name;
                TopExpenseCategoryAmount = Math.Abs(biggest.TotalAmount);
            }
            else
            {
                TopExpenseCategoryName = "Немає витрат";
                TopExpenseCategoryAmount = 0;
            }

            foreach (var cat in expenses.OrderByDescending(c => Math.Abs(c.TotalAmount)).Take(5))
            {
                TopExpenseCategories.Add(new TopExpenseCategory
                {
                    Name = cat.Name,
                    Amount = Math.Abs(cat.TotalAmount),
                    Percentage = totalExpensesAbs > 0
                        ? (double)(Math.Abs(cat.TotalAmount) / totalExpensesAbs * 100)
                        : 0
                });
            }

            ExpenseSeries = new SeriesCollection();
            foreach (var category in TopExpenseCategories)
            {
                ExpenseSeries.Add(new PieSeries
                {
                    Title = category.Name,
                    Values = new ChartValues<double> { (double)category.Amount },
                    DataLabels = true
                });
            }
            OnPropertyChanged(nameof(ExpenseSeries));

            RecentTransactions.Clear();
            foreach (var t in items.Take(5))
            {
                RecentTransactions.Add(t);
            }
            OnPropertyChanged(nameof(HasRecentTransactions));
            OnPropertyChanged(nameof(HasExpenses));

            MonthlyBudgets.Clear();
            var budgets = await _financeService.GetMonthlyBudgetsAsync(DateTime.Now.Year, DateTime.Now.Month);
            foreach (var b in budgets)
            {
                MonthlyBudgets.Add(b);
            }
            OnPropertyChanged(nameof(HasMonthlyBudgets));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
