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
    public partial class ProtocolsView : UserControl
    {
        private readonly ApiClient _apiClient;
        private ObservableCollection<LabTest> _allProtocols = new ObservableCollection<LabTest>();

        public ProtocolsView(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;
            this.Loaded += async (s, e) => await LoadProtocols();
        }

        private async Task LoadProtocols()
        {
            try
            {
                var tests = await _apiClient.GetCompletedLabTestsAsync();
                if (tests == null || tests.Count == 0) return;

                var users = await _apiClient.GetUsersAsync();
                var userDict = users?.ToDictionary(u => u.Id, u => u.FullName) ?? new Dictionary<long, string>();

                var rawBatches = await _apiClient.GetRawMaterialBatchesAsync();
                var prodBatches = await _apiClient.GetProductionBatchesAsync();
                var products = await _apiClient.GetProductsAsync();
                var productDict = products?.ToDictionary(p => p.Id, p => p.Name) ?? new Dictionary<long, string>();

                var rawDict = rawBatches?.ToDictionary(b => b.Id, b => b.BatchNumber) ?? new Dictionary<long, string>();
                var prodDict = prodBatches?.ToDictionary(b => b.Id, b =>
                {
                    var productName = productDict.ContainsKey(b.ProductId) ? productDict[b.ProductId] : "Неизвестно";
                    return $"{b.BatchNumber} ({productName})";
                }) ?? new Dictionary<long, string>();

                _allProtocols.Clear();
                foreach (var t in tests)
                {
                    t.TestedByName = t.TestedBy.HasValue && userDict.ContainsKey(t.TestedBy.Value)
                        ? userDict[t.TestedBy.Value] : "Неизвестно";

                    t.ObjectName = t.ObjectType == "raw_material" && rawDict.ContainsKey(t.ObjectId)
                        ? rawDict[t.ObjectId]
                        : (t.ObjectType == "product" && prodDict.ContainsKey(t.ObjectId)
                            ? prodDict[t.ObjectId]
                            : t.ObjectId.ToString());

                    _allProtocols.Add(t);
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
            if (_allProtocols == null || _allProtocols.Count == 0) return;

            var query = _allProtocols.AsEnumerable();

            var searchText = SearchBox?.Text?.ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(p => p.TestNumber.ToLower().Contains(searchText) ||
                                        (p.ObjectName?.ToLower().Contains(searchText) ?? false));
            }

            if (TypeFilter?.SelectedItem is ComboBoxItem typeItem)
            {
                var typeFilter = typeItem.Content?.ToString();
                if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "Все типы")
                {
                    query = query.Where(p => p.ObjectType == typeFilter);
                }
            }

            if (DecisionFilter?.SelectedItem is ComboBoxItem decisionItem)
            {
                var decisionFilter = decisionItem.Content?.ToString();
                if (!string.IsNullOrEmpty(decisionFilter) && decisionFilter != "Все решения")
                {
                    query = query.Where(p => p.Decision == decisionFilter);
                }
            }

            if (DateFrom?.SelectedDate != null)
                query = query.Where(p => p.TestedAt.HasValue && p.TestedAt.Value.Date >= DateFrom.SelectedDate.Value.Date);
            if (DateTo?.SelectedDate != null)
                query = query.Where(p => p.TestedAt.HasValue && p.TestedAt.Value.Date <= DateTo.SelectedDate.Value.Date);

            ProtocolsGrid.ItemsSource = query.ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void TypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DecisionFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void ProtocolsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProtocolsGrid?.SelectedItem is LabTest test)
            {
                var window = new ProtocolViewWindow(_apiClient, test);
                window.ShowDialog();
                ProtocolsGrid.SelectedItem = null;
            }
        }

        private void ViewProtocol_Click(object sender, RoutedEventArgs e)
        {
            var test = (sender as Button)?.Tag as LabTest;
            if (test != null)
            {
                var window = new ProtocolViewWindow(_apiClient, test);
                window.ShowDialog();
            }
        }
    }
}