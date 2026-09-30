using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FinanceTracker.wpf.Converters
{
    public class TransactionTypeToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush IncomeBrush = new(Color.FromRgb(16, 185, 129));
        private static readonly SolidColorBrush ExpenseBrush = new(Color.FromRgb(239, 68, 68));
        private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(107, 114, 128));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isIncome)
            {
                return isIncome ? IncomeBrush : ExpenseBrush;
            }
            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
