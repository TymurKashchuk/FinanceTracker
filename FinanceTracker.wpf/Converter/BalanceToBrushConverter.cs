using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FinanceTracker.wpf.Converters
{
    public class BalanceToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush PositiveBrush = new(Color.FromRgb(16, 185, 129));
        private static readonly SolidColorBrush NegativeBrush = new(Color.FromRgb(239, 68, 68));
        private static readonly SolidColorBrush NeutralBrush = new(Color.FromRgb(107, 114, 128));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal dec)
            {
                if (dec < 0) return NegativeBrush;
                if (dec > 0) return PositiveBrush;
                return NeutralBrush;
            }

            if (value is double dbl)
            {
                if (dbl < 0) return NegativeBrush;
                if (dbl > 0) return PositiveBrush;
                return NeutralBrush;
            }

            return NeutralBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
