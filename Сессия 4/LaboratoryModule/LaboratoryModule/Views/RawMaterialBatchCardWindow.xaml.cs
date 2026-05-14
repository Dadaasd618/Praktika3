using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LaboratoryModule.Models;
using LaboratoryModule.Services;

namespace LaboratoryModule.Views
{
    public partial class RawMaterialBatchCardWindow : Window
    {
        private readonly ApiClient _apiClient;
        private readonly RawMaterialBatch _batch;
        private ObservableCollection<LabTest> _tests = new ObservableCollection<LabTest>();

        public RawMaterialBatchCardWindow(ApiClient apiClient, RawMaterialBatch batch)
        {
            InitializeComponent();
            _apiClient = apiClient;
            _batch = batch;

            Loaded += async (s, e) => await LoadData();
        }

        private async Task LoadData()
        {
            BatchNumberText.Text = _batch.BatchNumber;
            MaterialText.Text = _batch.RawMaterialName;
            CategoryText.Text = _batch.Category;
            SupplierText.Text = _batch.SupplierName;
            ArrivalDateText.Text = _batch.ArrivalDate.ToString("dd.MM.yyyy");
            QuantityText.Text = $"{_batch.QuantityKg:F2} {_batch.Unit}";
            StorageText.Text = _batch.StorageLocation ?? "Не указано";

            UpdateStatusDisplay(_batch.Status);

            if (_batch.Tests != null)
            {
                _tests.Clear();
                var users = await _apiClient.GetUsersAsync();
                var userDict = users.ToDictionary(u => u.Id, u => u.FullName);

                foreach (var t in _batch.Tests)
                {
                    t.TestedByName = t.TestedBy.HasValue && userDict.ContainsKey(t.TestedBy.Value)
                        ? userDict[t.TestedBy.Value] : "Неизвестно";
                    _tests.Add(t);
                }
                TestsGrid.ItemsSource = _tests;
            }

            if (_batch.Status == "approved" || _batch.Status == "blocked")
            {
                DecisionButton.Visibility = Visibility.Collapsed;
                DecisionCommentBox.IsEnabled = false;
                var lastTest = _tests.LastOrDefault(t => t.Decision != null);
                if (lastTest != null)
                {
                    DecisionCommentBox.Text = lastTest.DecisionReason;
                    DecisionDateText.Text = lastTest.TestedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";
                    DecisionByText.Text = lastTest.TestedByName;
                }
            }
            else
            {
                DecisionButton.Visibility = Visibility.Visible;
                DecisionCommentBox.IsEnabled = true;
            }
        }

        private void UpdateStatusDisplay(string status)
        {
            StatusText.Text = status;
            Color color;
            switch (status)
            {
                case "pending":
                    color = (Color)ColorConverter.ConvertFromString("#94A3B8");
                    break;
                case "testing":
                    color = (Color)ColorConverter.ConvertFromString("#F59E0B");
                    break;
                case "approved":
                    color = (Color)ColorConverter.ConvertFromString("#10B981");
                    break;
                case "blocked":
                    color = (Color)ColorConverter.ConvertFromString("#EF4444");
                    break;
                default:
                    color = (Color)ColorConverter.ConvertFromString("#64748B");
                    break;
            }
            StatusBorder.Background = new SolidColorBrush(color);
            CurrentStatusText.Text = status;

            // Обновляем цвет статуса в зависимости от статуса
            if (status == "approved")
                CurrentStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            else if (status == "blocked")
                CurrentStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            else
                CurrentStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
        }

        private async void NewTest_Click(object sender, RoutedEventArgs e)
        {
            var testWindow = new TestWindow(_apiClient, null, _batch);
            if (testWindow.ShowDialog() == true)
            {
                await RefreshData();
            }
        }

        private async void TestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TestsGrid.SelectedItem is LabTest test)
            {
                var testWindow = new TestWindow(_apiClient, test, _batch);
                testWindow.ShowDialog();
                await RefreshData();
                TestsGrid.SelectedItem = null;
            }
        }
        private async void RefreshTests_Click(object sender, RoutedEventArgs e)
        {
            await RefreshData();
        }
        private async Task RefreshData()
        {
            try
            {
                // Загружаем свежие данные партии с испытаниями
                var updatedBatch = await _apiClient.GetRawMaterialBatchAsync(_batch.Id);
                if (updatedBatch != null)
                {
                    _batch.Status = updatedBatch.Status;
                    _batch.Tests = updatedBatch.Tests;

                    // Обновляем статус на форме
                    UpdateStatusDisplay(_batch.Status);

                    // Обновляем список испытаний
                    _tests.Clear();
                    var users = await _apiClient.GetUsersAsync();
                    var userDict = users.ToDictionary(u => u.Id, u => u.FullName);

                    if (_batch.Tests != null)
                    {
                        foreach (var t in _batch.Tests)
                        {
                            t.TestedByName = t.TestedBy.HasValue && userDict.ContainsKey(t.TestedBy.Value)
                                ? userDict[t.TestedBy.Value] : "Неизвестно";
                            _tests.Add(t);
                        }
                    }
                    TestsGrid.ItemsSource = _tests;

                    // Обновляем блок решения
                    if (_batch.Status == "approved" || _batch.Status == "blocked")
                    {
                        DecisionButton.Visibility = Visibility.Collapsed;
                        DecisionCommentBox.IsEnabled = false;
                        var lastTest = _tests.LastOrDefault(t => t.Decision != null);
                        if (lastTest != null)
                        {
                            DecisionCommentBox.Text = lastTest.DecisionReason;
                            DecisionDateText.Text = lastTest.TestedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";
                            DecisionByText.Text = lastTest.TestedByName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления: {ex.Message}");
            }
        }
        private async void MakeDecision_Click(object sender, RoutedEventArgs e)
        {
            var completedTests = _tests.Where(t => t.Status == "completed").ToList();
            if (completedTests.Count == 0)
            {
                MessageBox.Show("Невозможно принять решение: нет завершённых испытаний", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем, что все обязательные параметры заполнены
            foreach (var test in completedTests)
            {
                if (test.Parameters != null && test.Parameters.Any(p => p.MeasuredValue == null))
                {
                    MessageBox.Show($"В испытании №{test.TestNumber} не заполнены все параметры", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
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

                var success = await _apiClient.UpdateRawMaterialBatchStatusAsync(_batch.Id, decision, reason);
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