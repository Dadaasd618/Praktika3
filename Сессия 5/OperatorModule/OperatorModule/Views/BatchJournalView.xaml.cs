using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OperatorModule.Models;
using OperatorModule.Services;

namespace OperatorModule.Views
{
    public partial class BatchJournalView : UserControl
    {
        private readonly ApiClient _apiClient;
        private ObservableCollection<ProductionBatch> _batches = new ObservableCollection<ProductionBatch>();
        private ObservableCollection<JournalStepViewModel> _steps = new ObservableCollection<JournalStepViewModel>();

        public BatchJournalView(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;

            this.Loaded += async (s, e) =>
            {
                await LoadBatches();
            };
        }

        private async Task LoadBatches()
        {
            try
            {
                var batches = await _apiClient.GetActiveBatchesAsync();
                _batches.Clear();

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

                if (_batches.Any())
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
                await LoadJournal(batch.Id);
                BatchInfoText.Text = batch.BatchNumber;
            }
        }

        private async Task LoadJournal(long batchId)
        {
            try
            {
                var steps = await _apiClient.GetBatchJournalAsync(batchId);

                _steps.Clear();

                foreach (var step in steps)
                {
                    _steps.Add(new JournalStepViewModel(step));
                }

                StepsGrid.ItemsSource = _steps;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadBatches();
            if (BatchCombo.SelectedItem is ProductionBatch batch)
            {
                await LoadJournal(batch.Id);
            }
        }
    }

    // ViewModel для отображения с цветами
    public class JournalStepViewModel
    {
        private readonly BatchStep _step;

        public JournalStepViewModel(BatchStep step)
        {
            _step = step;
        }

        public int StepOrder => _step.StepOrder;
        public string StepName => _step.StepName;

        public string Status => _step.Status;

        public string StatusDisplay => _step.Status switch
        {
            "completed" => "✅ Завершен",
            "in_progress" => "🔄 Выполняется",
            "pending" => "⏳ Ожидание",
            _ => _step.Status
        };

        public Brush StatusBackground => _step.Status switch
        {
            "completed" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5")),
            "in_progress" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE")),
            "pending" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
            _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"))
        };

        public Brush StatusBorder => _step.Status switch
        {
            "completed" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
            "in_progress" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
            "pending" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
            _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"))
        };

        public Brush StatusForeground => _step.Status switch
        {
            "completed" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#065F46")),
            "in_progress" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E40AF")),
            "pending" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
            _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"))
        };

        public string ActualValueDisplay => _step.ActualValue?.ToString() ?? "-";

        public Brush ActualValueColor => (_step.DeviationFlag && _step.Status == "completed")
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));

        public FontWeight ActualValueWeight => (_step.DeviationFlag && _step.Status == "completed")
            ? FontWeights.Bold
            : FontWeights.Normal;

        public string DurationDisplay => _step.ActualDurationMin?.ToString() ?? "-";

        public string DeviationDisplay => _step.DeviationFlag ? "⚠️ Есть" : "✓ Нет";

        public Brush DeviationBackground => _step.DeviationFlag
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF2F2"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4"));

        public Brush DeviationForeground => _step.DeviationFlag
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));

        public string StartedAtDisplay => _step.StartedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";

        public string CompletedAtDisplay => _step.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";
    }
}