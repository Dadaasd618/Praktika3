using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TechnologistModule.Helpers;
using TechnologistModule.Models;
using TechnologistModule.Services;

namespace TechnologistModule.Controls
{
    public partial class ReportsControl : UserControl
    {

        private readonly ApiClient _apiClient;
        private ObservableCollection<ProductionBatch> _batchReportData = new ObservableCollection<ProductionBatch>();
        private ObservableCollection<Deviation> _deviationReportData = new ObservableCollection<Deviation>();
        private ObservableCollection<Recipe> _recipeReportData = new ObservableCollection<Recipe>();
        private ObservableCollection<object> _extruderReportData = new ObservableCollection<object>();
        private ObservableCollection<object> _blockReportData = new ObservableCollection<object>();

        public ReportsControl(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;
            BatchDateFrom.SelectedDate = DateTime.Now.AddMonths(-1);
            BatchDateTo.SelectedDate = DateTime.Now;
            DeviationDateFrom.SelectedDate = DateTime.Now.AddMonths(-1);
            DeviationDateTo.SelectedDate = DateTime.Now;
        }

        // Отчет по партиям (уже есть)
        private async void GenerateBatchReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var batches = await _apiClient.GetBatchesAsync();
                _batchReportData.Clear();
                var dateFrom = BatchDateFrom.SelectedDate;
                var dateTo = BatchDateTo.SelectedDate;
                foreach (var b in batches)
                {
                    if (dateFrom.HasValue && b.StartTime < dateFrom.Value) continue;
                    if (dateTo.HasValue && b.StartTime > dateTo.Value) continue;
                    _batchReportData.Add(b);
                }
                BatchReportGrid.ItemsSource = _batchReportData;
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private void ExportBatchToExcel_Click(object sender, RoutedEventArgs e)
        {
            var data = BatchReportGrid.ItemsSource;
            if (data == null)
            {
                MessageBox.Show("Нет данных для экспорта. Сначала сформируйте отчёт.");
                return;
            }

            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"BatchReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            ExcelExporter.ExportToExcel(path, "Партии", data.Cast<object>().ToList());
        }

        private void ExportBatchToCsv_Click(object sender, RoutedEventArgs e)
        {
            if (_batchReportData.Count == 0) { MessageBox.Show("Нет данных"); return; }
            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"BatchReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            CsvExporter.ExportToCsv(path, _batchReportData);
        }

        // Отчет по отклонениям (уже есть)
        private async void GenerateDeviationReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var deviations = await _apiClient.GetDeviationsAsync();
                _deviationReportData.Clear();
                var dateFrom = DeviationDateFrom.SelectedDate;
                var dateTo = DeviationDateTo.SelectedDate;
                foreach (var d in deviations)
                {
                    if (dateFrom.HasValue && d.CreatedAt < dateFrom.Value) continue;
                    if (dateTo.HasValue && d.CreatedAt > dateTo.Value) continue;
                    _deviationReportData.Add(d);
                }
                DeviationReportGrid.ItemsSource = _deviationReportData;
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private void ExportDeviationToExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_deviationReportData.Count == 0) { MessageBox.Show("Нет данных"); return; }
            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"DeviationReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            ExcelExporter.ExportToExcel(path, "Отклонения", _deviationReportData);
        }

        private void ExportDeviationToCsv_Click(object sender, RoutedEventArgs e)
        {
            if (_deviationReportData.Count == 0) { MessageBox.Show("Нет данных"); return; }
            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"DeviationReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            CsvExporter.ExportToCsv(path, _deviationReportData);
        }

        // Отчет по рецептурам
        private async void GenerateRecipeReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var recipes = await _apiClient.GetRecipesAsync();
                var products = await _apiClient.GetProductsAsync();
                var productDict = products.ToDictionary(p => p.Id, p => p.Name);

                _recipeReportData.Clear();
                foreach (var r in recipes)
                {
                    r.ProductName = productDict.ContainsKey(r.ProductId) ? productDict[r.ProductId] : "Неизвестно";
                    _recipeReportData.Add(r);
                }
                RecipeReportGrid.ItemsSource = _recipeReportData;
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private void ExportRecipeToExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_recipeReportData.Count == 0) { MessageBox.Show("Нет данных"); return; }
            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"RecipeReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            ExcelExporter.ExportToExcel(path, "Рецептуры", _recipeReportData);
        }

        private void ExportRecipeToCsv_Click(object sender, RoutedEventArgs e)
        {
            if (_recipeReportData.Count == 0) { MessageBox.Show("Нет данных"); return; }
            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"RecipeReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            CsvExporter.ExportToCsv(path, _recipeReportData);
        }

        private async void GenerateExtruderReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var telemetry = await _apiClient.GetExtruderTelemetryAsync();

                if (telemetry == null || telemetry.Count == 0)
                {
                    MessageBox.Show("Нет данных телеметрии экструдера");
                    ExtruderReportGrid.ItemsSource = null;
                    _extruderReportData.Clear();
                    return;
                }

                var batches = await _apiClient.GetBatchesAsync();
                var batchDict = batches.ToDictionary(b => b.Id, b => b.BatchNumber);

                // СОХРАНЯЕМ В ПЕРЕМЕННУЮ
                _extruderReportData.Clear();
                foreach (var t in telemetry)
                {
                    _extruderReportData.Add(new
                    {
                        Партия = batchDict.ContainsKey(t.BatchId) ? batchDict[t.BatchId] : "Неизвестно",
                        Зона = t.ZoneName,
                        Параметр = t.ParameterName,
                        Факт = t.ActualValue,
                        План = t.PlannedValue ?? "-",
                        Отклонение = t.DeviationFlag ? "Да" : "Нет",
                        Время = t.RecordedAt.ToString("dd.MM.yyyy HH:mm")
                    });
                }

                ExtruderReportGrid.ItemsSource = _extruderReportData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void ExportExtruderToExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_extruderReportData == null || _extruderReportData.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта. Сначала сформируйте отчёт.");
                return;
            }

            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"ExtruderReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            ExcelExporter.ExportToExcel(path, "Экструдер", _extruderReportData);
        }

        private void ExportExtruderToCsv_Click(object sender, RoutedEventArgs e)
        {
            if (_extruderReportData == null || _extruderReportData.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта. Сначала сформируйте отчёт.");
                return;
            }

            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"ExtruderReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            CsvExporter.ExportToCsv(path, _extruderReportData);
        }

        private async void GenerateBlockReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var labTests = await _apiClient.GetLabTestsAsync();
                var blockedTests = labTests.Where(t => t.Decision == "blocked").ToList();

                if (blockedTests.Count == 0)
                {
                    MessageBox.Show("Нет заблокированных партий", "Информация",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    BlockReportGrid.ItemsSource = null;
                    return;
                }

                // Загружаем данные о партиях и продуктах
                var batches = await _apiClient.GetBatchesAsync();
                var products = await _apiClient.GetProductsAsync();
                var productDict = products.ToDictionary(p => p.Id, p => p.Name);
                var batchDict = batches.ToDictionary(b => b.Id, b => new { b.BatchNumber, b.ProductId });

                var reportData = blockedTests.Select(t => new
                {
                    Партия = batchDict.ContainsKey(t.ObjectId) ? batchDict[t.ObjectId].BatchNumber : "Неизвестно",
                    Продукт = batchDict.ContainsKey(t.ObjectId) && productDict.ContainsKey(batchDict[t.ObjectId].ProductId)
                              ? productDict[batchDict[t.ObjectId].ProductId] : "Неизвестно",
                    Дата_блокировки = t.TestedAt?.ToString("dd.MM.yyyy") ?? "-",
                    Причина = t.DecisionReason ?? "-",
                    Ответственный = t.TestedBy?.ToString() ?? "-"
                });

                BlockReportGrid.ItemsSource = reportData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void ExportBlockToExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_blockReportData.Count == 0) { MessageBox.Show("Нет данных"); return; }
            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"BlockReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            ExcelExporter.ExportToExcel(path, "Блокировки", _blockReportData);
        }

        private void ExportBlockToCsv_Click(object sender, RoutedEventArgs e)
        {
            if (_blockReportData.Count == 0) { MessageBox.Show("Нет данных"); return; }
            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"BlockReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            CsvExporter.ExportToCsv(path, _blockReportData);
        }
    }
}