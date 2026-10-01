using System.Collections.ObjectModel;
using FinanceTracker.wpf.Models;
using FinanceTracker.wpf.Services;
using static FinanceTracker.wpf.Services.FinanceService;
using LiveCharts;

namespace FinanceTracker.wpf.ViewModels
{
    public partial class MainViewModel
    {
        public ObservableCollection<Transaction> RecentTransactions { get; } = new();
        public ObservableCollection<TopExpenseCategory> TopExpenseCategories { get; } = new();
        public ObservableCollection<AccountBalanceDto> AccountBalances { get; } = new();
        public ObservableCollection<CategorySummaryDto> CategorySummaries { get; } = new();
        public ObservableCollection<CategoryBudgetDto> MonthlyBudgets { get; } = new();

        public bool HasRecentTransactions => RecentTransactions.Count > 0;
        public bool HasExpenses => TopExpenseCategories.Count > 0;
        public bool HasMonthlyBudgets => MonthlyBudgets.Count > 0;

        public SeriesCollection ExpenseSeries { get; set; } = new();

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
    }
}
