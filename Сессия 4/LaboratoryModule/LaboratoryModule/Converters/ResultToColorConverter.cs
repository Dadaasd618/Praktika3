using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LaboratoryModule.Converters
{
    public class ResultToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool && (bool)value;
            return b ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}