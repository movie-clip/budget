using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HomeCharts.Converters
{
    public class SignToBrushConverter : IValueConverter
    {
        public Brush PositiveBrush { get; set; }
        public Brush NegativeBrush { get; set; }
        public Brush ZeroBrush     { get; set; } = Brushes.Black;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int dV)
            {
                return dV >= 0 ? PositiveBrush : NegativeBrush;
            }
            
            if (!(value is string d))
            {
                return ZeroBrush;
            }
            
            return d.Contains("+") ? PositiveBrush : NegativeBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}