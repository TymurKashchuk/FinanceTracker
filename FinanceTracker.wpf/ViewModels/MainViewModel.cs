using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using FinanceTracker.wpf.Models;
using FinanceTracker.wpf.Services;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using static FinanceTracker.wpf.Services.FinanceService;

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
        public ObservableCollection<Account> Accounts { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<TransactionType> TransactionTypes { get; } = new() { TransactionType.Income, TransactionType.Expense };
        public ObservableCollection<PeriodType> PeriodTypes { get; } = new() { PeriodType.Today, PeriodType.ThisWeek, PeriodType.ThisMonth, PeriodType.Last30Days };

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
            set { _selectedAccount = value; OnPropertyChanged(); }
        }

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        private Transaction? _selectedTransaction;
        public Transaction? SelectedTransaction
        {
            get => _selectedTransaction;
            set { _selectedTransaction = value; OnPropertyChanged(); }
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set { _amount = value; OnPropertyChanged(); }
        }

        private TransactionType _transactionType = TransactionType.Income;
        public TransactionType TransactionType
        {
            get => _transactionType;
            set
            {
                _transactionType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsExpenseSelected));
                if (value == TransactionType.Income) SelectedCategory = null;
            }
        }

        public bool IsExpenseSelected => TransactionType == TransactionType.Expense;

        public SeriesCollection ExpenseSeries { get; set; } = new();
        public ObservableCollection<AccountBalanceDto> AccountBalances { get; } = new();
        public ObservableCollection<CategorySummaryDto> CategorySummaries { get; } = new();
        public ObservableCollection<TopExpenseCategory> TopExpenseCategories { get; } = new();

        public ICommand LoadCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand DeleteTransactionCommand { get; }
        public ICommand SetPeriodTodayCommand { get; }
        public ICommand SetPeriodWeekCommand { get; }
        public ICommand SetPeriodMonthCommand { get; }

        public MainViewModel()
        {
            _financeService = new FinanceService();

            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            AddCommand = new RelayCommand(async _ => await AddAsync());
            ExportCsvCommand = new RelayCommand(async _ => await ExportCsvAsync());
            DeleteTransactionCommand = new RelayCommand(async obj => await DeleteTransactionAsync(obj));

            SetPeriodTodayCommand = new RelayCommand(_ => SelectedPeriod = PeriodType.Today);
            SetPeriodWeekCommand = new RelayCommand(_ => SelectedPeriod = PeriodType.ThisWeek);
            SetPeriodMonthCommand = new RelayCommand(_ => SelectedPeriod = PeriodType.ThisMonth);

            _ = InitializeAsync();
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
            foreach (var a in accounts) Accounts.Add(a);
            SelectedAccount ??= Accounts.FirstOrDefault();

            Categories.Clear();
            var categories = await _financeService.GetCategoriesAsync();
            foreach (var c in categories) Categories.Add(c);
            SelectedCategory ??= Categories.FirstOrDefault();

            var (from, to) = GetPeriodDates();
            Transactions.Clear();
            var items = await _financeService.GetTransactionsAsync(from, to);
            foreach (var t in items) Transactions.Add(t);

            AccountBalances.Clear();
            var balances = await _financeService.GetAccountBalancesAsync();
            foreach (var b in balances) AccountBalances.Add(b);
            TotalBalance = AccountBalances.Sum(b => b.Balance);

            CategorySummaries.Clear();
            var catSummaries = await _financeService.GetCategorySummariesAsync(from, to);
            foreach (var c in catSummaries) CategorySummaries.Add(c);

            TotalIncome = items.Where(t => t.IsIncome).Sum(t => t.Amount);
            TotalExpenses = items.Where(t => !t.IsIncome).Sum(t => t.Amount);

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
                TopExpenseCategoryName = "No expenses";
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
        }

        public async Task AddAsync()
        {
            if (string.IsNullOrWhiteSpace(Description) || Amount <= 0 || SelectedAccount == null) return;
            if (TransactionType == TransactionType.Expense && SelectedCategory == null) return;

            try
            {
                var transaction = new Transaction
                {
                    Description = Description,
                    Amount = Amount,
                    Date = DateTime.Now,
                    IsIncome = TransactionType == TransactionType.Income,
                    AccountId = SelectedAccount.Id,
                    CategoryId = TransactionType == TransactionType.Expense ? SelectedCategory?.Id : null
                };
                await _financeService.AddTransactionAsync(transaction);

                await LoadAsync();
                ResetForm();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                MessageBox.Show($"Помилка: {ex.Message}");
            }
        }

        public async Task DeleteTransactionAsync(object obj)
        {
            if (obj is not Transaction transaction) return;

            var result = MessageBox.Show("Видалити транзакцію?", "Підтвердження",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            await _financeService.DeleteTransactionAsync(transaction.Id);
            await LoadAsync();
        }

        private async Task ExportCsvAsync()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"transactions_{DateTime.Now:yyyy-MM-dd}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                var (from, to) = GetPeriodDates();
                await _financeService.ExportTransactionsToCsvAsync(dialog.FileName, from, to);
                MessageBox.Show("Експорт завершено!");
            }
        }

        private void ResetForm()
        {
            Description = string.Empty;
            Amount = 0;
            TransactionType = TransactionType.Income;
            SelectedAccount = Accounts.FirstOrDefault();
            SelectedCategory = null;
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

        private decimal _totalBalance;
        public decimal TotalBalance
        {
            get => _totalBalance;
            set { _totalBalance = value; OnPropertyChanged(); }
        }

        private decimal _totalIncome;
        public decimal TotalIncome
        {
            get => _totalIncome;
            set { _totalIncome = value; OnPropertyChanged(); }
        }

        private decimal _totalExpenses;
        public decimal TotalExpenses
        {
            get => _totalExpenses;
            set { _totalExpenses = value; OnPropertyChanged(); }
        }

        private string _topExpenseCategoryName = "No expenses";
        public string TopExpenseCategoryName
        {
            get => _topExpenseCategoryName;
            set { _topExpenseCategoryName = value; OnPropertyChanged(); }
        }

        private decimal _topExpenseCategoryAmount;
        public decimal TopExpenseCategoryAmount
        {
            get => _topExpenseCategoryAmount;
            set { _topExpenseCategoryAmount = value; OnPropertyChanged(); }
        }

        public int TransactionCount => Transactions.Count;

        public class TopExpenseCategory
        {
            public string Name { get; set; } = "";
            public decimal Amount { get; set; }
            public double Percentage { get; set; }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public static DateTime StartOfWeek(DateTime dt, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
        {
            var diff = (int)(dt.DayOfWeek - firstDayOfWeek);
            if (diff < 0) diff += 7;
            return dt.AddDays(-diff).Date;
        }

        public static DateTime EndOfWeek(DateTime dt, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
            => StartOfWeek(dt, firstDayOfWeek).AddDays(6).Date.AddDays(1).AddTicks(-1);

        public static DateTime StartOfMonth(DateTime dt) => new DateTime(dt.Year, dt.Month, 1);
        public static DateTime EndOfMonth(DateTime dt) => StartOfMonth(dt).AddMonths(1).AddTicks(-1);
    }
}
