using System;
using System.Threading.Tasks;
using System.Windows;
using TechnologistModule.Models;
using TechnologistModule.Services;
using TechnologistModule.Views;
namespace TechnologistModule.Views;

public partial class ProductCardWindow : Window
{
    private readonly ApiClient _apiClient;
    private readonly Product _product;
    private readonly bool _isEditMode;

    public ProductCardWindow(ApiClient apiClient, Product product = null)
    {
        InitializeComponent();
        _apiClient = apiClient;
        _product = product ?? new Product();
        _isEditMode = product != null && product.Id > 0;

        Title = _isEditMode ? "✏️ Редактирование продукта" : "➕ Новый продукт";
        TitleText.Text = _isEditMode ? "✏️ Редактирование продукта" : "📦 Новый продукт";
        SaveButton.Content = _isEditMode ? "Обновить" : "Сохранить";

        if (_isEditMode)
        {
            CodeBox.Text = _product.Code;
            NameBox.Text = _product.Name;
            TypeBox.Text = _product.Type;
            FormBox.Text = _product.Form;
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CodeBox.Text))
        {
            MessageBox.Show("Введите код продукта!");
            return;
        }
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show("Введите наименование!");
            return;
        }

        _product.Code = CodeBox.Text.Trim();
        _product.Name = NameBox.Text.Trim();
        _product.Type = TypeBox.Text.Trim();
        _product.Form = FormBox.Text.Trim();

        SaveButton.IsEnabled = false;
        SaveButton.Content = "Сохранение...";

        bool success = _isEditMode
            ? await _apiClient.UpdateProductAsync(_product.Id, _product) != null
            : await _apiClient.CreateProductAsync(_product) != null;

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