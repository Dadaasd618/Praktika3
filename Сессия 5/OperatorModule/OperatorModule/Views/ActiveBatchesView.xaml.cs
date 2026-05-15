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
    public partial class ActiveBatchesView : UserControl
    {
        private readonly ApiClient _apiClient;
        private ObservableCollection<ProductionBatch> _allBatches = new ObservableCollection<ProductionBatch>();
        private System.Timers.Timer _refreshTimer;

        public ActiveBatchesView(ApiClient apiClient)
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
            _refreshTimer = new System.Timers.Timer(10000);
            _refreshTimer.Elapsed += async (s, e) =>
            {
                Dispatcher.Invoke(async () => await LoadBatches());
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
                _allBatches.Clear();

                // Убираем дубликаты по BatchNumber
                var uniqueBatches = batches
                    .GroupBy(b => b.BatchNumber)
                    .Select(g => g.First())
                    .ToList();

                foreach (var b in uniqueBatches)
                {
                    _allBatches.Add(b);
                }
                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            if (BatchesGrid == null) return;

            var query = _allBatches.AsEnumerable();

            var searchText = SearchBox?.Text?.ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(b => b.BatchNumber.ToLower().Contains(searchText));
            }

            if (LineFilter?.SelectedItem is ComboBoxItem lineItem)
            {
                var lineFilter = lineItem.Content?.ToString();
                if (lineFilter != "Все линии" && !string.IsNullOrEmpty(lineFilter))
                {
                    query = query.Where(b => b.Line == lineFilter);
                }
            }

            if (StatusFilter?.SelectedItem is ComboBoxItem statusItem)
            {
                var statusFilter = statusItem.Content?.ToString();
                if (statusFilter != "Все статусы" && !string.IsNullOrEmpty(statusFilter))
                {
                    query = query.Where(b => b.Status == statusFilter);
                }
            }

            BatchesGrid.ItemsSource = query.ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadBatches();
        }

        // ОБРАБОТЧИК КЛИКА ПО СТРОКЕ
        private void BatchesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BatchesGrid.SelectedItem is ProductionBatch batch)
            {
                var programView = new BatchProgramView(_apiClient, batch.Id);
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    var contentArea = mainWindow.FindName("ContentArea") as System.Windows.Controls.ContentControl;
                    if (contentArea != null)
                    {
                        contentArea.Content = programView;
                    }
                }
                BatchesGrid.SelectedItem = null;
            }
        }

        private void GoToProgram_Click(object sender, RoutedEventArgs e)
        {
            var batch = (sender as Button)?.Tag as ProductionBatch;
            if (batch == null) return;

            var programView = new BatchProgramView(_apiClient, batch.Id);

            foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
            {
                if (window is MainWindow mainWindow)
                {
                    var contentArea = mainWindow.FindName("ContentArea") as System.Windows.Controls.ContentControl;
                    if (contentArea != null)
                    {
                        contentArea.Content = programView;
                    }
                    return;
                }
            }
        }
    }
}