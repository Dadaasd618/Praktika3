using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LaboratoryModule.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;
            switch (status)
            {
                case "pending": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                case "testing": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                case "approved": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                case "blocked": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                case "created": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                case "in_progress": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                case "completed": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                default: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}