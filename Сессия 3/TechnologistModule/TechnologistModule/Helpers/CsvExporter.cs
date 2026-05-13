using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace TechnologistModule.Helpers
{
    public static class CsvExporter
    {
        public static void ExportToCsv(string fileName, IEnumerable<object> data)
        {
            try
            {
                var sb = new StringBuilder();

                if (data != null && data.Any())
                {
                    // Получаем свойства первого объекта для заголовков
                    var properties = data.First().GetType().GetProperties();

                    // Заголовки
                    sb.AppendLine(string.Join(";", properties.Select(p => p.Name)));

                    // Данные
                    foreach (var item in data)
                    {
                        var values = properties.Select(p => p.GetValue(item)?.ToString() ?? "");
                        sb.AppendLine(string.Join(";", values));
                    }
                }
                else
                {
                    sb.AppendLine("Нет данных для отображения");
                }

                File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);

                MessageBox.Show($"Отчёт успешно сохранён:\n{fileName}", "Успех",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в CSV: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}