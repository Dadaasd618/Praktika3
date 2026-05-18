using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TechnologistModule.Services;
using TechnologistModule.Helpers;

namespace TechnologistModule.Views
{
    public partial class LoginWindow : Window
    {
        private readonly ApiClient _apiClient;

        // Конструктор без параметров (нужен для XAML)
        public LoginWindow()
        {
            InitializeComponent();
            _apiClient = new ApiClient();

            LoginTab.Checked += (s, e) => SwitchToLogin();
            RegisterTab.Checked += (s, e) => SwitchToRegister();

            // Начальное состояние
            SwitchToLogin();

            Loaded += (s, e) => LoadCaptcha();
        }

        private void SwitchToLogin()
        {
            LoginPanel.Visibility = Visibility.Visible;
            RegisterPanel.Visibility = Visibility.Collapsed;
        }

        private void SwitchToRegister()
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            RegisterPanel.Visibility = Visibility.Visible;
        }

        private void LoadCaptcha()
        {
            try
            {
                var captchaImage = CaptchaGenerator.GenerateCaptchaImage();
                CaptchaImage.Source = captchaImage;
                CaptchaCodeBox.Text = "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка CAPTCHA: {ex.Message}");
                MessageBox.Show($"Ошибка генерации CAPTCHA: {ex.Message}");
            }
        }

        private void RefreshCaptcha_Click(object sender, MouseButtonEventArgs e)
        {
            LoadCaptcha();
        }

        private void RefreshCaptchaButton_Click(object sender, RoutedEventArgs e)
        {
            LoadCaptcha();
        }

        // Обработчик кнопки ВОЙТИ
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var captchaCode = CaptchaCodeBox.Text.Trim();
            if (string.IsNullOrEmpty(captchaCode))
            {
                ShowError("Введите код с картинки");
                return;
            }

            if (!CaptchaGenerator.VerifyCode(captchaCode))
            {
                ShowError("Неверный код CAPTCHA");
                LoadCaptcha();
                return;
            }

            LoginButton.IsEnabled = false;
            LoginButton.Content = "Вход...";

            try
            {
                var username = LoginUsernameBox.Text.Trim();
                var password = LoginPasswordBox.Password;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ShowError("Введите логин и пароль");
                    return;
                }

                var success = await _apiClient.LoginAsync(username, password, "");
                if (success)
                {
                    var mainWindow = new MainWindow(_apiClient);
                    mainWindow.Show();
                    Close();
                }
                else
                {
                    ShowError("Неверный логин или пароль");
                    LoadCaptcha();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
                LoadCaptcha();
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Content = "ВОЙТИ";
            }
        }

        // Обработчик кнопки ЗАРЕГИСТРИРОВАТЬСЯ
        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var captchaCode = CaptchaCodeBox.Text.Trim();
            if (string.IsNullOrEmpty(captchaCode))
            {
                ShowError("Введите код с картинки");
                return;
            }

            if (!CaptchaGenerator.VerifyCode(captchaCode))
            {
                ShowError("Неверный код CAPTCHA");
                LoadCaptcha();
                return;
            }

            RegisterButton.IsEnabled = false;
            RegisterButton.Content = "Регистрация...";

            try
            {
                var username = RegUsernameBox.Text.Trim();
                var password = RegPasswordBox.Password;
                var fullName = RegFullNameBox.Text.Trim();
                var role = (RegRoleBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                var department = RegDepartmentBox.Text.Trim();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) ||
                    string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(department))
                {
                    ShowError("Заполните все поля");
                    return;
                }

                var success = await _apiClient.RegisterAsync(username, password, fullName, role, department, "");
                if (success)
                {
                    MessageBox.Show("Регистрация успешна! Теперь вы можете войти.", "Успех",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    // Переключаемся на вкладку входа
                    LoginTab.IsChecked = true;
                    SwitchToLogin();
                    LoginUsernameBox.Text = username;
                    LoginPasswordBox.Password = "";
                    CaptchaCodeBox.Text = "";
                    LoadCaptcha();
                }
                else
                {
                    ShowError("Ошибка регистрации. Возможно, логин уже занят");
                    LoadCaptcha();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
                LoadCaptcha();
            }
            finally
            {
                RegisterButton.IsEnabled = true;
                RegisterButton.Content = "ЗАРЕГИСТРИРОВАТЬСЯ";
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;

            var timer = new System.Timers.Timer(5000);
            timer.Elapsed += (s, e) =>
            {
                Dispatcher.Invoke(() => ErrorText.Visibility = Visibility.Collapsed);
                timer.Stop();
            };
            timer.Start();
        }

        private void RegisterTab_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}