using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LaboratoryModule.Models;
using LaboratoryModule.Services;

namespace LaboratoryModule.Views
{
    public partial class ProductBatchCardWindow : Window
    {
        private readonly ApiClient _apiClient;
        private readonly ProductionBatch _batch;
        private ObservableCollection<LabTest> _tests = new ObservableCollection<LabTest>();

        public ProductBatchCardWindow(ApiClient apiClient, ProductionBatch batch)
        {
            InitializeComponent();
            _apiClient = apiClient;
            _batch = batch;

            Loaded += async (s, e) => await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                // Загружаем основную информацию
                var fullBatch = await _apiClient.GetProductionBatchAsync(_batch.Id);
                if (fullBatch != null)
                {
                    _batch.Status = fullBatch.Status;
                    UpdateStatusDisplay(_batch.Status);

                    BatchNumberText.Text = fullBatch.BatchNumber;
                    QuantityText.Text = $"{fullBatch.ActualQuantityKg:F2} кг";
                    ProductNameText.Text = fullBatch.ProductName;
                    OrderNumberText.Text = fullBatch.OrderNumber;
                    StartTimeText.Text = fullBatch.StartTime?.ToString("dd.MM.yyyy HH:mm") ?? "-";
                    RecipeVersionText.Text = fullBatch.RecipeVersionId.ToString();
                    TechCardText.Text = fullBatch.TechCardId.ToString();
                }

                // Загружаем испытания напрямую
                await LoadTestsDirect();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async Task LoadTestsDirect()
        {
            _tests.Clear();

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {App.AuthToken}");

                // Правильный URL: objectType=product, а не цифра
                var url = $"http://localhost:5134/api/Laboratory/tests?objectType=product&objectId={_batch.Id}";

                var response = await client.GetAsync(url);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseString);
                    var data = doc.RootElement.GetProperty("data");
                    var tests = JsonSerializer.Deserialize<List<LabTest>>(data.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (tests != null && tests.Any())
                    {
                        var users = await _apiClient.GetUsersAsync();
                        var userDict = users.ToDictionary(u => u.Id, u => u.FullName);

                        foreach (var t in tests)
                        {
                            t.TestedByName = t.TestedBy.HasValue && userDict.ContainsKey(t.TestedBy.Value)
                                ? userDict[t.TestedBy.Value] : "Неизвестно";
                            _tests.Add(t);
                        }
                    }
                }

                TestsGrid.ItemsSource = _tests;
                System.Diagnostics.Debug.WriteLine($"Загружено испытаний: {_tests.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private void UpdateStatusDisplay(string status)
        {
            StatusText.Text = status;
            var color = status switch
            {
                "planned" => "#94A3B8",
                "running" => "#3B82F6",
                "quality_control" => "#F59E0B",
                "approved" => "#10B981",
                "blocked" => "#EF4444",
                "completed" => "#10B981",
                _ => "#64748B"
            };
            StatusBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            CurrentStatusText.Text = status;
        }

        private async void RefreshTests_Click(object sender, RoutedEventArgs e)
        {
            await LoadTestsDirect();
            MessageBox.Show($"Обновлено. Найдено испытаний: {_tests.Count}", "Информация",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void NewTest_Click(object sender, RoutedEventArgs e)
        {
            var testWindow = new ProductTestWindow(_apiClient, null, _batch);
            if (testWindow.ShowDialog() == true)
            {
                await LoadData();
            }
        }

        private async void TestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TestsGrid.SelectedItem is LabTest test)
            {
                var testWindow = new ProductTestWindow(_apiClient, test, _batch);
                if (testWindow.ShowDialog() == true)
                {
                    await LoadData();
                }
                TestsGrid.SelectedItem = null;
            }
        }

        private async void MakeDecision_Click(object sender, RoutedEventArgs e)
        {
            await LoadTestsDirect();

            var completedTests = _tests.Where(t => t.Status == "completed").ToList();
            if (completedTests.Count == 0)
            {
                MessageBox.Show("Невозможно принять решение: нет завершённых испытаний", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var decisionWindow = new Window
            {
                Title = "Принятие решения",
                Width = 450,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = Brushes.White
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var titleText = new TextBlock { Text = "🔬 Принятие решения по партии", FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 15) };
            stackPanel.Children.Add(titleText);

            var decisionCombo = new ComboBox { Margin = new Thickness(0, 5, 0, 15), Height = 35 };
            decisionCombo.Items.Add(new ComboBoxItem { Content = "✅ Разрешить", Tag = "approved", IsSelected = true });
            decisionCombo.Items.Add(new ComboBoxItem { Content = "❌ Заблокировать", Tag = "blocked" });
            stackPanel.Children.Add(new TextBlock { Text = "Решение:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            stackPanel.Children.Add(decisionCombo);

            var reasonBox = new TextBox { Height = 80, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 15) };
            stackPanel.Children.Add(new TextBlock { Text = "Причина / Комментарий:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            stackPanel.Children.Add(reasonBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var cancelBtn = new Button { Content = "Отмена", Width = 80, Height = 32, Margin = new Thickness(0, 0, 10, 0), Style = (Style)FindResource("SecondaryButton") };
            cancelBtn.Click += (s2, e2) => decisionWindow.Close();
            var okBtn = new Button { Content = "Применить", Width = 80, Height = 32, Style = (Style)FindResource("PrimaryButton") };
            okBtn.Click += async (s2, e2) =>
            {
                var selectedItem = decisionCombo.SelectedItem as ComboBoxItem;
                var decision = selectedItem?.Tag?.ToString();
                var reason = reasonBox.Text.Trim();

                if (decision == "blocked" && string.IsNullOrEmpty(reason))
                {
                    MessageBox.Show("При блокировке партии необходимо указать причину!", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var success = await _apiClient.UpdateProductionBatchStatusAsync(_batch.Id, decision, reason);
                if (success)
                {
                    _batch.Status = decision;
                    decisionWindow.Close();
                    await LoadData();
                    MessageBox.Show($"Партия {(decision == "approved" ? "разрешена" : "заблокирована")}!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Ошибка при сохранении решения", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            buttonPanel.Children.Add(cancelBtn);
            buttonPanel.Children.Add(okBtn);
            stackPanel.Children.Add(buttonPanel);

            decisionWindow.Content = stackPanel;
            decisionWindow.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}