using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using OperatorModule.Services;
using OperatorModule.Views;

namespace OperatorModule.Views
{
    public partial class MainWindow : Window
    {
        private readonly ApiClient _apiClient;
        private System.Timers.Timer _notificationTimer;

        public MainWindow(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;

            if (App.CurrentUser != null)
            {
                LoadUserInfo();
                LoadUserPhoto();
                LoadCurrentShift();
            }

            if (ContentArea != null)
            {
                ContentArea.Content = new ActiveBatchesView(_apiClient);
            }

            StartNotificationMonitoring();
        }

        private void LoadUserInfo()
        {
            if (App.CurrentUser != null)
            {
                UserFullName.Text = App.CurrentUser.FullName ?? "Неизвестно";
                UserRole.Text = App.CurrentUser.Role == "operator" ? "Аппаратчик" : (App.CurrentUser.Role ?? "Пользователь");
            }
        }

        private void LoadCurrentShift()
        {
            int hour = DateTime.Now.Hour;
            string shiftText = hour switch
            {
                >= 6 and < 14 => "Смена: 1 (дневная)",
                >= 14 and < 22 => "Смена: 2 (вечерняя)",
                _ => "Смена: 3 (ночная)"
            };
            UserShift.Text = shiftText;
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки фото: {ex.Message}");
            }
        }

        private async void Avatar_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var dialog = new OpenFileDialog();
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

        private void StartNotificationMonitoring()
        {
            _notificationTimer = new System.Timers.Timer(10000);
            _notificationTimer.Elapsed += async (s, e) =>
            {
                Dispatcher.Invoke(async () => await LoadNotificationsCount());
            };
            _notificationTimer.Start();
        }

        private async Task LoadNotificationsCount()
        {
            try
            {
                var unreadCount = await _apiClient.GetUnreadNotificationsCountAsync();
                if (unreadCount > 0)
                {
                    NotificationBadge.Visibility = Visibility.Visible;
                    NotificationCount.Text = unreadCount.ToString();
                }
                else
                {
                    NotificationBadge.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки уведомлений: {ex.Message}");
            }
        }

        private async void NotificationsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ContentArea != null)
            {
                ContentArea.Content = new NotificationView(_apiClient);

                // Сбрасываем счетчик
                NotificationBadge.Visibility = Visibility.Collapsed;
                NotificationCount.Text = "0";
            }
        }

        private void ActiveBatchesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ContentArea != null)
            {
                ContentArea.Content = new ActiveBatchesView(_apiClient);
            }
        }

        private void BatchProgramButton_Click(object sender, RoutedEventArgs e)
        {
            if (ContentArea != null)
            {
                ContentArea.Content = new BatchProgramView(_apiClient);
            }
        }

        private void ExtruderLiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ContentArea != null)
            {
                ContentArea.Content = new ExtruderLiveView(_apiClient);
            }
        }

        private void BatchJournalButton_Click(object sender, RoutedEventArgs e)
        {
            if (ContentArea != null)
            {
                ContentArea.Content = new BatchJournalView(_apiClient);
            }
        }

        private void ReportProblemButton_Click(object sender, RoutedEventArgs e)
        {
            if (ContentArea != null)
            {
                ContentArea.Content = new ReportProblemView(_apiClient);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _notificationTimer?.Stop();
            _notificationTimer?.Dispose();

            var login = new LoginWindow();
            login.Show();
            Close();
        }
    }
}