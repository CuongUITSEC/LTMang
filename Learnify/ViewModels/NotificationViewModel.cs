using System.Collections.ObjectModel;
using Learnify.Models;
using System.Windows.Input;
using Learnify.Commands;
using System.Linq;

namespace Learnify.ViewModels
{
    public class NotificationViewModel : ViewModelBase
    {
        private bool _isPanelVisible;
        public bool IsPanelVisible
        {
            get => _isPanelVisible;
            set
            {
                _isPanelVisible = value;
                OnPropertyChanged(nameof(IsPanelVisible));

                // Tự động đánh dấu đã đọc khi mở panel
                if (value) MarkAllAsRead();
            }
        }

        public ObservableCollection<Notification> Notifications { get; }
        public int UnreadCount => Notifications.Count(n => !n.IsRead);

        public ICommand ToggleNotificationCommand { get; }
        public ICommand MarkAsReadCommand { get; }
        public ICommand ClearAllCommand { get; }

        public NotificationViewModel()
        {
            Notifications = new ObservableCollection<Notification>();
            LoadSampleNotifications();

            ToggleNotificationCommand = new ViewModelCommand(ToggleNotification);
            MarkAsReadCommand = new RelayCommand<Notification>(MarkAsRead);
            ClearAllCommand = new ViewModelCommand(ClearAllNotifications);
        }

        private void LoadSampleNotifications()
        {
            Notifications.Add(new Notification
            {
                Title = "Welcome",
                Message = "Welcome to Learnify!",
                Time = "Just now",
                IsRead = true
            });

            Notifications.Add(new Notification
            {
                Title = "Calendar",
                Message = "You have a schedule!",
                Time = "5 minutes ago",
                IsRead = false
            });
        }

        public void AddNotification(Notification notification)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Notifications.Insert(0, notification);
                OnPropertyChanged(nameof(UnreadCount));

                // Hiển thị panel nếu có thông báo mới
                if (!notification.IsRead) IsPanelVisible = true;
            });
        }

        private void ToggleNotification(object parameter)
        {
            IsPanelVisible = !IsPanelVisible;
        }

        private void MarkAsRead(Notification notification)
        {
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                OnPropertyChanged(nameof(UnreadCount));
            }
        }

        private void MarkAllAsRead()
        {
            foreach (var notification in Notifications.Where(n => !n.IsRead))
            {
                notification.IsRead = true;
            }
            OnPropertyChanged(nameof(UnreadCount));
            OnPropertyChanged(nameof(Notifications));
        }

        private void ClearAllNotifications(object parameter)
        {
            Notifications.Clear();
            OnPropertyChanged(nameof(UnreadCount));
        }
    }
}