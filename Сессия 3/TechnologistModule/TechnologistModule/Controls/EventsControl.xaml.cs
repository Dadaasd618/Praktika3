using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using TechnologistModule.Models;
using TechnologistModule.Services;

namespace TechnologistModule.Controls
{
    public partial class EventsControl : UserControl
    {
        private readonly ApiClient _apiClient;

        public EventsControl(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;
            this.Loaded += async (s, e) => await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                var deviations = await _apiClient.GetDeviationsAsync();
                DeviationsGrid.ItemsSource = deviations;

                var events = await _apiClient.GetEventsAsync();
                EventsGrid.ItemsSource = events;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }
}