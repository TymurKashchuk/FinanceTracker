using System.Windows.Input;
using FinanceTracker.wpf.Models;

namespace FinanceTracker.wpf.ViewModels
{
    public partial class MainViewModel
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

        public ICommand SetTabDashboardCommand { get; private set; } = null!;
        public ICommand SetTabTransactionsCommand { get; private set; } = null!;
        public ICommand SetTabAnalyticsCommand { get; private set; } = null!;

        private void InitializeNavigationCommands()
        {
            SetTabDashboardCommand = new RelayCommand(_ => CurrentTab = NavigationTab.Dashboard);
            SetTabTransactionsCommand = new RelayCommand(_ => CurrentTab = NavigationTab.Transactions);
            SetTabAnalyticsCommand = new RelayCommand(_ => CurrentTab = NavigationTab.Analytics);
        }
    }
}
