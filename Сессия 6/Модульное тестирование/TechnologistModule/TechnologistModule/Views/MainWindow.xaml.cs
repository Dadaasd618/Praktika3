using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TechnologistModule.Controls;
using TechnologistModule.Models;
using TechnologistModule.Services;
using TechnologistModule.Helpers;
namespace TechnologistModule.Views
{
    public partial class MainWindow : Window
    {
        private readonly ApiClient _apiClient;

        public MainWindow(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;
            LoadUserInfo();
            LoadUserPhoto();

            // По умолчанию показываем Dashboard
            ContentArea.Content = new DashboardControl(_apiClient);
        }

        private void LoadUserInfo()
        {
            if (App.CurrentUser != null)
            {
                UserFullName.Text = App.CurrentUser.FullName;
                UserRole.Text = App.CurrentUser.Role;
                UserDepartment.Text = App.CurrentUser.Department;
            }
        }

        private async void LoadUserPhoto()
        {
            try
            {
                var photo = await _apiClient.GetUserPhotoAsync(App.CurrentUser.Id);
                if (photo != null)
                {
                    UserAvatar.Source = photo;
                }
                else
                {
                    // Аватарка по умолчанию (иконка пользователя)
                    UserAvatar.Source = null;
                    // Можно поставить заглушку
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки фото: {ex.Message}");
            }
        }

        private async void Avatar_Click(object sender, MouseButtonEventArgs e)
        {
            // Открываем диалог выбора фото
            var dialog = new OpenFileDialog();
            dialog.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp";
            dialog.Title = "Выберите фото профиля";

            if (dialog.ShowDialog() == true)
            {
                var success = await _apiClient.UploadUserPhotoAsync(App.CurrentUser.Id, dialog.FileName);
                if (success)
                {
                    // Обновляем фото
                    LoadUserPhoto();
                    MessageBox.Show("Фото профиля обновлено!", "Успех",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Ошибка при загрузке фото", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new DashboardControl(_apiClient);
        }

        private void ProductsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new ProductsControl(_apiClient);
        }

        private void RecipesButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new RecipesControl(_apiClient);
        }

        private void TechCardsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new TechCardsControl(_apiClient);
        }

        private void OrdersButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new OrdersControl(_apiClient);
        }

        private void BatchesButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new BatchesControl(_apiClient);
        }

        private void ExtruderButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new ExtruderControl(_apiClient);
        }

        private void EventsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new EventsControl(_apiClient);
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new ReportsControl(_apiClient);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            Close();
        }
    }
}