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
        AllTime
    }

    public enum TransactionType
    {
        Income,
        Expense
    }

    public enum NavigationTab
    {
        Dashboard,
        Transactions,
        Analytics
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private NavigationTab _currentTab = NavigationTab.Dashboard;
        public NavigationTab CurrentTab
        {
            get => _currentTab;
            set
            {
                _currentTab = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDashboardTab));
                OnPropertyChanged(nameof(IsTransactionsTab));
                OnPropertyChanged(nameof(IsAnalyticsTab));
                OnPropertyChanged(nameof(CurrentPageTitle));
                OnPropertyChanged(nameof(CurrentPageSubtitle));
            }
        }

        public bool IsDashboardTab => CurrentTab == NavigationTab.Dashboard;
        public bool IsTransactionsTab => CurrentTab == NavigationTab.Transactions;
        public bool IsAnalyticsTab => CurrentTab == NavigationTab.Analytics;

        public string CurrentPageTitle => CurrentTab switch
        {
            NavigationTab.Dashboard => "Дашборд",
            NavigationTab.Transactions => "Транзакції",
            NavigationTab.Analytics => "Аналітика",
            _ => "Фінанси"
        };

        public string CurrentPageSubtitle => CurrentTab switch
        {
            NavigationTab.Dashboard => "Огляд балансу, ключові показники та активність",
            NavigationTab.Transactions => "Повна історія операцій з пошуком та фільтрами",
            NavigationTab.Analytics => "Структура витрат, підсумки категорій та звіти",
            _ => string.Empty
        };

        public string CurrentPeriodText => SelectedPeriod switch
        {
            PeriodType.Today => "Сьогодні",
            PeriodType.ThisWeek => "Тиждень",
            PeriodType.ThisMonth => "Місяць",
            PeriodType.AllTime => "Весь час",
            _ => "Весь час"
        };

        private readonly IFinanceService _financeService;

        public ObservableCollection<Transaction> Transactions { get; } = new();
        public ObservableCollection<Transaction> FilteredTransactions { get; } = new();
        public ObservableCollection<Transaction> RecentTransactions { get; } = new();
        public ObservableCollection<Account> Accounts { get; } = new();
        public ObservableCollection<string> AccountFilterOptions { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<Category> FilteredCategories { get; } = new();
        public ObservableCollection<TransactionType> TransactionTypes { get; } = new() { TransactionType.Expense, TransactionType.Income };
        public ObservableCollection<PeriodType> PeriodTypes { get; } = new() { PeriodType.Today, PeriodType.ThisWeek, PeriodType.ThisMonth, PeriodType.AllTime };

        public bool HasRecentTransactions => RecentTransactions.Count > 0;
        public bool HasExpenses => TopExpenseCategories.Count > 0;

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyTransactionFilter();
            }
        }

        private string _selectedAccountFilter = "Всі рахунки";
        public string SelectedAccountFilter
        {
            get => _selectedAccountFilter;
            set
            {
                _selectedAccountFilter = value;
                OnPropertyChanged();
                ApplyTransactionFilter();
            }
        }

        public int FilteredTransactionsCount => FilteredTransactions.Count;

        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set { _selectedDate = value; OnPropertyChanged(); }
        }

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

        private TransactionType _transactionType = TransactionType.Expense;
        public TransactionType TransactionType
        {
            get => _transactionType;
            set
            {
                _transactionType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsExpenseSelected));
                UpdateFilteredCategories();
            }
        }

        public bool IsExpenseSelected => TransactionType == TransactionType.Expense;

        public SeriesCollection ExpenseSeries { get; set; } = new();
        public ObservableCollection<AccountBalanceDto> AccountBalances { get; } = new();
        public ObservableCollection<CategorySummaryDto> CategorySummaries { get; } = new();
        public ObservableCollection<TopExpenseCategory> TopExpenseCategories { get; } = new();

        public ICommand AddCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand DeleteTransactionCommand { get; }
        public ICommand ClearFormCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand SetPeriodTodayCommand { get; }
        public ICommand SetPeriodWeekCommand { get; }
        public ICommand SetPeriodMonthCommand { get; }
        public ICommand SetPeriodAllTimeCommand { get; }
        public ICommand SetPeriodLast30DaysCommand => SetPeriodAllTimeCommand;
        public ICommand SetTabDashboardCommand { get; }
        public ICommand SetTabTransactionsCommand { get; }
        public ICommand SetTabAnalyticsCommand { get; }

        public MainViewModel()
        {
            _financeService = new FinanceService();

            AddCommand = new RelayCommand(async _ => await AddAsync());
            ExportCsvCommand = new RelayCommand(async _ => await ExportCsvAsync());
            DeleteTransactionCommand = new RelayCommand(async obj => await DeleteTransactionAsync(obj));
            ClearFormCommand = new RelayCommand(_ => ResetForm());
            ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);

            SetPeriodTodayCommand = new RelayCommand(_ => SelectedPeriod = PeriodType.Today);
            SetPeriodWeekCommand = new RelayCommand(_ => SelectedPeriod = PeriodType.ThisWeek);
            SetPeriodMonthCommand = new RelayCommand(_ => SelectedPeriod = PeriodType.ThisMonth);
            SetPeriodAllTimeCommand = new RelayCommand(_ => SelectedPeriod = PeriodType.AllTime);

            SetTabDashboardCommand = new RelayCommand(_ => CurrentTab = NavigationTab.Dashboard);
            SetTabTransactionsCommand = new RelayCommand(_ => CurrentTab = NavigationTab.Transactions);
            SetTabAnalyticsCommand = new RelayCommand(_ => CurrentTab = NavigationTab.Analytics);

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

            AccountFilterOptions.Clear();
            AccountFilterOptions.Add("Всі рахунки");
            foreach (var a in Accounts) AccountFilterOptions.Add(a.Name);
            if (!AccountFilterOptions.Contains(SelectedAccountFilter))
            {
                SelectedAccountFilter = "Всі рахунки";
            }

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

            RecentTransactions.Clear();
            foreach (var t in items.Take(5))
            {
                RecentTransactions.Add(t);
            }
            OnPropertyChanged(nameof(HasRecentTransactions));
            OnPropertyChanged(nameof(HasExpenses));
        }

        private void UpdateFilteredCategories()
        {
            var previousSelectedId = SelectedCategory?.Id;
            FilteredCategories.Clear();
            bool isIncome = TransactionType == TransactionType.Income;
            var matched = Categories.Where(c => c.IsIncome == isIncome).ToList();
            foreach (var c in matched)
            {
                FilteredCategories.Add(c);
            }
            SelectedCategory = FilteredCategories.FirstOrDefault(c => c.Id == previousSelectedId) 
                               ?? FilteredCategories.FirstOrDefault();
        }

        public void ApplyTransactionFilter()
        {
            FilteredTransactions.Clear();
            var query = Transactions.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim().ToLower();
                query = query.Where(t => (t.Description != null && t.Description.ToLower().Contains(term))
                                      || (t.Category != null && t.Category.Name.ToLower().Contains(term)));
            }

            if (!string.IsNullOrEmpty(SelectedAccountFilter) && SelectedAccountFilter != "Всі рахунки")
            {
                query = query.Where(t => t.Account != null && t.Account.Name == SelectedAccountFilter);
            }

            foreach (var t in query)
            {
                FilteredTransactions.Add(t);
            }

            OnPropertyChanged(nameof(FilteredTransactionsCount));
        }

        public async Task AddAsync()
        {
            if (string.IsNullOrWhiteSpace(Description) || Amount <= 0 || SelectedAccount == null)
            {
                MessageBox.Show("Будь ласка, введіть опис, коректну суму (більше 0) та оберіть рахунок.",
                    "Увага", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var transaction = new Transaction
                {
                    Description = Description.Trim(),
                    Amount = Amount,
                    Date = SelectedDate.Date + DateTime.Now.TimeOfDay,
                    IsIncome = TransactionType == TransactionType.Income,
                    AccountId = SelectedAccount.Id,
                    CategoryId = SelectedCategory?.Id
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

        public async Task DeleteTransactionAsync(object? obj)
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
            SelectedDate = DateTime.Today;
            TransactionType = TransactionType.Expense;
            SelectedAccount = Accounts.FirstOrDefault();
            UpdateFilteredCategories();
        }

        private (DateTime? from, DateTime? to) GetPeriodDates()
        {
            return SelectedPeriod switch
            {
                PeriodType.Today => (DateTime.Now.Date, DateTime.Now.Date.AddDays(1).AddTicks(-1)),
                PeriodType.ThisWeek => (StartOfWeek(DateTime.Now), EndOfWeek(DateTime.Now)),
                PeriodType.ThisMonth => (StartOfMonth(DateTime.Now), EndOfMonth(DateTime.Now)),
                PeriodType.AllTime => (null, null),
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

        private decimal _netSavings;
        public decimal NetSavings
        {
            get => _netSavings;
            set { _netSavings = value; OnPropertyChanged(); }
        }

        private double _savingsRate;
        public double SavingsRate
        {
            get => _savingsRate;
            set { _savingsRate = value; OnPropertyChanged(); }
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
