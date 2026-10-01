using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using FinanceTracker.wpf.Data;

namespace FinanceTracker.wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        var culture = CultureInfo.GetCultureInfo("uk-UA");
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(
                XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        using var db = new AppDbContext();
        db.Database.EnsureCreated();
    }
}

