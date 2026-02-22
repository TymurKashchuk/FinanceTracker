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

namespace FinanceTracker.wpf.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IFinanceService _financeService;
        public ObservableCollection<Transaction> Transactions { get; } = new();

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

        public ICommand LoadCommand { get; }
        public ICommand AddCommand { get; }

        public MainViewModel()
        {
            _financeService = new FinanceService();

            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            AddCommand = new RelayCommand(async _ => await AddAsync());
        }

        public async Task LoadAsync()
        {
            Transactions.Clear();
            var items = await _financeService.GetTransactionsAsync();
            foreach (var t in items)
                Transactions.Add(t);
        }

        public async Task AddAsync()
        {
            var transaction = new Transaction
            {
                Description = Description,
                Amount = Amount,
                Date = DateTime.Now,
                IsIncome = true
            };

            await _financeService.AddTransactionAsync(transaction);
            await LoadAsync();

            Description = string.Empty;
            Amount = 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
