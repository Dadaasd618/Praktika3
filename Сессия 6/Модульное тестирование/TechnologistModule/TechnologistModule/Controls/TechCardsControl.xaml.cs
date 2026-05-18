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
    public partial class TechCardsControl : UserControl
    {
        private readonly ApiClient _apiClient;
        private ObservableCollection<TechCard> _allTechCards = new ObservableCollection<TechCard>();

        public TechCardsControl(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;
            this.Loaded += async (s, e) => await LoadTechCards();
        }

        private async Task LoadTechCards()
        {
            try
            {
                var products = await _apiClient.GetProductsAsync();
                var productDict = products.ToDictionary(p => p.Id, p => p.Name);

                var techCardsList = await _apiClient.GetTechCardsAsync();
                _allTechCards.Clear();
                foreach (var tc in techCardsList)
                {
                    tc.ProductName = productDict.ContainsKey(tc.ProductId) ? productDict[tc.ProductId] : "Неизвестно";
                    _allTechCards.Add(tc);
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
            if (TechCardsGrid == null) return;

            var query = _allTechCards.AsEnumerable();

            var searchText = SearchBox?.Text?.ToLower();
            if (!string.IsNullOrEmpty(searchText))
                query = query.Where(tc => (tc.ProductName?.ToLower().Contains(searchText) ?? false) ||
                                         (tc.Version?.Contains(searchText) ?? false));

            if (StatusFilter?.SelectedItem is ComboBoxItem statusItem && statusItem.Content?.ToString() != "Все статусы")
            {
                var statusFilter = statusItem.Content?.ToString();
                if (!string.IsNullOrEmpty(statusFilter))
                    query = query.Where(tc => tc.Status == statusFilter);
            }

            TechCardsGrid.ItemsSource = query.ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private async void NewTechCard_Click(object sender, RoutedEventArgs e)
        {
            var window = new TechCardWindow(_apiClient, null);
            if (window.ShowDialog() == true)
                await LoadTechCards();
        }

        private async void TechCardsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TechCardsGrid.SelectedItem is TechCard techCard)
            {
                var window = new TechCardWindow(_apiClient, techCard);
                window.ShowDialog();
                await LoadTechCards();
                TechCardsGrid.SelectedItem = null;
            }
        }

        private async void EditTechCard_Click(object sender, RoutedEventArgs e)
        {
            var techCard = (sender as Button)?.Tag as TechCard;
            if (techCard != null)
            {
                var window = new TechCardWindow(_apiClient, techCard);
                if (window.ShowDialog() == true)
                    await LoadTechCards();
            }
        }

        private async void ApproveTechCard_Click(object sender, RoutedEventArgs e)
        {
            var techCard = (sender as Button)?.Tag as TechCard;
            if (techCard != null)
            {
                if (MessageBox.Show($"Утвердить техкарту для {techCard.ProductName} v{techCard.Version}?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _apiClient.ApproveTechCardAsync(techCard.Id);
                    await LoadTechCards();
                }
            }
        }

        private async void ArchiveTechCard_Click(object sender, RoutedEventArgs e)
        {
            var techCard = (sender as Button)?.Tag as TechCard;
            if (techCard != null)
            {
                if (MessageBox.Show($"Архивировать техкарту {techCard.ProductName} v{techCard.Version}?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _apiClient.ArchiveTechCardAsync(techCard.Id);
                    await LoadTechCards();
                }
            }
        }
    }
}