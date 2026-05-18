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
    public partial class TechCardWindow : Window
    {
        private readonly ApiClient _apiClient;
        private readonly TechCard _techCard;
        private readonly bool _isEditMode;
        private ObservableCollection<Product> _products;
        private ObservableCollection<TechStep> _steps;

        public TechCardWindow(ApiClient apiClient, TechCard techCard = null)
        {
            InitializeComponent();
            _apiClient = apiClient;
            _techCard = techCard ?? new TechCard { Steps = new System.Collections.Generic.List<TechStep>() };
            _isEditMode = techCard != null && techCard.Id > 0;
            _steps = new ObservableCollection<TechStep>(_techCard.Steps ?? new System.Collections.Generic.List<TechStep>());

            Title = _isEditMode ? "✏️ Редактирование техкарты" : "➕ Новая техкарта";
            TitleText.Text = _isEditMode ? $"✏️ Редактирование - v{_techCard.Version}" : "⚙️ Новая техкарта";
            SaveButton.Content = _isEditMode ? "Обновить" : "Сохранить";
            StepsGrid.ItemsSource = _steps;

            Loaded += async (s, e) => await LoadProducts();
        }

        private async Task LoadProducts()
        {
            var productsList = await _apiClient.GetProductsAsync();
            _products = new ObservableCollection<Product>(productsList);
            ProductCombo.ItemsSource = _products;

            if (_isEditMode)
            {
                ProductCombo.SelectedValue = _techCard.ProductId;
                VersionBox.Text = _techCard.Version;
            }
        }

        private void AddStep_Click(object sender, RoutedEventArgs e)
        {
            _steps.Add(new TechStep
            {
                StepOrder = _steps.Count + 1,
                Name = "Новый шаг",
                StepType = "operation",
                PlannedMin = 0,
                PlannedMax = 0,
                Unit = "",
                IsMandatory = true,
                Instruction = ""
            });
        }

        private void RemoveStep_Click(object sender, RoutedEventArgs e)
        {
            var step = (sender as Button)?.Tag as TechStep;
            if (step != null)
            {
                _steps.Remove(step);
                for (int i = 0; i < _steps.Count; i++)
                    _steps[i].StepOrder = i + 1;
            }
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
            if (_steps.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один шаг!");
                return;
            }

            _techCard.ProductId = (long)ProductCombo.SelectedValue;
            _techCard.Version = VersionBox.Text;
            _techCard.Steps = _steps.ToList();

            SaveButton.IsEnabled = false;
            SaveButton.Content = "Сохранение...";

            bool success = _isEditMode
                ? await _apiClient.UpdateTechCardAsync(_techCard.Id, _techCard) != null
                : await _apiClient.CreateTechCardAsync(_techCard) != null;

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