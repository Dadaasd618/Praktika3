using LaboratoryModule.Services;
using LaboratoryModule.Views;
using System.Windows;
using System.Windows.Input;

namespace LaboratoryModule.Views
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
            ContentArea.Content = new RawMaterialBatchesView(_apiClient);
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
                // Получаем фото из API
                var photo = await _apiClient.GetUserPhotoAsync(App.CurrentUser.Id);
                if (photo != null)
                {
                    UserAvatar.Source = photo;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки фото: {ex.Message}");
            }
        }

        private async void Avatar_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp";
            dialog.Title = "Выберите фото профиля";

            if (dialog.ShowDialog() == true)
            {
                var success = await _apiClient.UploadUserPhotoAsync(App.CurrentUser.Id, dialog.FileName);
                if (success)
                {
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

        
        private void RawMaterialBatchesButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new RawMaterialBatchesView(_apiClient);
        }

        private void ProductBatchesButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new ProductBatchesView(_apiClient);
        }

        private void ProtocolsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new ProtocolsView(_apiClient);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            Close();
        }
    }
}