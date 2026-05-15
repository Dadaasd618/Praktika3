using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OperatorModule.Models;
using OperatorModule.Services;

namespace OperatorModule.Views
{
    public partial class BatchProgramView : UserControl
    {
        private readonly ApiClient _apiClient;
        private long _batchId;
        private ProductionBatch _batch;
        private ObservableCollection<ProductionBatch> _batches = new ObservableCollection<ProductionBatch>();
        private ObservableCollection<BatchStep> _steps = new ObservableCollection<BatchStep>();
        private BatchStep _currentStep;
        private System.Timers.Timer _refreshTimer;

        public BatchProgramView(ApiClient apiClient, long batchId = 0)
        {
            InitializeComponent();
            _apiClient = apiClient;
            _batchId = batchId;

            this.Loaded += async (s, e) =>
            {
                await LoadBatches();
                // StartAutoRefresh();  // ← ЗАКОММЕНТИРОВАТЬ ИЛИ УДАЛИТЬ
            };

            // this.Unloaded += (s, e) => StopAutoRefresh();  // ← ТОЖЕ УДАЛИТЬ
        }

        private void StartAutoRefresh()
        {
            _refreshTimer = new System.Timers.Timer(5000);
            _refreshTimer.Elapsed += async (s, e) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    if (BatchCombo.SelectedItem != null)
                    {
                        await LoadBatchProgram();
                    }
                });
            };
            _refreshTimer.Start();
        }

        private void StopAutoRefresh()
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
        }

        private async Task LoadBatches()
        {
            try
            {
                var batches = await _apiClient.GetActiveBatchesAsync();
                _batches.Clear();

                // Убираем дубликаты по BatchNumber
                var uniqueBatches = batches
                    .GroupBy(b => b.BatchNumber)
                    .Select(g => g.First())
                    .ToList();

                foreach (var b in uniqueBatches)
                {
                    _batches.Add(b);
                }

                BatchCombo.ItemsSource = _batches;
                BatchCombo.DisplayMemberPath = "BatchNumber";
                BatchCombo.SelectedValuePath = "Id";

                if (_batches.Any() && _batchId == 0)
                {
                    BatchCombo.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки партий: {ex.Message}");
            }
        }

        private async void BatchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BatchCombo.SelectedItem is ProductionBatch batch)
            {
                _batchId = batch.Id;
                await LoadBatchProgram();
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadBatches();
            if (_batchId != 0)
            {
                await LoadBatchProgram();
            }
        }

        private async Task LoadBatchProgram()
        {
            try
            {
                _batch = await _apiClient.GetBatchProgramAsync(_batchId);
                if (_batch == null)
                {
                    if (_batchId != 0)
                        MessageBox.Show($"Партия с ID {_batchId} не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                BatchNumberText.Text = $"Партия: {_batch.BatchNumber}";
                ProductNameText.Text = $"| Продукт: {_batch.ProductName}";
                LineText.Text = $"| Линия: {_batch.Line ?? "L-01"}";

                var statusColor = _batch.Status == "running" ? "#10B981" : (_batch.Status == "quality_control" ? "#F59E0B" : "#94A3B8");
                StatusIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(statusColor));
                StatusText.Text = _batch.Status == "running" ? "В работе" : (_batch.Status == "quality_control" ? "Контроль качества" : "Ожидание");

                if (_batch.Steps != null && _batch.Steps.Any())
                {
                    _steps.Clear();
                    foreach (var s in _batch.Steps)
                    {
                        if (s.Status != "completed")
                        {
                            _steps.Add(s);
                        }
                    }

                    if (_steps.Count == 0)
                    {
                        StepsList.ItemsSource = null;
                        return;
                    }

                    StepsList.ItemsSource = _steps;

                    _currentStep = _steps.FirstOrDefault(s => s.Status == "in_progress");
                    if (_currentStep == null)
                    {
                        _currentStep = _steps.FirstOrDefault(s => s.Status == "pending");
                    }

                    if (_currentStep != null)
                    {
                        StepsList.SelectedItem = _currentStep;
                        DisplayStep(_currentStep);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RefreshData()
        {
            if (_batchId == 0) return;

            var freshBatch = await _apiClient.GetBatchProgramAsync(_batchId);
            if (freshBatch != null && freshBatch.Steps != null)
            {
                _steps.Clear();
                foreach (var s in freshBatch.Steps)
                {
                    if (s.Status != "completed")
                    {
                        _steps.Add(s);
                    }
                }

                if (_steps.Count == 0)
                {
                    StepsList.ItemsSource = null;
                    return;
                }

                StepsList.ItemsSource = _steps;

                var freshCurrentStep = _steps.FirstOrDefault(s => s.Status == "in_progress");
                if (freshCurrentStep == null)
                {
                    freshCurrentStep = _steps.FirstOrDefault(s => s.Status == "pending");
                }

                if (freshCurrentStep != null && (_currentStep == null || freshCurrentStep.Id != _currentStep.Id))
                {
                    _currentStep = freshCurrentStep;
                    DisplayStep(_currentStep);
                }
                else if (_currentStep != null)
                {
                    var updatedStep = _steps.FirstOrDefault(s => s.Id == _currentStep.Id);
                    if (updatedStep != null)
                    {
                        _currentStep = updatedStep;
                        DisplayStep(_currentStep);
                    }
                }

                StepsList.Items.Refresh();
            }
        }

        private void DisplayStep(BatchStep step)
        {
            if (step == null) return;

            StepNameTitle.Text = $"Шаг: {step.StepName}";
            StepTypeText.Text = step.StepType ?? "Операция";

            switch (step.Status)
            {
                case "pending":
                    StepStatusText.Text = "Не начат";
                    StepStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                    StartStepButton.IsEnabled = true;
                    CompleteStepButton.IsEnabled = false;
                    ActualValueBox.IsEnabled = true;
                    DurationBox.IsEnabled = true;
                    break;
                case "in_progress":
                    StepStatusText.Text = "Выполняется";
                    StepStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                    StartStepButton.IsEnabled = false;
                    CompleteStepButton.IsEnabled = true;
                    ActualValueBox.IsEnabled = true;
                    DurationBox.IsEnabled = true;
                    break;
                case "completed":
                    StepStatusText.Text = "Завершён";
                    StepStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                    StartStepButton.IsEnabled = false;
                    CompleteStepButton.IsEnabled = false;
                    ActualValueBox.IsEnabled = false;
                    DurationBox.IsEnabled = false;
                    break;
            }

            InstructionText.Text = step.Instruction ?? "Инструкции не заданы";

            if (step.PlannedMin.HasValue && step.PlannedMax.HasValue)
            {
                NormText.Text = $"Норма: {step.PlannedMin} - {step.PlannedMax} {step.Unit}";
                DurationNormText.Text = $"Норма: {step.PlannedMax} мин";
            }
            else if (step.PlannedMin.HasValue)
            {
                NormText.Text = $"Норма: ≥ {step.PlannedMin} {step.Unit}";
                DurationNormText.Text = "Норма: не задана";
            }
            else if (step.PlannedMax.HasValue)
            {
                NormText.Text = $"Норма: ≤ {step.PlannedMax} {step.Unit}";
                DurationNormText.Text = $"Норма: {step.PlannedMax} мин";
            }
            else
            {
                NormText.Text = "Норма: не задана";
                DurationNormText.Text = "Норма: не задана";
            }

            if (step.ActualValue.HasValue)
            {
                ActualValueBox.Text = step.ActualValue.Value.ToString();
            }
            else
            {
                ActualValueBox.Text = "";
            }

            if (step.ActualDurationMin.HasValue)
            {
                DurationBox.Text = step.ActualDurationMin.Value.ToString();
            }
            else
            {
                DurationBox.Text = "";
            }

            DeviationWarning.Visibility = step.DeviationFlag ? Visibility.Visible : Visibility.Collapsed;
        }

        private void StepsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StepsList.SelectedItem is BatchStep step)
            {
                _currentStep = step;
                DisplayStep(step);
            }
        }

        private async void StartStep_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep == null || _currentStep.Status != "pending")
            {
                MessageBox.Show("Шаг нельзя начать", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StartStepButton.IsEnabled = false;
            StartStepButton.Content = "Запуск...";

            var success = await _apiClient.StartStepAsync(_currentStep.Id, App.CurrentUser?.Id ?? 0);

            StartStepButton.IsEnabled = true;
            StartStepButton.Content = "▶ Начать шаг";

            if (success)
            {
                await LoadBatchProgram();  // вместо await RefreshData();
            }
            else
            {
                MessageBox.Show("Ошибка при запуске шага", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CompleteStep_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep == null || _currentStep.Status != "in_progress")
            {
                MessageBox.Show("Шаг нельзя завершить (статус не 'in_progress')", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(ActualValueBox.Text, out decimal actualValue))
            {
                MessageBox.Show("Введите корректное фактическое значение", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(DurationBox.Text, out int duration))
            {
                MessageBox.Show("Введите корректную длительность", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isDeviation = false;
            string deviationMsg = "";

            if (_currentStep.PlannedMin.HasValue && actualValue < _currentStep.PlannedMin.Value)
            {
                isDeviation = true;
                deviationMsg += $"Значение ниже нормы ({_currentStep.PlannedMin.Value} {_currentStep.Unit})\n";
            }
            if (_currentStep.PlannedMax.HasValue && actualValue > _currentStep.PlannedMax.Value)
            {
                isDeviation = true;
                deviationMsg += $"Значение выше нормы ({_currentStep.PlannedMax.Value} {_currentStep.Unit})\n";
            }

            if (isDeviation)
            {
                var result = MessageBox.Show($"Обнаружено отклонение:\n{deviationMsg}\nПродолжить?",
                    "Отклонение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                    return;
            }

            CompleteStepButton.IsEnabled = false;
            CompleteStepButton.Content = "Завершение...";

            try
            {
                // Отладка
                System.Diagnostics.Debug.WriteLine($"=== CompleteStep ===");
                System.Diagnostics.Debug.WriteLine($"StepId: {_currentStep.Id}");
                System.Diagnostics.Debug.WriteLine($"ActualValue: {actualValue}");
                System.Diagnostics.Debug.WriteLine($"Duration: {duration}");
                System.Diagnostics.Debug.WriteLine($"OperatorId: {App.CurrentUser?.Id ?? 0}");

                var success = await _apiClient.CompleteStepAsync(_currentStep.Id, actualValue, duration, "", App.CurrentUser?.Id ?? 0);

                System.Diagnostics.Debug.WriteLine($"Success: {success}");

                if (success)
                {
                    await LoadBatchProgram();  // вместо await RefreshData();
                }
                else
                {
                    MessageBox.Show("Ошибка при завершении шага (API вернул false)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // Показываем полную ошибку
                MessageBox.Show($"Ошибка при завершении шага:\n{ex.Message}\n\n{ex.StackTrace}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CompleteStepButton.IsEnabled = true;
                CompleteStepButton.Content = "✓ Завершить шаг";
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^0-9,.]");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}