using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OperatorModule.Models;
using OperatorModule.Services;

namespace OperatorModule.Views
{
    public partial class ReportProblemView : UserControl
    {
        private readonly ApiClient _apiClient;
        private ObservableCollection<ProductionBatch> _batches = new ObservableCollection<ProductionBatch>();
        private ObservableCollection<BatchStep> _steps = new ObservableCollection<BatchStep>();

        public ReportProblemView(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;

            this.Loaded += async (s, e) => await LoadBatches();
        }

        private async Task LoadBatches()
        {
            try
            {
                var batches = await _apiClient.GetActiveBatchesAsync();
                var uniqueBatches = batches
                    .GroupBy(b => b.BatchNumber)
                    .Select(g => g.First())
                    .ToList();

                BatchCombo.ItemsSource = uniqueBatches;
                BatchCombo.DisplayMemberPath = "BatchNumber";
                BatchCombo.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private async void BatchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BatchCombo.SelectedItem is ProductionBatch batch)
            {
                await LoadSteps(batch.Id);
            }
        }

        private async Task LoadSteps(long batchId)
        {
            try
            {
                var batch = await _apiClient.GetBatchProgramAsync(batchId);
                if (batch != null && batch.Steps != null)
                {
                    _steps.Clear();
                    foreach (var s in batch.Steps)
                    {
                        // Показываем только активные шаги (не завершённые)
                        if (s.Status != "completed")
                        {
                            _steps.Add(s);
                        }
                    }

                    StepCombo.ItemsSource = _steps;
                    StepCombo.DisplayMemberPath = "StepName";
                    StepCombo.SelectedValuePath = "Id";

                    if (_steps.Any())
                    {
                        StepCombo.SelectedIndex = 0;
                    }
                    else
                    {
                        StepCombo.ItemsSource = null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private async void SubmitProblem_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            SuccessText.Visibility = Visibility.Collapsed;

            if (BatchCombo.SelectedItem == null)
            {
                ShowError("Выберите партию");
                return;
            }

            if (StepCombo.SelectedItem == null)
            {
                ShowError("Выберите шаг");
                return;
            }

            var description = DescriptionBox.Text.Trim();
            if (string.IsNullOrEmpty(description))
            {
                ShowError("Введите описание проблемы");
                return;
            }

            var batch = BatchCombo.SelectedItem as ProductionBatch;
            var step = StepCombo.SelectedItem as BatchStep;
            var problemType = (ProblemTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var severity = (SeverityCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "medium";

            SubmitButton.IsEnabled = false;
            SubmitButton.Content = "Отправка...";

            try
            {
                var success = await _apiClient.ReportProblemAsync(batch.Id, step.Id, problemType, description, severity, App.CurrentUser?.Id ?? 0);

                if (success)
                {
                    SuccessText.Text = "Сообщение отправлено! Технолог уведомлён.";
                    SuccessText.Visibility = Visibility.Visible;
                    DescriptionBox.Text = "";
                }
                else
                {
                    ShowError("Ошибка при отправке сообщения");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
            finally
            {
                SubmitButton.IsEnabled = true;
                SubmitButton.Content = "📤 Отправить сообщение";
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}