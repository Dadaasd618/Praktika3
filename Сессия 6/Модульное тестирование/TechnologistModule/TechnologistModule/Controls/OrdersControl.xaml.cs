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
    public partial class OrdersControl : UserControl
    {
        private readonly ApiClient _apiClient;
        private ObservableCollection<ProductionOrder> _allOrders = new ObservableCollection<ProductionOrder>();

        public OrdersControl(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;
            this.Loaded += async (s, e) => await LoadOrders();
        }

        private async Task LoadOrders()
        {
            try
            {
                var ordersList = await _apiClient.GetOrdersAsync();
                var recipes = await _apiClient.GetRecipesAsync();
                var products = await _apiClient.GetProductsAsync();

                var recipeDict = recipes.ToDictionary(r => r.Id, r => r);
                var productDict = products.ToDictionary(p => p.Id, p => p);

                _allOrders.Clear();
                foreach (var o in ordersList)
                {
                    if (recipeDict.ContainsKey(o.RecipeId))
                    {
                        var recipe = recipeDict[o.RecipeId];
                        o.RecipeVersion = recipe.Version;
                        o.ProductName = productDict.ContainsKey(recipe.ProductId) ? productDict[recipe.ProductId].Name : "Неизвестно";
                    }
                    else
                    {
                        o.RecipeVersion = "Неизвестно";
                        o.ProductName = "Неизвестно";
                    }
                    _allOrders.Add(o);
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
            if (OrdersGrid == null) return;

            var query = _allOrders.AsEnumerable();

            var searchText = SearchBox?.Text?.ToLower();
            if (!string.IsNullOrEmpty(searchText))
                query = query.Where(o => (o.OrderNumber?.ToLower().Contains(searchText) ?? false) ||
                                        (o.ProductName?.ToLower().Contains(searchText) ?? false));

            if (StatusFilter?.SelectedItem is ComboBoxItem statusItem && statusItem.Content?.ToString() != "Все статусы")
            {
                var statusFilter = statusItem.Content?.ToString();
                if (!string.IsNullOrEmpty(statusFilter))
                    query = query.Where(o => o.Status == statusFilter);
            }

            OrdersGrid.ItemsSource = query.ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private async void NewOrder_Click(object sender, RoutedEventArgs e)
        {
            var window = new OrderCardWindow(_apiClient, null);
            if (window.ShowDialog() == true)
                await LoadOrders();
        }

        private async void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrdersGrid.SelectedItem is ProductionOrder order)
            {
                var window = new OrderCardWindow(_apiClient, order);
                window.ShowDialog();
                await LoadOrders();
                OrdersGrid.SelectedItem = null;
            }
        }

        private async void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            var order = (sender as Button)?.Tag as ProductionOrder;
            if (order != null)
            {
                var window = new OrderCardWindow(_apiClient, order);
                if (window.ShowDialog() == true)
                    await LoadOrders();
            }
        }

        private async void CancelOrder_Click(object sender, RoutedEventArgs e)
        {
            var order = (sender as Button)?.Tag as ProductionOrder;
            if (order != null)
            {
                if (MessageBox.Show($"Отменить заказ {order.OrderNumber}?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _apiClient.CancelOrderAsync(order.Id);
                    await LoadOrders();
                }
            }
        }
    }
}