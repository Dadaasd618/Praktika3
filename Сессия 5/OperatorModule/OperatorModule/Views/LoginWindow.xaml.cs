using System;
using System.Threading.Tasks;
using System.Windows;
using OperatorModule.Services;

namespace OperatorModule.Views
{
    public partial class LoginWindow : Window
    {
        private readonly ApiClient _apiClient;

        public LoginWindow()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Введите логин и пароль");
                return;
            }

            LoginButton.IsEnabled = false;
            LoginButton.Content = "Вход...";

            try
            {
                var success = await _apiClient.LoginAsync(username, password);
                if (success && App.CurrentUser != null)
                {
                    var mainWindow = new MainWindow(_apiClient);
                    mainWindow.Show();
                    Close();
                }
                else
                {
                    ShowError("Неверный логин или пароль");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка подключения: {ex.Message}");
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Content = "ВОЙТИ";
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}