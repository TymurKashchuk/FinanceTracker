using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FinanceTracker.wpf.Converters
{
    public class BalanceToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal dec)
            {
                if (dec < 0)
                    return Brushes.Red;
                if (dec > 0)
                    return Brushes.Green;
                return Brushes.Gray;
            }

            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
