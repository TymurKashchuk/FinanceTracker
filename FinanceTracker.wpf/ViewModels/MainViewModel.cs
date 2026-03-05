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
using static FinanceTracker.wpf.Services.FinanceService;

namespace FinanceTracker.wpf.ViewModels
{
    public enum PeriodType
    {
        Today,
        ThisWeek,
        ThisMonth,
        Last30Days,
        Custom
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
            PeriodType.Last30Days,
            PeriodType.Custom
        };

        private PeriodType _selectedPeriod = PeriodType.ThisMonth;
        public PeriodType SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                _selectedPeriod = value;
                OnPropertyChanged();
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

        public ICommand SetPeriodTodayCommand { get; private set; }
        public ICommand SetPeriodWeekCommand { get; private set; }
        public ICommand SetPeriodMonthCommand { get; private set; }
        public ICommand LoadCommand { get; }
        public ICommand AddCommand { get; }

        public MainViewModel()
        {
            _financeService = new FinanceService();

            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            AddCommand = new RelayCommand(async _ => await AddAsync());

            SetPeriodTodayCommand = new RelayCommand(obj => SelectedPeriod = PeriodType.Today);
            SetPeriodWeekCommand = new RelayCommand(obj => SelectedPeriod = PeriodType.ThisWeek);
            SetPeriodMonthCommand = new RelayCommand(obj => SelectedPeriod = PeriodType.ThisMonth);

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

            Transactions.Clear();
            var items = await _financeService.GetTransactionsAsync();
            foreach (var t in items)
                Transactions.Add(t);

            AccountBalances.Clear();
            var balances = await _financeService.GetAccountBalancesAsync();
            foreach (var b in balances)
                AccountBalances.Add(b);

            TotalBalance = AccountBalances.Sum(b => b.Balance);

            CategorySummaries.Clear();
            var catSummaries = await _financeService.GetCategorySummariesAsync();
            foreach (var c in catSummaries)
                CategorySummaries.Add(c);

            TotalIncome = catSummaries.Where(c => c.IsIncome).Sum(c => c.TotalAmount);
            TotalExpenses = catSummaries.Where(c => !c.IsIncome).Sum(c => Math.Abs(c.TotalAmount));
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
