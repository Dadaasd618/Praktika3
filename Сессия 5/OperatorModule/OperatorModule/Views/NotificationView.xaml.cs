using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OperatorModule.Models;
using OperatorModule.Services;

namespace OperatorModule.Views
{
    public partial class NotificationView : UserControl
    {
        private readonly ApiClient _apiClient;
        private ObservableCollection<NotificationViewModel> _notifications = new ObservableCollection<NotificationViewModel>();

        public NotificationView(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;
            this.Loaded += async (s, e) => await LoadNotifications();
        }

        private async Task LoadNotifications()
        {
            try
            {
                var notifications = await _apiClient.GetNotificationsAsync();
                _notifications.Clear();

                foreach (var n in notifications)
                {
                    _notifications.Add(new NotificationViewModel(n));
                }

                NotificationsList.ItemsSource = _notifications;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private async void MarkAllAsRead_Click(object sender, RoutedEventArgs e)
        {
            await _apiClient.MarkAllNotificationsAsReadAsync();
            await LoadNotifications();
        }

        private async void Notification_Click(object sender, MouseButtonEventArgs e)
        {
            var notification = (sender as Border)?.DataContext as NotificationViewModel;
            if (notification != null && !notification.IsRead)
            {
                await _apiClient.MarkNotificationAsReadAsync(notification.Id);
                await LoadNotifications();
            }
        }
    }

    public class NotificationViewModel : Notification
    {
        public NotificationViewModel(Notification notification)
        {
            Id = notification.Id;
            Title = notification.Title;
            Message = notification.Message;
            Type = notification.Type;
            CreatedAt = notification.CreatedAt;
            IsRead = notification.IsRead;
            BatchId = notification.BatchId;
            BatchNumber = notification.BatchNumber;
        }

        public string Icon => Type switch
        {
            "warning" => "⚠️",
            "error" => "❌",
            "success" => "✅",
            _ => "ℹ️"
        };

        public string Status => IsRead ? "Прочитано" : "Новое";
        public string StatusColor => IsRead ? "#94A3B8" : "#3B82F6";
        public string BackgroundColor => IsRead ? "#F8FAFC" : "#EFF6FF";
        public string BorderColor => IsRead ? "#E2E8F0" : "#BFDBFE";
    }
}