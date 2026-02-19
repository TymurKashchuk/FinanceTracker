using FinanceTracker.wpf.Data;
using System.Configuration;
using System.Data;
using System.Windows;

namespace FinanceTracker.wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App() {
        using var db = new AppDbContext();
        db.Database.EnsureCreated();
    }
}

