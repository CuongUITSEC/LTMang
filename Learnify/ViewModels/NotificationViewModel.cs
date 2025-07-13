using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Learnify.Models;
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
        public ICommand ClearAllCommand { get; }
        private readonly FirebaseService _firebaseService = new FirebaseService();
        private readonly MainViewModel _mainViewModel; // Tham chiếu để reload FriendsList

        private System.Threading.CancellationTokenSource _pollingCts;
        private HashSet<string> _notifiedFriendRequests = new HashSet<string>();

        public NotificationViewModel(MainViewModel mainViewModel = null)
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
            // System.Diagnostics.Debug.WriteLine($"[Notification] AddNotification called: {notification?.Title} - {notification?.Message}");
            App.Current.Dispatcher.Invoke(() =>
            {
                Notifications.Insert(0, notification);
                OnPropertyChanged(nameof(UnreadCount));
                // System.Diagnostics.Debug.WriteLine($"[Notification] Notifications count: {Notifications.Count}");

                // Hiển thị panel nếu có thông báo mới
                if (!notification.IsRead)
                {
                    IsPanelVisible = true;

                    // Phát âm thanh thông báo cho kết bạn thành công
                    if (notification is FriendAcceptedNotification)
                    {
                        try
                        {
                            System.Media.SystemSounds.Asterisk.Play();
                        }
                        catch
                        {
                            // Ignore sound errors
                        }
                    }
                }
            });
        }

        private void ToggleNotification(object parameter)
        {
            // Thông báo cho MainViewModel để ẩn/hiện notification panel
            if (_mainViewModel != null)
            {
                _mainViewModel.IsNotificationVisible = !_mainViewModel.IsNotificationVisible;
            }
            else
            {
                // Fallback nếu không có MainViewModel
                IsPanelVisible = !IsPanelVisible;
            }
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
            // Hiển thị dialog xác nhận trước khi xóa
            var result = System.Windows.MessageBox.Show(
                "Bạn có chắc chắn muốn xóa tất cả thông báo?",
                "Xác nhận xóa",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                Notifications.Clear();
                OnPropertyChanged(nameof(UnreadCount));
                // System.Diagnostics.Debug.WriteLine("[NotificationViewModel] Cleared all notifications");
            }
        }

        public void AddFriendRequestNotification(string senderId, string senderName, string requestId)
        {
            // System.Diagnostics.Debug.WriteLine($"[Notification] AddFriendRequestNotification called: senderId={senderId}, senderName={senderName}, requestId={requestId}");
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
        }
        private async Task AcceptFriendRequest(string senderId, string requestId)
        {
            try
            {
                var receiverId = AuthService.GetUserId();
                // System.Diagnostics.Debug.WriteLine($"[NotificationViewModel] Accepting friend request from {senderId} to {receiverId}");

                var result = await _firebaseService.AcceptFriendRequestAsync(senderId, receiverId, requestId);

                if (result)
                {
                    // Xóa thông báo khỏi danh sách
                    RemoveNotificationByRequestId(requestId);

                    // Hiển thị thông báo thành công với thông tin chi tiết
                    var senderName = Notifications.OfType<FriendRequestNotification>()
                        .FirstOrDefault(n => n.SenderId == senderId)?.SenderName ?? "Bạn";

                    System.Windows.MessageBox.Show(
                        $"🎉 Bạn và {senderName} đã trở thành bạn bè!\n\nBây giờ các bạn có thể:\n• Theo dõi tiến độ học tập của nhau\n• Cùng tham gia các thử thách học tập\n• Chia sẻ thành tích và động lực học tập",
                        "Kết bạn thành công!",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);

                    // Phát âm thanh thành công
                    try
                    {
                        System.Media.SystemSounds.Exclamation.Play();
                    }
                    catch
                    {
                        // Ignore sound errors
                    }

                    // Gửi thông báo đã chấp nhận kết bạn cho sender (sau khi ấn OK)
                    await _firebaseService.NotifyFriendAcceptedAsync(receiverId, senderId);

                    // Reload FriendsList ngay lập tức sau khi chấp nhận kết bạn thành công
                    if (_mainViewModel != null)
                    {
                        // System.Diagnostics.Debug.WriteLine("[NotificationViewModel] Force reloading friends list after accepting friend request");
                        // Sử dụng ForceReloadFriendsListAsync để đảm bảo cập nhật ngay lập tức
                        await _mainViewModel.ForceReloadFriendsListAsync();
                        // System.Diagnostics.Debug.WriteLine("[NotificationViewModel] FriendsList force reloaded successfully after accepting friend request");
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine("[NotificationViewModel] _mainViewModel is null, cannot reload friends list");
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Có lỗi xảy ra khi chấp nhận lời mời kết bạn!", "Lỗi",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[NotificationViewModel] AcceptFriendRequest error: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"[NotificationViewModel] StackTrace: {ex.StackTrace}");
                System.Windows.MessageBox.Show("Có lỗi xảy ra khi chấp nhận lời mời kết bạn!", "Lỗi",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
        }
        private async Task PollFriendRequestsAsync(System.Threading.CancellationToken token)
        {
            var userId = AuthService.GetUserId();
            // System.Diagnostics.Debug.WriteLine($"[Notification] Start polling friend requests for user: {userId}");
            // System.Diagnostics.Debug.WriteLine($"[Notification] Current token: {AuthService.GetToken()?.Substring(0, 50)}...");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var url = GetAuthenticatedUrl($"friendRequests/{userId}.json");
                    // System.Diagnostics.Debug.WriteLine($"[Notification] Polling URL: {url}");                    // Sử dụng HttpClient static từ FirebaseService
                    var response = await Learnify.Services.FirebaseService.SharedHttpClient.GetAsync(url);
                    // System.Diagnostics.Debug.WriteLine($"[Notification] Polling response status: {response.StatusCode}");

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        // System.Diagnostics.Debug.WriteLine("[Notification] Token expired or invalid, stopping polling");
                        break; // Dừng polling khi token hết hạn
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        // System.Diagnostics.Debug.WriteLine($"[Notification] Polling response content: {content}");
                        var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(content);
                        if (dict != null)
                        {
                            foreach (var kv in dict)
                            {
                                var req = kv.Value;
                                string reqId = kv.Key;
                                string senderId = req.senderId;
                                string senderName = req.senderName;
                                string status = req.status;                                // System.Diagnostics.Debug.WriteLine($"[Notification] Found request: reqId={reqId}, senderId={senderId}, senderName={senderName}, status={status}");
                                if (status == "Pending" && !_notifiedFriendRequests.Contains(reqId))
                                {
                                    _notifiedFriendRequests.Add(reqId);
                                    // System.Diagnostics.Debug.WriteLine($"[Notification] Adding notification for Pending request: {reqId}");
                                    App.Current.Dispatcher.Invoke(() =>
                                    {
                                        // System.Diagnostics.Debug.WriteLine($"[Notification] AddFriendRequestNotification: senderId={senderId}, senderName={senderName}, requestId={reqId}");
                                        AddFriendRequestNotification(senderId, senderName, reqId);
                                    });
                                }
                                else if (status == "Pending")
                                {
                                    // System.Diagnostics.Debug.WriteLine($"[Notification] Request {reqId} already notified, skipping");
                                }
                                else
                                {
                                    // System.Diagnostics.Debug.WriteLine($"[Notification] Request {reqId} has status {status}, skipping notification");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // System.Diagnostics.Debug.WriteLine($"[Notification] PollFriendRequestsAsync error: {ex.Message}");
                }
                await Task.Delay(5000, token); // Poll every 5 seconds
            }
        }

        public void AddFriendAcceptedNotification(string friendId, string friendName)
        {
            // System.Diagnostics.Debug.WriteLine($"[Notification] AddFriendAcceptedNotification called: friendId={friendId}, friendName={friendName}");
            var notification = new FriendAcceptedNotification(friendId, friendName);
            AddNotification(notification);

            // Hiển thị thông báo toast/popup cho người gửi
            App.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Hiển thị MessageBox với thông tin rõ ràng
                    System.Windows.MessageBox.Show(
                        $"🎉 {friendName} đã chấp nhận lời mời kết bạn của bạn!\n\nBây giờ các bạn đã là bạn bè và có thể theo dõi tiến độ học tập của nhau.",
                        "Kết bạn thành công!",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information
                    );
                }
                catch (Exception ex)
                {
                    // System.Diagnostics.Debug.WriteLine($"[Notification] Error showing friend accepted popup: {ex.Message}");
                }
            });
        }

        public void AddUnfriendNotification(string friendId, string friendName)
        {
            // System.Diagnostics.Debug.WriteLine($"[Notification] AddUnfriendNotification called: friendId={friendId}, friendName={friendName}");

            // Tạo notification object cho unfriend
            var now = DateTime.Now;
            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                Type = "Unfriend",
                Title = "Hủy kết bạn",
                Message = $"{friendName} đã xóa bạn khỏi danh sách bạn bè",
                Time = now.ToString("HH:mm"),
                Timestamp = now,
                IsRead = false
            };

            AddNotification(notification);

            // Hiển thị thông báo toast/popup
            App.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    System.Windows.MessageBox.Show(
                        $"💔 {friendName} đã xóa bạn khỏi danh sách bạn bè.\n\nBạn sẽ không còn thể xem tiến độ học tập của họ nữa.",
                        "Đã bị hủy kết bạn",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information
                    );
                }
                catch (Exception ex)
                {
                    // System.Diagnostics.Debug.WriteLine($"[Notification] Error showing unfriend popup: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Trigger reload of friends list through MainViewModel
        /// </summary>
        public async void TriggerFriendsListReload()
        {
            try
            {
                if (_mainViewModel != null)
                {
                    // System.Diagnostics.Debug.WriteLine("[NotificationViewModel] TriggerFriendsListReload called - reloading friends list");
                    await _mainViewModel.ForceReloadFriendsListAsync();
                    // System.Diagnostics.Debug.WriteLine("[NotificationViewModel] TriggerFriendsListReload completed successfully");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("[NotificationViewModel] TriggerFriendsListReload failed: _mainViewModel is null");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[NotificationViewModel] TriggerFriendsListReload error: {ex.Message}");
            }
        }

        // Polling for shared campaign requests
        private System.Threading.CancellationTokenSource _sharedCampaignPollingCts;
        public void StartPollingSharedCampaignRequests()
        {
            _sharedCampaignPollingCts = new System.Threading.CancellationTokenSource();
            Task.Run(() => PollSharedCampaignRequestsAsync(_sharedCampaignPollingCts.Token));
        }

        private async Task PollSharedCampaignRequestsAsync(System.Threading.CancellationToken token)
        {
            var firebase = new FirebaseService();
            string userId = Services.AuthService.GetUserId();
            var handledRequests = new HashSet<string>();
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Lấy các request chia sẻ chiến dịch mới
                    var url = firebase.GetAuthenticatedUrl($"sharedCampaignRequests/{userId}.json");
                    var response = await FirebaseService.SharedHttpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(content) && content != "null")
                        {
                            var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(content);
                            foreach (var kv in dict)
                            {
                                string requestId = kv.Key;
                                var req = kv.Value;
                                if (req == null || handledRequests.Contains(requestId)) continue;
                                if ((string)req.status != "Pending") continue;

                                // Hiển thị notification với Accept/Decline
                                App.Current.Dispatcher.Invoke(() =>
                                {
                                    var noti = new Notification
                                    {
                                        Id = requestId,
                                        Type = "SharedCampaign",
                                        Title = "Lời mời tham gia chiến dịch",
                                        Message = $"Bạn nhận được lời mời tham gia chiến dịch '{req.campaignName}' từ bạn bè.",
                                        Time = DateTime.Now.ToString("HH:mm"),
                                        Timestamp = DateTime.Now,
                                        IsRead = false
                                    };
                                    AddNotification(noti);
                                    // Hiển thị popup với Accept/Decline
                                    var result = System.Windows.MessageBox.Show(
                                        $"Bạn nhận được lời mời tham gia chiến dịch '{req.campaignName}' từ bạn bè.\n\nBạn có muốn đồng ý và thêm chiến dịch này vào danh sách của mình không?",
                                        "Chia sẻ chiến dịch",
                                        System.Windows.MessageBoxButton.YesNo,
                                        System.Windows.MessageBoxImage.Question
                                    );
                                    if (result == System.Windows.MessageBoxResult.Yes)
                                    {
                                        // Đồng ý: thêm campaign vào danh sách của mình
                                        Task.Run(async () =>
                                        {
                                            await AcceptSharedCampaignRequest(requestId, req);
                                        });
                                    }
                                    else
                                    {
                                        // Từ chối: cập nhật trạng thái
                                        Task.Run(async () =>
                                        {
                                            await UpdateSharedCampaignRequestStatus(requestId, "Declined");
                                        });
                                    }
                                });
                                handledRequests.Add(requestId);
                            }
                        }
                    }
                }
                catch { }
                await Task.Delay(5000, token); // Poll mỗi 5s
            }
        }

        private async Task AcceptSharedCampaignRequest(string requestId, dynamic req)
        {
            try
            {
                var firebase = new FirebaseService();
                string userId = Services.AuthService.GetUserId();
                // Thêm campaign vào danh sách của mình (giả sử có AddCampaignAsync)
                var campaignData = new Dictionary<string, object>
                {
                    ["title"] = (string)req.campaignName,
                    ["date"] = (string)req.campaignDate,
                    ["sharedBy"] = (string)req.fromUserId,
                    ["acceptedAt"] = DateTime.UtcNow.ToString("o")
                };
                var url = firebase.GetAuthenticatedUrl($"campaigns/{userId}/{requestId}.json");
                var content = new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(campaignData), System.Text.Encoding.UTF8, "application/json");
                var response = await FirebaseService.SharedHttpClient.PutAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    await UpdateSharedCampaignRequestStatus(requestId, "Accepted");
                }
            }
            catch { }
        }

        private async Task UpdateSharedCampaignRequestStatus(string requestId, string status)
        {
            try
            {
                var firebase = new FirebaseService();
                string userId = Services.AuthService.GetUserId();
                var url = firebase.GetAuthenticatedUrl($"sharedCampaignRequests/{userId}/{requestId}/status.json");
                var content = new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(status), System.Text.Encoding.UTF8, "application/json");
                await FirebaseService.SharedHttpClient.PutAsync(url, content);
            }
            catch { }
        }
    }
}