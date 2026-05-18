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
    public partial class RecipesControl : UserControl
    {
        private readonly ApiClient _apiClient;
        private ObservableCollection<Recipe> _allRecipes = new ObservableCollection<Recipe>();

        public RecipesControl(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;
            this.Loaded += async (s, e) => await LoadRecipes();
        }

        private async Task LoadRecipes()
        {
            try
            {
                var products = await _apiClient.GetProductsAsync();
                var productDict = products.ToDictionary(p => p.Id, p => p.Name);

                var recipesList = await _apiClient.GetRecipesAsync();
                _allRecipes.Clear();
                foreach (var r in recipesList)
                {
                    r.ProductName = productDict.ContainsKey(r.ProductId) ? productDict[r.ProductId] : "Неизвестно";
                    _allRecipes.Add(r);
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
            if (RecipesGrid == null) return;

            var query = _allRecipes.AsEnumerable();

            var searchText = SearchBox?.Text?.ToLower();
            if (!string.IsNullOrEmpty(searchText))
                query = query.Where(r => (r.ProductName?.ToLower().Contains(searchText) ?? false) ||
                                        (r.Version?.Contains(searchText) ?? false));

            if (StatusFilter?.SelectedItem is ComboBoxItem statusItem && statusItem.Content?.ToString() != "Все статусы")
            {
                var statusFilter = statusItem.Content?.ToString();
                if (!string.IsNullOrEmpty(statusFilter))
                    query = query.Where(r => r.Status == statusFilter);
            }

            RecipesGrid.ItemsSource = query.ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private async void NewRecipe_Click(object sender, RoutedEventArgs e)
        {
            var window = new RecipeCardWindow(_apiClient, null);
            if (window.ShowDialog() == true)
                await LoadRecipes();
        }

        private async void RecipesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecipesGrid.SelectedItem is Recipe recipe)
            {
                var window = new RecipeCardWindow(_apiClient, recipe);
                window.ShowDialog();
                await LoadRecipes();
                RecipesGrid.SelectedItem = null;
            }
        }

        private async void EditRecipe_Click(object sender, RoutedEventArgs e)
        {
            var recipe = (sender as Button)?.Tag as Recipe;
            if (recipe != null)
            {
                var window = new RecipeCardWindow(_apiClient, recipe);
                if (window.ShowDialog() == true)
                    await LoadRecipes();
            }
        }

        private async void ApproveRecipe_Click(object sender, RoutedEventArgs e)
        {
            var recipe = (sender as Button)?.Tag as Recipe;
            if (recipe != null)
            {
                if (MessageBox.Show($"Утвердить рецептуру {recipe.ProductName} v{recipe.Version}?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _apiClient.ApproveRecipeAsync(recipe.Id);
                    await LoadRecipes();
                }
            }
        }

        private async void ArchiveRecipe_Click(object sender, RoutedEventArgs e)
        {
            var recipe = (sender as Button)?.Tag as Recipe;
            if (recipe != null)
            {
                if (MessageBox.Show($"Архивировать рецептуру {recipe.ProductName} v{recipe.Version}?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _apiClient.ArchiveRecipeAsync(recipe.Id);
                    await LoadRecipes();
                }
            }
        }
    }
}