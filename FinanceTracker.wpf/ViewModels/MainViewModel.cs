using FinanceTracker.wpf.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinanceTracker.wpf.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LiveCharts;
using LiveCharts.Wpf;
using static FinanceTracker.wpf.Services.FinanceService;
using System.Windows;
using Microsoft.Win32;

namespace FinanceTracker.wpf.ViewModels
{
    public enum PeriodType
    {
        Today,
        ThisWeek,
        ThisMonth,
        Last30Days
    }

    public enum TransactionType
    {
        Income,
        Expense
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        public string CurrentPeriodText => SelectedPeriod switch
        {
            PeriodType.Today => "Today",
            PeriodType.ThisWeek => "This week",
            PeriodType.ThisMonth => "This month",
            PeriodType.Last30Days => "30 days",
            _ => "Custom"
        };

        private readonly IFinanceService _financeService;
        public ObservableCollection<Transaction> Transactions { get; } = new();

        private string _description = string.Empty;
        private decimal _amount;
        private TransactionType _transactionType = TransactionType.Income;

        public ObservableCollection<Account> Accounts { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<TransactionType> TransactionTypes { get; } = new()
        {
            TransactionType.Income,
            TransactionType.Expense
        };

        public ObservableCollection<PeriodType> PeriodTypes { get; } = new()
        {
            PeriodType.Today,
            PeriodType.ThisWeek,
            PeriodType.ThisMonth,
            PeriodType.Last30Days
        };

        private PeriodType _selectedPeriod = PeriodType.ThisMonth;
        public PeriodType SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                _selectedPeriod = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentPeriodText));
                _ = LoadAsync();
            }
        }

        private Account? _selectedAccount;
        public Account? SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                if (_selectedAccount == value) return;
                _selectedAccount = value;
                OnPropertyChanged();
            }
        }

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory == value) return;
                _selectedCategory = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description == value) return;
                _description = value;
                OnPropertyChanged();
            }
        }

        public decimal Amount
        {
            get => _amount;
            set
            {
                if (_amount == value) return;
                _amount = value;
                OnPropertyChanged();
            }
        }

        public TransactionType TransactionType
        {
            get => _transactionType;
            set
            {
                if (_transactionType == value) return;
                _transactionType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsExpenseSelected));

                if (value == TransactionType.Income)
                {
                    SelectedCategory = null;
                }
            }
        }

        public bool IsExpenseSelected => TransactionType == TransactionType.Expense;

        public SeriesCollection ExpenseSeries { get; set; } = new();

        public ICommand SetPeriodTodayCommand { get; private set; }
        public ICommand SetPeriodWeekCommand { get; private set; }
        public ICommand SetPeriodMonthCommand { get; private set; }
        public ICommand LoadCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand ExportCsvCommand { get; }

        public MainViewModel()
        {
            _financeService = new FinanceService();

            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            AddCommand = new RelayCommand(async _ => await AddAsync());

            SetPeriodTodayCommand = new RelayCommand(obj => SelectedPeriod = PeriodType.Today);
            SetPeriodWeekCommand = new RelayCommand(obj => SelectedPeriod = PeriodType.ThisWeek);
            SetPeriodMonthCommand = new RelayCommand(obj => SelectedPeriod = PeriodType.ThisMonth);

            _ = InitializeAsync();

            ExportCsvCommand = new RelayCommand(async _ => await ExportCsvAsync());
        }

        private async Task InitializeAsync()
        {
            await _financeService.SeedAsync();
            await LoadAsync();
        }

        public async Task LoadAsync()
        {
            Accounts.Clear();
            var accounts = await _financeService.GetAccountsAsync();
            foreach (var a in accounts)
                Accounts.Add(a);

            if (SelectedAccount == null && Accounts.Any())
                SelectedAccount = Accounts.First();

            Categories.Clear();
            var categories = await _financeService.GetCategoriesAsync();
            foreach (var c in categories)
                Categories.Add(c);

            if (SelectedCategory == null && Categories.Any())
                SelectedCategory = Categories.First();

            (DateTime? from, DateTime? to) period = SelectedPeriod switch
            {
                PeriodType.Today => (DateTime.Now.Date, DateTime.Now.Date.AddDays(1).AddTicks(-1)),

                PeriodType.ThisWeek => (StartOfWeek(DateTime.Now), EndOfWeek(DateTime.Now)),

                PeriodType.ThisMonth => (StartOfMonth(DateTime.Now), EndOfMonth(DateTime.Now)),

                PeriodType.Last30Days => (DateTime.Now.AddDays(-30), DateTime.Now),

                _ => (null, null)
            };

            Transactions.Clear();
            var items = await _financeService.GetTransactionsAsync(period.from, period.to);
            foreach (var t in items)
                Transactions.Add(t);

            OnPropertyChanged(nameof(TransactionCount));

            AccountBalances.Clear();
            var balances = await _financeService.GetAccountBalancesAsync();
            foreach (var b in balances)
                AccountBalances.Add(b);

            TotalBalance = AccountBalances.Sum(b => b.Balance);

            CategorySummaries.Clear();
            var catSummaries = await _financeService.GetCategorySummariesAsync(period.from, period.to);
            foreach (var c in catSummaries)
            {
                System.Diagnostics.Debug.WriteLine($"Category: {c.Name}, IsIncome: {c.IsIncome}, TotalAmount: {c.TotalAmount}");
                CategorySummaries.Add(c);
            }

            TopExpenseCategories.Clear();

            TotalIncome = items.Where(t => t.IsIncome).Sum(t => t.Amount);
            TotalExpenses = items.Where(t => !t.IsIncome).Sum(t => t.Amount);

            TopExpenseCategories.Clear();
            var expenses = catSummaries.Where(c => !c.IsIncome && c.TotalAmount < 0).ToList();
            var totalExpenses = expenses.Sum(c => Math.Abs(c.TotalAmount));

            foreach (var cat in expenses.OrderByDescending(c => Math.Abs(c.TotalAmount)).Take(5))
            {
                TopExpenseCategories.Add(new TopExpenseCategory
                {
                    Name = cat.Name,
                    Amount = Math.Abs(cat.TotalAmount),
                    Percentage = totalExpenses > 0 ? (double)(Math.Abs(cat.TotalAmount) / totalExpenses * 100) : 0
                });
            }

            ExpenseSeries = new SeriesCollection();
            foreach (var category in TopExpenseCategories) {
                ExpenseSeries.Add(new PieSeries
                {
                    Title = category.Name,
                    Values = new ChartValues<double> {
                    (double)category.Amount
                    },
                    DataLabels = true
                });
            }

            OnPropertyChanged(nameof(TopExpenseCategoryName));
            OnPropertyChanged(nameof(TopExpenseCategoryAmount));
            OnPropertyChanged(nameof(ExpenseSeries));
        }

        public async Task AddAsync()
        {
            if (string.IsNullOrWhiteSpace(Description)) return;
            if (Amount <= 0) return;
            if (SelectedAccount == null) return;

            if (TransactionType == TransactionType.Expense && SelectedCategory == null)
                return;

            try
            {
                Debug.WriteLine($"AddAsync: IsIncome={TransactionType == TransactionType.Income}, AccountId={SelectedAccount.Id}, CategoryId={(TransactionType == TransactionType.Income ? "null" : SelectedCategory?.Id)}");

                var transaction = new Transaction
                {
                    Description = Description,
                    Amount = Amount,
                    Date = DateTime.Now,
                    IsIncome = TransactionType == TransactionType.Income,
                    AccountId = SelectedAccount.Id,
                    CategoryId = TransactionType == TransactionType.Income ? null : SelectedCategory?.Id
                };

                await _financeService.AddTransactionAsync(transaction);
                await LoadAsync();

                Description = string.Empty;
                Amount = 0;

                TransactionType = TransactionType.Income;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in AddAsync: {ex.Message}");
                throw;
            }
        }

        private async Task ExportCsvAsync()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"transactions_{DateTime.Now:yyyy-MM-dd}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                var (from, to) = GetPeriodDates();
                await _financeService.ExportTransactionsToCsvAsync(dialog.FileName, from, to);
                MessageBox.Show("Transactions exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private (DateTime? from, DateTime? to) GetPeriodDates()
        {
            return SelectedPeriod switch
            {
                PeriodType.Today => (DateTime.Now.Date, DateTime.Now.Date.AddDays(1).AddTicks(-1)),
                PeriodType.ThisWeek => (StartOfWeek(DateTime.Now), EndOfWeek(DateTime.Now)),
                PeriodType.ThisMonth => (StartOfMonth(DateTime.Now), EndOfMonth(DateTime.Now)),
                PeriodType.Last30Days => (DateTime.Now.AddDays(-30), DateTime.Now),
                _ => (null, null)
            };
        }

        public ObservableCollection<AccountBalanceDto> AccountBalances { get; } = new();
        private decimal _totalBalance;
        public decimal TotalBalance
        {
            get => _totalBalance;
            set
            {
                if (_totalBalance == value) return;
                _totalBalance = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<CategorySummaryDto> CategorySummaries { get; } = new();
        private decimal _totalIncome;
        public decimal TotalIncome
        {
            get => _totalIncome;
            set
            {
                if (_totalIncome == value) return;
                _totalIncome = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TopExpenseCategory> TopExpenseCategories { get; } = new();
        public string TopExpenseCategoryName => TopExpenseCategories.Any() ? TopExpenseCategories.First().Name : "No data";

        public decimal TopExpenseCategoryAmount => TopExpenseCategories.Any() ? TopExpenseCategories.First().Amount : 0;
        public class TopExpenseCategory
        {
            public string Name { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public double Percentage { get; set; }
        }

        private decimal _totalExpenses;
        public decimal TotalExpenses
        {
            get => _totalExpenses;
            set
            {
                if (_totalExpenses == value) return;
                _totalExpenses = value;
                OnPropertyChanged();
            }
        }

        public int TransactionCount => Transactions.Count;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public static DateTime StartOfWeek(DateTime dt, DayOfWeek firstDayOfWeek = DayOfWeek.Monday) {
            var diff = (int)(dt.DayOfWeek - firstDayOfWeek);
            if (diff < 0) diff += 7;
            return dt.AddDays(-diff).Date;//поверення початку тмжня
        }
        public static DateTime EndOfWeek(DateTime dt, DayOfWeek firstDayOfWeek = DayOfWeek.Monday) {
            var start = StartOfWeek(dt, firstDayOfWeek);
            return start.AddDays(6).Date.AddDays(1).AddTicks(-1);
        }

        public static DateTime StartOfMonth(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1);
        }

        public static DateTime EndOfMonth(DateTime dt)
        {
            return StartOfMonth(dt).AddMonths(1).AddTicks(-1);
        }
    }
}
