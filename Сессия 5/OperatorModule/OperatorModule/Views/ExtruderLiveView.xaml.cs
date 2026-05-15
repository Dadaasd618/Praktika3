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
    public partial class ExtruderLiveView : UserControl
    {
        private readonly ApiClient _apiClient;
        private ObservableCollection<ProductionBatch> _batches = new ObservableCollection<ProductionBatch>();
        private ObservableCollection<TelemetryItem> _temperatureZones = new ObservableCollection<TelemetryItem>();
        private ObservableCollection<TelemetryItem> _pressureParams = new ObservableCollection<TelemetryItem>();
        private System.Timers.Timer _refreshTimer;

        public ExtruderLiveView(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;

            this.Loaded += async (s, e) =>
            {
                await LoadBatches();
                StartAutoRefresh();
            };

            this.Unloaded += (s, e) => StopAutoRefresh();
        }

        private void StartAutoRefresh()
        {
            _refreshTimer = new System.Timers.Timer(3000); // Обновление каждые 3 секунды
            _refreshTimer.Elapsed += async (s, e) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    if (BatchCombo.SelectedItem != null)
                    {
                        await LoadTelemetry();
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
                await LoadTelemetry();
            }
        }

        private async Task LoadTelemetry()
        {
            try
            {
                var batch = BatchCombo.SelectedItem as ProductionBatch;
                if (batch == null) return;

                var telemetry = await _apiClient.GetExtruderTelemetryAsync(batch.Id);

                System.Diagnostics.Debug.WriteLine($"Loaded {telemetry.Count} telemetry items for batch {batch.Id}");

                _temperatureZones.Clear();
                _pressureParams.Clear();

                // Берем только последние записи (убираем дубликаты по зонам)
                var latestTelemetry = telemetry
                    .GroupBy(t => new { t.ZoneName, t.ParameterName })
                    .Select(g => g.OrderByDescending(t => t.RecordedAt).First())
                    .ToList();

                foreach (var item in latestTelemetry)
                {
                    if (item.ParameterName == "Температура")
                    {
                        _temperatureZones.Add(item);
                    }
                    else if (item.ParameterName == "Давление")
                    {
                        _pressureParams.Add(item);
                    }
                }

                TemperatureZones.ItemsSource = _temperatureZones;
                PressureParameters.ItemsSource = _pressureParams;

                System.Diagnostics.Debug.WriteLine($"Temperature zones: {_temperatureZones.Count}, Pressure: {_pressureParams.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки телеметрии: {ex.Message}");
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadBatches();
            if (BatchCombo.SelectedItem != null)
            {
                await LoadTelemetry();
            }
        }
    }
}