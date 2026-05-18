using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TechnologistModule.Models;
using TechnologistModule.Services;
using TechnologistModule.Views;

namespace TechnologistModule.Controls
{
    public partial class BatchesControl : UserControl
    {
        private readonly ApiClient _apiClient;
        private ObservableCollection<ProductionBatch> _allBatches = new ObservableCollection<ProductionBatch>();

        public BatchesControl(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;
            this.Loaded += async (s, e) => await LoadBatches();
        }

        private async Task LoadBatches()
        {
            try
            {
                var products = await _apiClient.GetProductsAsync();
                var productDict = products.ToDictionary(p => p.Id, p => p.Name);

                var batchesList = await _apiClient.GetBatchesAsync();
                _allBatches.Clear();
                foreach (var b in batchesList)
                {
                    b.ProductName = productDict.ContainsKey(b.ProductId) ? productDict[b.ProductId] : "Неизвестно";
                    _allBatches.Add(b);
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            if (BatchesGrid == null) return;

            var query = _allBatches.AsEnumerable();

            var searchText = SearchBox?.Text?.ToLower();
            if (!string.IsNullOrEmpty(searchText))
                query = query.Where(b => b.BatchNumber?.ToLower().Contains(searchText) ?? false);

            if (StatusFilter?.SelectedItem is ComboBoxItem statusItem && statusItem.Content?.ToString() != "Все статусы")
            {
                var statusFilter = statusItem.Content?.ToString();
                if (!string.IsNullOrEmpty(statusFilter))
                    query = query.Where(b => b.Status == statusFilter);
            }

            BatchesGrid.ItemsSource = query.ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private async void NewBatch_Click(object sender, RoutedEventArgs e)
        {
            var window = new BatchCardWindow(_apiClient, null);
            if (window.ShowDialog() == true)
                await LoadBatches();
        }

        private async void BatchesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BatchesGrid.SelectedItem is ProductionBatch batch)
            {
                var window = new BatchCardWindow(_apiClient, batch);
                window.ShowDialog();
                await LoadBatches();
                BatchesGrid.SelectedItem = null;
            }
        }

        private async void EditBatch_Click(object sender, RoutedEventArgs e)
        {
            var batch = (sender as Button)?.Tag as ProductionBatch;
            if (batch != null)
            {
                var window = new BatchCardWindow(_apiClient, batch);
                if (window.ShowDialog() == true)
                    await LoadBatches();
            }
        }

        private async void CancelBatch_Click(object sender, RoutedEventArgs e)
        {
            var batch = (sender as Button)?.Tag as ProductionBatch;
            if (batch != null)
            {
                if (MessageBox.Show($"Отменить партию {batch.BatchNumber}?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _apiClient.CancelBatchAsync(batch.Id);
                    await LoadBatches();
                }
            }
        }
    }
}