using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using TechnologistModule.Models;
using TechnologistModule.Services;

namespace TechnologistModule.Views
{
    public partial class OrderCardWindow : Window
    {
        private readonly ApiClient _apiClient;
        private readonly ProductionOrder _order;
        private readonly bool _isEditMode;
        private ObservableCollection<Recipe> _recipes;

        public OrderCardWindow(ApiClient apiClient, ProductionOrder order = null)
        {
            InitializeComponent();
            _apiClient = apiClient;
            _order = order ?? new ProductionOrder();
            _isEditMode = order != null && order.Id > 0;

            Title = _isEditMode ? "✏️ Редактирование заказа" : "➕ Новый заказ";
            TitleText.Text = _isEditMode ? "✏️ Редактирование заказа" : "📄 Новый заказ";
            SaveButton.Content = _isEditMode ? "Обновить" : "Сохранить";

            Loaded += async (s, e) => await LoadRecipes();

            if (_isEditMode)
            {
                OrderNumberBox.Text = _order.OrderNumber;
                QuantityBox.Text = _order.PlannedQuantityKg.ToString();
                StartDatePicker.SelectedDate = _order.PlannedStartDate;
            }
        }

        private async Task LoadRecipes()
        {
            var recipesList = await _apiClient.GetRecipesAsync();
            var products = await _apiClient.GetProductsAsync();
            var productDict = products.ToDictionary(p => p.Id, p => p.Name);

            _recipes = new ObservableCollection<Recipe>();
            foreach (var r in recipesList)
            {
                r.ProductName = productDict.ContainsKey(r.ProductId) ? productDict[r.ProductId] : "Неизвестно";
                _recipes.Add(r);
            }

            RecipeCombo.ItemsSource = _recipes;
            RecipeCombo.DisplayMemberPath = "DisplayName";

            if (_isEditMode && _order.RecipeId > 0)
            {
                RecipeCombo.SelectedValue = _order.RecipeId;
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OrderNumberBox.Text))
            {
                MessageBox.Show("Введите номер заказа!");
                return;
            }
            if (RecipeCombo.SelectedValue == null)
            {
                MessageBox.Show("Выберите рецептуру!");
                return;
            }
            if (!decimal.TryParse(QuantityBox.Text, out var quantity))
            {
                MessageBox.Show("Введите корректное количество!");
                return;
            }
            if (StartDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату запуска!");
                return;
            }

            _order.OrderNumber = OrderNumberBox.Text.Trim();
            _order.RecipeId = (long)RecipeCombo.SelectedValue;
            _order.PlannedQuantityKg = quantity;
            _order.PlannedStartDate = StartDatePicker.SelectedDate.Value;

            SaveButton.IsEnabled = false;
            SaveButton.Content = "Сохранение...";

            bool success = _isEditMode
                ? await _apiClient.UpdateOrderAsync(_order.Id, _order) != null
                : await _apiClient.CreateOrderAsync(_order) != null;

            SaveButton.IsEnabled = true;
            SaveButton.Content = _isEditMode ? "Обновить" : "Сохранить";

            if (success)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Ошибка при сохранении!");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}