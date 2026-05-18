using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TechnologistModule.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string;
            return status switch
            {
                "active" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                "draft" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                "archived" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                "completed" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                "running" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                "blocked" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                "planned" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                "quality_control" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}