using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FinanceTracker.wpf.Models;
using Microsoft.Win32;

namespace FinanceTracker.wpf.ViewModels
{
    public partial class MainViewModel
    {
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

        private string _selectedTypeFilter = "Всі типи";
        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set
            {
                _selectedTypeFilter = value;
                OnPropertyChanged();
                ApplyTransactionFilter();
            }
        }

        public ObservableCollection<string> AccountFilterOptions { get; } = new() { "Всі рахунки" };
        public ObservableCollection<string> TypeFilterOptions { get; } = new() { "Всі типи", "Тільки витрати", "Тільки доходи" };

        public int FilteredTransactionsCount => FilteredTransactions.Count;

        private Transaction? _selectedTransaction;
        public Transaction? SelectedTransaction
        {
            get => _selectedTransaction;
            set { _selectedTransaction = value; OnPropertyChanged(); }
        }

        public ICommand ClearSearchCommand { get; private set; } = null!;
        public ICommand DeleteTransactionCommand { get; private set; } = null!;
        public ICommand ExportCsvCommand { get; private set; } = null!;

        private void InitializeTransactionCommands()
        {
            ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);
            DeleteTransactionCommand = new RelayCommand(async obj => await DeleteTransactionAsync(obj));
            ExportCsvCommand = new RelayCommand(async _ => await ExportCsvAsync());
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

            if (!string.IsNullOrEmpty(SelectedTypeFilter) && SelectedTypeFilter != "Всі типи")
            {
                bool filterIncome = SelectedTypeFilter == "Тільки доходи";
                query = query.Where(t => t.IsIncome == filterIncome);
            }

            foreach (var t in query)
            {
                FilteredTransactions.Add(t);
            }

            OnPropertyChanged(nameof(FilteredTransactionsCount));
        }

        public async Task DeleteTransactionAsync(object? obj)
        {
            var transaction = obj as Transaction ?? SelectedTransaction;
            if (transaction == null) return;

            var sign = transaction.IsIncome ? "+" : "-";
            var result = MessageBox.Show(
                $"Ви впевнені, що хочете видалити операцію?\n\n" +
                $"• Опис: {transaction.Description}\n" +
                $"• Сума: {sign}{transaction.Amount:N2} ₴\n" +
                $"• Дата: {transaction.Date:dd.MM.yyyy HH:mm}\n" +
                $"• Рахунок: {transaction.Account?.Name ?? "—"}",
                "Підтвердження видалення",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await _financeService.DeleteTransactionAsync(transaction.Id);
                await LoadAsync();
                SetStatusMessage($"Операцію «{transaction.Description}» успішно видалено", isError: false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                SetStatusMessage($"Помилка при видаленні: {ex.Message}", isError: true);
            }
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
    }
}
