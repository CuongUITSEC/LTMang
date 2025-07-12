using System.Windows;
using System.Windows.Input;
using Learnify.Models;
using Learnify.Services;
using System;
using Learnify.Commands;
using System.ComponentModel;

namespace Learnify.ViewModels
{    public class FriendInfoWindowViewModel : INotifyPropertyChanged
    {
        private readonly FirebaseService _firebaseService = new FirebaseService();
        private readonly NotificationViewModel _notificationViewModel;
        private readonly MainViewModel _mainViewModel;
        
        public Friend Friend { get; }
        public ICommand AddFriendCommand { get; }
        public ICommand UnfriendCommand { get; }
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private string _buttonText = "Kết bạn";
        public string ButtonText
        {
            get => _buttonText;
            set
            {
                if (_buttonText != value)
                {
                    _buttonText = value;
                    OnPropertyChanged(nameof(ButtonText));
                }
            }
        }

        private bool _isFriend = false;
        public bool IsFriend
        {
            get => _isFriend;
            set
            {
                if (_isFriend != value)
                {
                    _isFriend = value;
                    OnPropertyChanged(nameof(IsFriend));
                }
            }
        }

        public string Name => Friend?.Name;
        public bool IsOnline => Friend?.IsOnline ?? false;

        public FriendInfoWindowViewModel(Friend friend, NotificationViewModel notificationViewModel, MainViewModel mainViewModel = null)
        {
            Friend = friend;
            _notificationViewModel = notificationViewModel;
            _mainViewModel = mainViewModel;
            AddFriendCommand = new RelayCommand(SendFriendRequest);
            UnfriendCommand = new RelayCommand(Unfriend);
            
            // Cập nhật text button dựa trên trạng thái
            UpdateButtonText();
        }

        private async void UpdateButtonText()
        {
            try
            {
                var currentUserId = AuthService.GetUserId();
                System.Diagnostics.Debug.WriteLine($"[LOG] currentUserId: {currentUserId}, Friend.Id: {Friend.Id}");

                if (Friend.Id == currentUserId)
                {
                    ButtonText = string.Empty;
                    IsFriend = true;
                    System.Diagnostics.Debug.WriteLine("[LOG] Đang xem chính mình, ẩn nút kết bạn.");
                    return;
                }

                // Kiểm tra trạng thái hiện tại của user
                switch (Friend.Status)
                {
                    case FriendStatus.Friends:
                        ButtonText = "Đã kết bạn";
                        IsFriend = true;
                        System.Diagnostics.Debug.WriteLine("[LOG] Đã là bạn bè, ẩn nút kết bạn.");
                        break;
                        
                    case FriendStatus.Pending:
                        ButtonText = "Đã gửi lời mời";
                        IsFriend = false;
                        System.Diagnostics.Debug.WriteLine("[LOG] Đã gửi lời mời, hiện nút với text 'Đã gửi lời mời'.");
                        break;
                        
                    case FriendStatus.None:
                    default:
                        // Kiểm tra lại từ Firebase để đảm bảo tính chính xác
                        bool alreadyFriends = await _firebaseService.AreAlreadyFriendsAsync(currentUserId, Friend.Id);
                        if (alreadyFriends)
                        {
                            ButtonText = "Đã kết bạn";
                            IsFriend = true;
                            Friend.Status = FriendStatus.Friends;
                            System.Diagnostics.Debug.WriteLine("[LOG] Đã là bạn bè (kiểm tra lại), ẩn nút kết bạn.");
                        }
                        else
                        {
                            bool hasPending = await _firebaseService.HasPendingRequestAsync(currentUserId, Friend.Id);
                            if (hasPending)
                            {
                                ButtonText = "Đã gửi lời mời";
                                IsFriend = false;
                                Friend.Status = FriendStatus.Pending;
                                System.Diagnostics.Debug.WriteLine("[LOG] Đã gửi lời mời (kiểm tra lại), hiện nút với text 'Đã gửi lời mời'.");
                            }
                            else
                            {
                                ButtonText = "Kết bạn";
                                IsFriend = false;
                                Friend.Status = FriendStatus.None;
                                System.Diagnostics.Debug.WriteLine("[LOG] Có thể gửi lời mời, hiện nút 'Kết bạn'.");
                            }
                        }
                        break;
                }
                
                System.Diagnostics.Debug.WriteLine($"[LOG] Kết quả cuối: ButtonText={ButtonText}, IsFriend={IsFriend}, Status={Friend.Status}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LOG] UpdateButtonText error: {ex.Message}");
                ButtonText = "Kết bạn";
                IsFriend = false;
            }
        }

        private async void SendFriendRequest()
        {
            try
            {
                // Gửi request lên Firebase
                var currentUserId = AuthService.GetUserId();
                var currentUserName = AuthService.GetUsername();
                var result = await _firebaseService.SendFriendRequestAsync(currentUserId, currentUserName, Friend.Id, Friend.Name);
                
                // Hiển thị thông báo dựa trên kết quả
                string message;
                MessageBoxImage icon;
                
                switch (result)
                {
                    case SendFriendRequestResult.Success:
                        message = "Đã gửi lời mời kết bạn thành công!";
                        icon = MessageBoxImage.Information;
                        break;
                    case SendFriendRequestResult.AlreadyFriends:
                        message = "Các bạn đã là bạn bè rồi!";
                        icon = MessageBoxImage.Warning;
                        break;
                    case SendFriendRequestResult.HasPending:
                        message = "Bạn đã gửi lời mời kết bạn cho người này rồi, hãy đợi phản hồi!";
                        icon = MessageBoxImage.Warning;
                        break;
                    case SendFriendRequestResult.ExceedsLimit:
                        message = "Bạn đã gửi quá nhiều lời mời kết bạn cho người này trong 30 phút qua. Vui lòng thử lại sau!";
                        icon = MessageBoxImage.Warning;
                        break;
                    case SendFriendRequestResult.Error:
                    default:
                        message = "Có lỗi xảy ra khi gửi lời mời kết bạn. Vui lòng thử lại!";
                        icon = MessageBoxImage.Error;
                        break;
                }
                
                MessageBox.Show(message, "Kết bạn", MessageBoxButton.OK, icon);
                
                // Cập nhật UI ngay lập tức sau khi gửi lời mời thành công
                if (result == SendFriendRequestResult.Success)
                {
                    ButtonText = "Đã gửi lời mời";
                    IsFriend = false;
                    
                    // KHÔNG reload danh sách bạn bè khi gửi lời mời
                    // Chỉ reload khi người nhận chấp nhận lời mời
                    System.Diagnostics.Debug.WriteLine("[FriendInfoWindowVM] Friend request sent successfully, not reloading friends list yet");
                }
                else if (result == SendFriendRequestResult.AlreadyFriends)
                {
                    ButtonText = "Đã kết bạn";
                    IsFriend = true;
                    
                    // Chỉ reload danh sách bạn bè khi đã thực sự là bạn bè
                    if (_mainViewModel != null)
                    {
                        await _mainViewModel.ReloadFriendsListAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FriendInfoWindowVM] SendFriendRequest error: {ex.Message}");
                MessageBox.Show("Có lỗi xảy ra khi gửi lời mời kết bạn. Vui lòng thử lại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Unfriend()
        {
            try
            {
                var currentUserId = AuthService.GetUserId();
                System.Diagnostics.Debug.WriteLine($"[FriendInfoWindowVM] Unfriending: {currentUserId} -> {Friend.Id}");
                
                var result = await _firebaseService.RemoveFriendAsync(currentUserId, Friend.Id);
                if (result)
                {
                    // RemoveFriendAsync already creates unfriend marker, no need for separate notification
                    System.Diagnostics.Debug.WriteLine($"[FriendInfoWindowVM] ✅ Unfriend successful, marker created automatically");
                    
                    // Cập nhật UI ngay lập tức
                    ButtonText = "Kết bạn";
                    IsFriend = false;
                    
                    MessageBox.Show("Đã hủy kết bạn thành công!", "Hủy kết bạn", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Reload danh sách bạn bè ngay lập tức
                    if (_mainViewModel != null)
                    {
                        System.Diagnostics.Debug.WriteLine("[FriendInfoWindowVM] Force reloading friends list after unfriending");
                        await _mainViewModel.ForceReloadFriendsListAsync();
                        System.Diagnostics.Debug.WriteLine("[FriendInfoWindowVM] Friends list force reloaded successfully after unfriending");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[FriendInfoWindowVM] _mainViewModel is null, cannot reload friends list");
                    }
                    
                    // Thông báo cho NotificationViewModel để cập nhật nếu cần
                    if (_notificationViewModel != null)
                    {
                        // Có thể thêm logic thông báo ở đây nếu cần
                    }
                }
                else
                {
                    MessageBox.Show("Có lỗi khi hủy kết bạn!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FriendInfoWindowVM] Unfriend error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[FriendInfoWindowVM] StackTrace: {ex.StackTrace}");
                MessageBox.Show("Có lỗi khi hủy kết bạn!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
