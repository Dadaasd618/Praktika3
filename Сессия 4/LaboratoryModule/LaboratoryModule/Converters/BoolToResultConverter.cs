using System;
using System.Globalization;
using System.Windows.Data;

namespace LaboratoryModule.Converters
{
    public class BoolToResultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool && (bool)value;
            return b ? "✅ Соответствует" : "❌ Не соответствует";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}