using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using TechnologistModule.Models;
using TechnologistModule.Services;

namespace TechnologistModule.Views
{
    public partial class BatchCardWindow : Window
    {
        private readonly ApiClient _apiClient;
        private readonly ProductionBatch _batch;
        private readonly bool _isEditMode;
        private ObservableCollection<ProductionOrder> _orders;

        public BatchCardWindow(ApiClient apiClient, ProductionBatch batch = null)
        {
            InitializeComponent();
            _apiClient = apiClient;
            _batch = batch ?? new ProductionBatch();
            _isEditMode = batch != null && batch.Id > 0;

            Title = _isEditMode ? "✏️ Редактирование партии" : "➕ Новая партия";
            TitleText.Text = _isEditMode ? "✏️ Редактирование партии" : "🏭 Новая партия";
            SaveButton.Content = _isEditMode ? "Обновить" : "Сохранить";

            Loaded += async (s, e) => await LoadOrders();

            if (_isEditMode)
            {
                BatchNumberBox.Text = _batch.BatchNumber;
                if (_batch.OrderId > 0) OrderCombo.SelectedValue = _batch.OrderId;
            }
        }

        private async Task LoadOrders()
        {
            var ordersList = await _apiClient.GetOrdersAsync();
            _orders = new ObservableCollection<ProductionOrder>(ordersList);
            OrderCombo.ItemsSource = _orders;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (OrderCombo.SelectedValue == null)
            {
                MessageBox.Show("Выберите заказ!");
                return;
            }
            if (string.IsNullOrWhiteSpace(BatchNumberBox.Text))
            {
                MessageBox.Show("Введите номер партии!");
                return;
            }

            _batch.OrderId = (long)OrderCombo.SelectedValue;
            _batch.BatchNumber = BatchNumberBox.Text.Trim();

            SaveButton.IsEnabled = false;
            SaveButton.Content = "Сохранение...";

            bool success = _isEditMode
                ? await _apiClient.UpdateBatchAsync(_batch.Id, _batch.Status, _batch.ActualQuantityKg, _batch.EndTime)
                : await _apiClient.CreateBatchAsync(_batch.OrderId, _batch.BatchNumber) != null;

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