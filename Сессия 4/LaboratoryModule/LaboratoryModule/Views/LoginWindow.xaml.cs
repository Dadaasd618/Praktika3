using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LaboratoryModule.Services;

namespace LaboratoryModule.Views
{
    public partial class LoginWindow : Window
    {
        private readonly ApiClient _apiClient;
        private readonly CaptchaService _captchaService;
        private readonly CaptchaService _regCaptchaService;

        public LoginWindow()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            _captchaService = new CaptchaService();
            _regCaptchaService = new CaptchaService();

            LoginTab.Checked += (s, e) => SwitchToLogin();
            RegisterTab.Checked += (s, e) => SwitchToRegister();

            Loaded += (s, e) =>
            {
                LoadCaptcha();
                LoadRegCaptcha();
            };
        }

        private void SwitchToLogin()
        {
            LoginPanel.Visibility = Visibility.Visible;
            RegisterPanel.Visibility = Visibility.Collapsed;
            ActionButton.Content = "ВОЙТИ";
            ErrorText.Visibility = Visibility.Collapsed;

            LoginTabBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
            LoginTab.Foreground = Brushes.White;
            RegisterTabBorder.Background = Brushes.Transparent;
            RegisterTabBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
            RegisterTab.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
        }

        private void SwitchToRegister()
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            RegisterPanel.Visibility = Visibility.Visible;
            ErrorText.Visibility = Visibility.Collapsed;

            RegisterTabBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
            RegisterTab.Foreground = Brushes.White;
            LoginTabBorder.Background = Brushes.Transparent;
            LoginTabBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
            LoginTab.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
        }

        private void LoadCaptcha()
        {
            try
            {
                var captchaImage = _captchaService.GenerateCaptchaImage();
                CaptchaImage.Source = captchaImage;
                CaptchaCodeBox.Text = "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка CAPTCHA: {ex.Message}");
            }
        }

        private void LoadRegCaptcha()
        {
            try
            {
                var captchaImage = _regCaptchaService.GenerateCaptchaImage();
                RegCaptchaImage.Source = captchaImage;
                RegCaptchaCodeBox.Text = "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка CAPTCHA: {ex.Message}");
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

        private void RefreshRegCaptcha_Click(object sender, MouseButtonEventArgs e)
        {
            LoadRegCaptcha();
        }

        private void RefreshRegCaptchaButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRegCaptcha();
        }

        private async void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            var captchaCode = CaptchaCodeBox.Text.Trim();
            if (string.IsNullOrEmpty(captchaCode))
            {
                ShowError("Введите код с картинки");
                return;
            }

            if (!_captchaService.VerifyCode(captchaCode))
            {
                ShowError("Неверный код CAPTCHA");
                LoadCaptcha();
                return;
            }

            ActionButton.IsEnabled = false;
            ActionButton.Content = "Проверка...";

            try
            {
                var username = LoginUsernameBox.Text.Trim();
                var password = LoginPasswordBox.Password;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ShowError("Введите логин и пароль");
                    return;
                }

                var success = await _apiClient.LoginAsync(username, password);
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
                ActionButton.IsEnabled = true;
                ActionButton.Content = "ВОЙТИ";
            }
        }

        private async void RegisterActionButton_Click(object sender, RoutedEventArgs e)
        {
            var captchaCode = RegCaptchaCodeBox.Text.Trim();
            if (string.IsNullOrEmpty(captchaCode))
            {
                ShowError("Введите код с картинки");
                return;
            }

            if (!_regCaptchaService.VerifyCode(captchaCode))
            {
                ShowError("Неверный код CAPTCHA");
                LoadRegCaptcha();
                return;
            }

            RegisterActionButton.IsEnabled = false;
            RegisterActionButton.Content = "Регистрация...";

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

                var success = await _apiClient.RegisterAsync(username, password, fullName, role, department);
                if (success)
                {
                    MessageBox.Show("Регистрация успешна! Теперь вы можете войти.", "Успех",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    LoginTab.IsChecked = true;
                    SwitchToLogin();
                    LoginUsernameBox.Text = username;
                    LoginPasswordBox.Password = "";
                    RegUsernameBox.Text = "";
                    RegPasswordBox.Password = "";
                    RegFullNameBox.Text = "";
                    RegDepartmentBox.Text = "";
                    LoadCaptcha();
                    LoadRegCaptcha();
                }
                else
                {
                    ShowError("Ошибка регистрации. Возможно, логин уже занят");
                    LoadRegCaptcha();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
                LoadRegCaptcha();
            }
            finally
            {
                RegisterActionButton.IsEnabled = true;
                RegisterActionButton.Content = "ЗАРЕГИСТРИРОВАТЬСЯ";
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}