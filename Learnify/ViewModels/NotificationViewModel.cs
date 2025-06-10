using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Learnify.Commands;
using Learnify.Models;
using Learnify.Services;
using System.Threading.Tasks;

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
        public ICommand ClearAllCommand { get; }        private readonly FirebaseService _firebaseService = new FirebaseService();
        private readonly object _mainViewModel; // Tham chiếu để reload FriendsList

        private System.Threading.CancellationTokenSource _pollingCts;
        private HashSet<string> _notifiedFriendRequests = new HashSet<string>();

        public NotificationViewModel(object mainViewModel = null)
        {
            _mainViewModel = mainViewModel;
            Notifications = new ObservableCollection<Notification>();
            // Không load thông báo mẫu tĩnh ở đây nữa
            ToggleNotificationCommand = new ViewModelCommand(ToggleNotification);
            MarkAsReadCommand = new RelayCommand<Notification>(MarkAsRead);
            ClearAllCommand = new ViewModelCommand(ClearAllNotifications);
        }

        public void AddNotification(Notification notification)
        {
            System.Diagnostics.Debug.WriteLine($"[Notification] AddNotification called: {notification?.Title} - {notification?.Message}");
            App.Current.Dispatcher.Invoke(() =>
            {
                Notifications.Insert(0, notification);
                OnPropertyChanged(nameof(UnreadCount));
                System.Diagnostics.Debug.WriteLine($"[Notification] Notifications count: {Notifications.Count}");
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

        public void AddFriendRequestNotification(string senderId, string senderName, string requestId)
        {
            System.Diagnostics.Debug.WriteLine($"[Notification] AddFriendRequestNotification called: senderId={senderId}, senderName={senderName}, requestId={requestId}");
            var notification = new FriendRequestNotification
            {
                Title = "Lời mời kết bạn",
                Message = $"{senderName} đã gửi cho bạn một lời mời kết bạn.",
                Time = DateTime.Now.ToString("HH:mm dd/MM/yyyy"),
                IsRead = false,
                SenderId = senderId,
                SenderName = senderName,
                AcceptCommand = new RelayCommand<object>(async _ => await AcceptFriendRequest(senderId, requestId)),
                DeclineCommand = new RelayCommand<object>(async _ => await DeclineFriendRequest(requestId))
            };
            AddNotification(notification);
        }        private async Task AcceptFriendRequest(string senderId, string requestId)
        {
            var receiverId = AuthService.GetUserId();
            var result = await _firebaseService.AcceptFriendRequestAsync(senderId, receiverId, requestId);
            RemoveNotificationByRequestId(requestId);
            
            // Reload FriendsList sau khi chấp nhận kết bạn thành công
            if (result && _mainViewModel != null)
            {
                // Sử dụng reflection để gọi ReloadFriendsListAsync
                var method = _mainViewModel.GetType().GetMethod("ReloadFriendsListAsync");
                if (method != null)
                {
                    var task = (Task)method.Invoke(_mainViewModel, null);
                    await task;
                }
            }
        }

        private async Task DeclineFriendRequest(string requestId)
        {
            var receiverId = AuthService.GetUserId();
            var result = await _firebaseService.DeclineFriendRequestAsync(receiverId, requestId);
            RemoveNotificationByRequestId(requestId);
        }

        private void RemoveNotificationByRequestId(string requestId)
        {
            var noti = Notifications.OfType<FriendRequestNotification>().FirstOrDefault(n => n.SenderId + "_" == requestId.Substring(0, n.SenderId.Length + 1));
            if (noti != null)
            {
                App.Current.Dispatcher.Invoke(() => Notifications.Remove(noti));
            }
        }

        public void StartFriendRequestPolling()
        {
            _pollingCts = new System.Threading.CancellationTokenSource();
            Task.Run(() => PollFriendRequestsAsync(_pollingCts.Token));
        }

        public void StopFriendRequestPolling()
        {
            _pollingCts?.Cancel();
        }

        private string GetAuthenticatedUrl(string path)
        {
            var token = AuthService.GetToken();
            return $"https://learnify-b5cf3-default-rtdb.asia-southeast1.firebasedatabase.app/{path}?auth={token}";
        }        private async Task PollFriendRequestsAsync(System.Threading.CancellationToken token)
        {
            var userId = AuthService.GetUserId();
            System.Diagnostics.Debug.WriteLine($"[Notification] Start polling friend requests for user: {userId}");
            System.Diagnostics.Debug.WriteLine($"[Notification] Current token: {AuthService.GetToken()?.Substring(0, 50)}...");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var url = GetAuthenticatedUrl($"friendRequests/{userId}.json");
                    System.Diagnostics.Debug.WriteLine($"[Notification] Polling URL: {url}");                    // Sử dụng HttpClient static từ FirebaseService
                    var response = await Learnify.Services.FirebaseService.SharedHttpClient.GetAsync(url);
                    System.Diagnostics.Debug.WriteLine($"[Notification] Polling response status: {response.StatusCode}");
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine("[Notification] Token expired or invalid, stopping polling");
                        break; // Dừng polling khi token hết hạn
                    }
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"[Notification] Polling response content: {content}");
                        var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(content);
                        if (dict != null)
                        {
                            foreach (var kv in dict)
                            {
                                var req = kv.Value;
                                string reqId = kv.Key;
                                string senderId = req.senderId;
                                string senderName = req.senderName;
                                string status = req.status;                                System.Diagnostics.Debug.WriteLine($"[Notification] Found request: reqId={reqId}, senderId={senderId}, senderName={senderName}, status={status}");
                                if (status == "Pending" && !_notifiedFriendRequests.Contains(reqId))
                                {
                                    _notifiedFriendRequests.Add(reqId);
                                    System.Diagnostics.Debug.WriteLine($"[Notification] Adding notification for Pending request: {reqId}");
                                    App.Current.Dispatcher.Invoke(() =>
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[Notification] AddFriendRequestNotification: senderId={senderId}, senderName={senderName}, requestId={reqId}");
                                        AddFriendRequestNotification(senderId, senderName, reqId);
                                    });
                                }
                                else if (status == "Pending")
                                {
                                    System.Diagnostics.Debug.WriteLine($"[Notification] Request {reqId} already notified, skipping");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[Notification] Request {reqId} has status {status}, skipping notification");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Notification] PollFriendRequestsAsync error: {ex.Message}");
                }
                await Task.Delay(5000, token); // Poll every 5 seconds
            }
        }
    }
}