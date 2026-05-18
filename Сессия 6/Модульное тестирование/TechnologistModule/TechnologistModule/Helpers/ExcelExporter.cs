using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace TechnologistModule.Helpers
{
    public static class ExcelExporter
    {
        public static void ExportToExcel(string fileName, string sheetName, IEnumerable<object> data)
        {
            try
            {
                if (data == null || !data.Any())
                {
                    MessageBox.Show("Нет данных для экспорта");
                    return;
                }

                var properties = data.First().GetType().GetProperties();

                // Создаем HTML таблицу (Excel открывает HTML)
                var html = new System.Text.StringBuilder();
                html.AppendLine("<html><head><meta charset='UTF-8'></head><body>");
                html.AppendLine($"<h2>{sheetName}</h2>");
                html.AppendLine("<table border='1' cellpadding='5' cellspacing='0'>");

                // Заголовки
                html.AppendLine("<tr>");
                foreach (var prop in properties)
                {
                    html.AppendLine($"<th>{prop.Name}</th>");
                }
                html.AppendLine("</tr>");

                // Данные
                foreach (var item in data)
                {
                    html.AppendLine("<tr>");
                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(item)?.ToString() ?? "";
                        html.AppendLine($"<td>{System.Security.SecurityElement.Escape(value)}</td>");
                    }
                    html.AppendLine("</tr>");
                }

                html.AppendLine("</table>");
                html.AppendLine("</body></html>");

                // Сохраняем как .xls (старый формат Excel) — Excel открывает HTML как таблицу
                var path = fileName.Replace(".xlsx", ".xls");
                File.WriteAllText(path, html.ToString(), System.Text.Encoding.UTF8);

                MessageBox.Show($"Отчёт сохранён:\n{path}\n(откроется в Excel)", "Успех",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}