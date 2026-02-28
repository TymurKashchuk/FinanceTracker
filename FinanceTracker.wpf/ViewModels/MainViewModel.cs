using FinanceTracker.wpf.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinanceTracker.wpf.Models;
using FinanceTracker.wpf.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static FinanceTracker.wpf.Services.FinanceService;

namespace FinanceTracker.wpf.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IFinanceService _financeService;
        public ObservableCollection<Transaction> Transactions { get; } = new();

        private string _description = string.Empty;

        private bool _isIncome = true;
        public ObservableCollection<Account> Accounts { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();

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

        public ICommand LoadCommand { get; }
        public ICommand AddCommand { get; }

        public MainViewModel()
        {
            _financeService = new FinanceService();

            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            AddCommand = new RelayCommand(async _ => await AddAsync());
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

            if (FromDate.HasValue)
                items = items.Where(t => t.Date >= FromDate.Value).ToList();

            if (ToDate.HasValue)
                items = items.Where(t => t.Date <= ToDate.Value).ToList();

            foreach (var t in items)
                Transactions.Add(t);

            AccountBalances.Clear();
            var balances = await _financeService.GetAccountBalancesAsync();
            foreach (var b in balances)
                AccountBalances.Add(b);

            TotalBalance = AccountBalances.Sum(b => b.Balance);

            CategorySummaries.Clear();
            var catSummaries = await _financeService.GetCategorySummariesAsync(FromDate, ToDate);
            foreach (var c in catSummaries)
                CategorySummaries.Add(c);

            TotalIncome = catSummaries.Where(c => c.IsIncome).Sum(c => c.TotalAmount);
            TotalExpenses = catSummaries.Where(c => !c.IsIncome).Sum(c => Math.Abs(c.TotalAmount));
        }

        public async Task AddAsync()
        {
            if (string.IsNullOrWhiteSpace(Description)) return;

            if (Amount <= 0)
                return;

            if (SelectedAccount == null || SelectedCategory == null) return;

            var transaction = new Transaction
            {
                Description = Description,
                Amount = Amount,
                Date = DateTime.Now,
                IsIncome = IsIncome,
                AccountId = SelectedAccount.Id,
                CategoryId = SelectedCategory.Id
            };

            await _financeService.AddTransactionAsync(transaction);
            await LoadAsync();

            Description = string.Empty;
            Amount = 0;
        }

        private DateTime? _fromDate;
        private DateTime? _toDate;
        public DateTime? FromDate
        {
            get => _fromDate;
            set { _fromDate = value; OnPropertyChanged(); }
        }

        public DateTime? ToDate
        {
            get => _toDate;
            set { _toDate = value; OnPropertyChanged(); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public bool IsIncome
        {
            get => _isIncome;
            set { _isIncome = value; OnPropertyChanged(); }
        }

        public ObservableCollection<AccountBalanceDto> AccountBalances { get; } = new();

        private decimal _totalBalance;
        public decimal TotalBalance
        {
            get => _totalBalance;
            set { _totalBalance = value; OnPropertyChanged(); }
        }

        public ObservableCollection<CategorySummaryDto> CategorySummaries { get; } = new();
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
    }
}
