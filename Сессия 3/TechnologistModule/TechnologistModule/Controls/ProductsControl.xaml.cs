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
    public partial class ProductsControl : UserControl
    {
        private readonly ApiClient _apiClient;
        private ObservableCollection<Product> _allProducts = new ObservableCollection<Product>();

        public ProductsControl(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;
            this.Loaded += async (s, e) => await LoadProducts();
        }

        private async Task LoadProducts()
        {
            try
            {
                var productsList = await _apiClient.GetProductsAsync();
                _allProducts.Clear();
                foreach (var p in productsList)
                    _allProducts.Add(p);

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            if (ProductsGrid == null) return;

            var query = _allProducts.AsEnumerable();

            var searchText = SearchBox?.Text?.ToLower();
            if (!string.IsNullOrEmpty(searchText))
                query = query.Where(p => (p.Name?.ToLower().Contains(searchText) ?? false) ||
                                        (p.Code?.ToLower().Contains(searchText) ?? false));

            if (TypeFilter?.SelectedItem is ComboBoxItem typeItem && typeItem.Content?.ToString() != "Все типы")
            {
                var typeFilter = typeItem.Content?.ToString();
                if (!string.IsNullOrEmpty(typeFilter))
                    query = query.Where(p => p.Type == typeFilter);
            }

            if (StatusFilter?.SelectedItem is ComboBoxItem statusItem && statusItem.Content?.ToString() != "Все статусы")
            {
                var statusFilter = statusItem.Content?.ToString();
                if (!string.IsNullOrEmpty(statusFilter))
                    query = query.Where(p => p.Status == statusFilter);
            }

            ProductsGrid.ItemsSource = query.ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private async void NewProduct_Click(object sender, RoutedEventArgs e)
        {
            var window = new ProductCardWindow(_apiClient, null);
            if (window.ShowDialog() == true)
                await LoadProducts();
        }

        private async void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            var product = (sender as Button)?.Tag as Product;
            if (product != null)
            {
                var window = new ProductCardWindow(_apiClient, product);
                if (window.ShowDialog() == true)
                    await LoadProducts();
            }
        }

        private async void ArchiveProduct_Click(object sender, RoutedEventArgs e)
        {
            var product = (sender as Button)?.Tag as Product;
            if (product != null)
            {
                var result = MessageBox.Show($"Архивировать продукт '{product.Name}'?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _apiClient.ArchiveProductAsync(product.Id);
                    await LoadProducts();
                }
            }
        }
    }
}