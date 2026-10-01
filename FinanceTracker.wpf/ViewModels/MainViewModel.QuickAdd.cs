using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using FinanceTracker.wpf.Models;
using FinanceTracker.wpf.Services;

namespace FinanceTracker.wpf.ViewModels
{
    public partial class MainViewModel
    {
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

        private DateTime? _selectedDate = DateTime.Today;
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set { _selectedDate = value ?? DateTime.Today; OnPropertyChanged(); }
        }

        private TransactionType _transactionType = TransactionType.Expense;
        public TransactionType TransactionType
        {
            get => _transactionType;
            set
            {
                _transactionType = value;
                OnPropertyChanged();
                UpdateFilteredCategories();
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

        private bool _isQuickAddExpanded = true;
        public bool IsQuickAddExpanded
        {
            get => _isQuickAddExpanded;
            set
            {
                _isQuickAddExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(QuickAddToggleText));
            }
        }

        public string QuickAddToggleText => IsQuickAddExpanded ? "Згорнути" : "+ Нова операція";

        public ICommand AddCommand { get; private set; } = null!;
        public ICommand ClearFormCommand { get; private set; } = null!;
        public ICommand ToggleQuickAddCommand { get; private set; } = null!;
        public ICommand SetDateTodayCommand { get; private set; } = null!;
        public ICommand SetDateYesterdayCommand { get; private set; } = null!;

        private void InitializeQuickAddCommands()
        {
            AddCommand = new RelayCommand(async _ => await AddAsync());
            ClearFormCommand = new RelayCommand(_ => ResetForm());
            ToggleQuickAddCommand = new RelayCommand(_ => IsQuickAddExpanded = !IsQuickAddExpanded);
            SetDateTodayCommand = new RelayCommand(_ => SelectedDate = DateTime.Today);
            SetDateYesterdayCommand = new RelayCommand(_ => SelectedDate = DateTime.Today.AddDays(-1));
        }

        public async Task AddAsync()
        {
            var validation = TransactionValidator.Validate(Description, Amount, SelectedAccount, SelectedCategory, SelectedDate);
            if (!validation.IsValid)
            {
                SetStatusMessage(validation.ErrorMessage ?? "Помилка валідації", isError: true);
                return;
            }

            try
            {
                var opDate = SelectedDate ?? DateTime.Today;
                var transaction = new Transaction
                {
                    Description = Description.Trim(),
                    Amount = Amount,
                    Date = opDate.Date + DateTime.Now.TimeOfDay,
                    IsIncome = TransactionType == TransactionType.Income,
                    AccountId = SelectedAccount!.Id,
                    CategoryId = SelectedCategory!.Id
                };

                await _financeService.AddTransactionAsync(transaction);
                await LoadAsync();

                var sign = transaction.IsIncome ? "+" : "-";
                SetStatusMessage($"✓ Операцію «{transaction.Description}» ({sign}{transaction.Amount:N2} ₴) успішно додано", isError: false);
                ResetForm();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                SetStatusMessage($"Помилка: {ex.Message}", isError: true);
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
    }
}
