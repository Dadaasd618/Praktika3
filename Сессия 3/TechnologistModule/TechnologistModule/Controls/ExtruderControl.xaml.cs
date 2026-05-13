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
    public partial class ExtruderControl : UserControl
    {
        private readonly ApiClient _apiClient;
        private ObservableCollection<ExtruderProgram> _allPrograms = new ObservableCollection<ExtruderProgram>();

        public ExtruderControl(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;
            this.Loaded += async (s, e) => await LoadPrograms();
        }

        private async Task LoadPrograms()
        {
            try
            {
                var programs = await _apiClient.GetExtruderProgramsAsync();
                _allPrograms.Clear();
                foreach (var p in programs)
                    _allPrograms.Add(p);
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            var query = _allPrograms.AsEnumerable();
            var searchText = SearchBox.Text?.ToLower();
            if (!string.IsNullOrEmpty(searchText))
                query = query.Where(p => p.Name.ToLower().Contains(searchText));
            ProgramsGrid.ItemsSource = query.ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

        private async void NewProgram_Click(object sender, RoutedEventArgs e)
        {
            var window = new ExtruderProgramWindow(_apiClient, null);
            if (window.ShowDialog() == true)
                await LoadPrograms();
        }

        private async void ProgramsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProgramsGrid.SelectedItem is ExtruderProgram program)
            {
                var window = new ExtruderProgramWindow(_apiClient, program);
                window.ShowDialog();
                await LoadPrograms();
                ProgramsGrid.SelectedItem = null;
            }
        }

        private async void EditProgram_Click(object sender, RoutedEventArgs e)
        {
            var program = (sender as Button)?.Tag as ExtruderProgram;
            if (program != null)
            {
                var window = new ExtruderProgramWindow(_apiClient, program);
                if (window.ShowDialog() == true)
                    await LoadPrograms();
            }
        }

        private async void DeleteProgram_Click(object sender, RoutedEventArgs e)
        {
            var program = (sender as Button)?.Tag as ExtruderProgram;
            if (program != null)
            {
                if (MessageBox.Show($"Удалить программу '{program.Name}'?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _apiClient.DeleteExtruderProgramAsync(program.Id);
                    await LoadPrograms();
                }
            }
        }
    }
}