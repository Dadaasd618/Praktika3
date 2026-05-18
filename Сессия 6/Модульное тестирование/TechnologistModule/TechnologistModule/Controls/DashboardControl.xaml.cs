using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using TechnologistModule.Models;
using TechnologistModule.Services;

namespace TechnologistModule.Controls
{
    public partial class DashboardControl : UserControl
    {
        private readonly ApiClient _apiClient;

        public DashboardControl(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;
            this.Loaded += async (s, e) => await LoadDashboard();
        }

        private async Task LoadDashboard()
        {
            try
            {
                var data = await _apiClient.GetDashboardDataAsync();

                ActiveProductsCount.Text = data.ActiveProducts.ToString();
                ActiveRecipesCount.Text = data.ActiveRecipes.ToString();
                ActiveTechCardsCount.Text = data.ActiveTechCards.ToString();
                OrdersInProgressCount.Text = data.OrdersInProgress.ToString();
                BatchesInProgressCount.Text = data.BatchesInProgress.ToString();
                BatchesWithDeviationsCount.Text = data.BatchesWithDeviations.ToString();
                BatchesAwaitingLabCount.Text = data.BatchesAwaitingLab.ToString();

                RecentEventsList.ItemsSource = data.RecentEvents;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }
}