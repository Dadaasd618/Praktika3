using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LaboratoryModule.Models;
using LaboratoryModule.Services;

namespace LaboratoryModule.Views
{
    public partial class ProductBatchesView : UserControl
    {
        private readonly ApiClient _apiClient;
        private ObservableCollection<ProductionBatch> _allBatches = new ObservableCollection<ProductionBatch>();

        public ProductBatchesView(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;
            this.Loaded += async (s, e) => await LoadBatches();
        }

        private async Task LoadBatches()
        {
            try
            {
                var batches = await _apiClient.GetProductionBatchesAsync();
                var products = await _apiClient.GetProductsAsync();
                var orders = await _apiClient.GetOrdersAsync();

                var productDict = products.ToDictionary(p => p.Id, p => p.Name);
                var orderDict = orders.ToDictionary(o => o.Id, o => o.OrderNumber);

                _allBatches.Clear();
                foreach (var b in batches)
                {
                    b.ProductName = productDict.ContainsKey(b.ProductId) ? productDict[b.ProductId] : "Неизвестно";
                    b.OrderNumber = orderDict.ContainsKey(b.OrderId) ? orderDict[b.OrderId] : "Неизвестно";
                    _allBatches.Add(b);
                }
                BatchesGrid.ItemsSource = _allBatches;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_allBatches == null) return;
            var searchText = SearchBox.Text?.ToLower();
            if (string.IsNullOrEmpty(searchText))
            {
                BatchesGrid.ItemsSource = _allBatches;
                return;
            }
            var filtered = _allBatches.Where(b => b.BatchNumber.ToLower().Contains(searchText) ||
                                                  (b.ProductName?.ToLower().Contains(searchText) ?? false));
            BatchesGrid.ItemsSource = filtered.ToList();
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DateFrom_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DateTo_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allBatches == null || _allBatches.Count == 0) return;

            var query = _allBatches.AsEnumerable();

            var searchText = SearchBox?.Text?.ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(b => b.BatchNumber.ToLower().Contains(searchText) ||
                                        (b.ProductName?.ToLower().Contains(searchText) ?? false));
            }

            if (StatusFilter?.SelectedItem is ComboBoxItem statusItem)
            {
                var statusFilter = statusItem.Content?.ToString();
                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "Все статусы")
                {
                    query = query.Where(b => b.Status == statusFilter);
                }
            }

            if (DateFrom?.SelectedDate != null)
                query = query.Where(b => b.StartTime.HasValue && b.StartTime.Value.Date >= DateFrom.SelectedDate.Value.Date);
            if (DateTo?.SelectedDate != null)
                query = query.Where(b => b.StartTime.HasValue && b.StartTime.Value.Date <= DateTo.SelectedDate.Value.Date);

            BatchesGrid.ItemsSource = query.ToList();
        }

        private void BatchesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BatchesGrid.SelectedItem is ProductionBatch batch)
            {
                var window = new ProductBatchCardWindow(_apiClient, batch);
                window.ShowDialog();
                _ = LoadBatches();
                BatchesGrid.SelectedItem = null;
            }
        }

        public async void CreateTest_Click(object sender, RoutedEventArgs e)
        {
            var batch = (sender as Button)?.Tag as ProductionBatch;
            if (batch != null)
            {
                var testWindow = new ProductTestWindow(_apiClient, null, batch);
                if (testWindow.ShowDialog() == true)
                {
                    await LoadBatches();
                }
            }
        }
    }
}