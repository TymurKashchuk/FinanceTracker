using System.Windows;
using System.Windows.Controls;
using FinanceTracker.wpf.Models;
using FinanceTracker.wpf.ViewModels;

namespace FinanceTracker.wpf.Views
{
    public partial class TransactionsView : UserControl
    {
        public TransactionsView()
        {
            InitializeComponent();
        }

        private void DeleteTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && TransactionsDataGrid.SelectedItem is Transaction t)
            {
                vm.DeleteTransactionCommand.Execute(t);
            }
        }
    }
}
