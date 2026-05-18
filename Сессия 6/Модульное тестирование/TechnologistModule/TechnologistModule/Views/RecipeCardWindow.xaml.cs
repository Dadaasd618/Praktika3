using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TechnologistModule.Models;
using TechnologistModule.Services;

namespace TechnologistModule.Views
{
    public partial class RecipeCardWindow : Window
    {
        private readonly ApiClient _apiClient;
        private readonly Recipe _recipe;
        private readonly bool _isEditMode;
        private ObservableCollection<Product> _products;
        private ObservableCollection<RecipeComponent> _components;

        public RecipeCardWindow(ApiClient apiClient, Recipe recipe = null)
        {
            InitializeComponent();
            _apiClient = apiClient;
            _recipe = recipe ?? new Recipe { Components = new System.Collections.Generic.List<RecipeComponent>() };
            _isEditMode = recipe != null && recipe.Id > 0;
            _components = new ObservableCollection<RecipeComponent>(_recipe.Components ?? new System.Collections.Generic.List<RecipeComponent>());

            Title = _isEditMode ? "✏️ Редактирование рецептуры" : "➕ Новая рецептура";
            TitleText.Text = _isEditMode ? $"✏️ Редактирование - v{_recipe.Version}" : "📋 Новая рецептура";
            SaveButton.Content = _isEditMode ? "Обновить" : "Сохранить";
            ComponentsGrid.ItemsSource = _components;

            Loaded += async (s, e) => await LoadProducts();
            UpdateTotalPercent();
        }

        private async Task LoadProducts()
        {
            var productsList = await _apiClient.GetProductsAsync();
            _products = new ObservableCollection<Product>(productsList);
            ProductCombo.ItemsSource = _products;

            if (_isEditMode)
            {
                ProductCombo.SelectedValue = _recipe.ProductId;
                VersionBox.Text = _recipe.Version;
            }
        }

        private void AddComponent_Click(object sender, RoutedEventArgs e)
        {
            _components.Add(new RecipeComponent
            {
                RawMaterialName = "Новый компонент",
                Percentage = 0,
                Tolerance = 0,
                LoadOrder = _components.Count + 1
            });
            UpdateTotalPercent();
        }

        private void RemoveComponent_Click(object sender, RoutedEventArgs e)
        {
            var component = (sender as Button)?.Tag as RecipeComponent;
            if (component != null)
            {
                _components.Remove(component);
                for (int i = 0; i < _components.Count; i++)
                    _components[i].LoadOrder = i + 1;
                UpdateTotalPercent();
            }
        }

        private void UpdateTotalPercent()
        {
            var total = _components.Sum(c => c.Percentage);
            TotalPercentText.Text = $"{total:F2}%";
            TotalPercentText.Foreground = total == 100m ?
                System.Windows.Media.Brushes.Green :
                System.Windows.Media.Brushes.Red;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ProductCombo.SelectedValue == null)
            {
                MessageBox.Show("Выберите продукт!");
                return;
            }
            if (string.IsNullOrWhiteSpace(VersionBox.Text))
            {
                MessageBox.Show("Введите версию!");
                return;
            }
            if (_components.Count == 0)
            {
                MessageBox.Show("Добавьте компоненты!");
                return;
            }
            if (Math.Abs(_components.Sum(c => c.Percentage) - 100) > 0.01m)
            {
                MessageBox.Show("Сумма долей должна быть 100%!");
                return;
            }

            _recipe.ProductId = (long)ProductCombo.SelectedValue;
            _recipe.Version = VersionBox.Text;
            _recipe.Components = _components.ToList();

            SaveButton.IsEnabled = false;
            SaveButton.Content = "Сохранение...";

            bool success = _isEditMode
                ? await _apiClient.UpdateRecipeAsync(_recipe.Id, _recipe) != null
                : await _apiClient.CreateRecipeAsync(_recipe) != null;

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