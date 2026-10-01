namespace FinanceTracker.wpf.Models
{
    public enum PeriodType
    {
        Today,
        ThisWeek,
        ThisMonth,
        AllTime,
        Custom
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
}
