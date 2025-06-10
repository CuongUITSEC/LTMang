using System.Windows;
using System.Windows.Input;
using Learnify.Models;
using Learnify.Services;
using System;
using Learnify.Commands;

namespace Learnify.ViewModels
{    public class FriendInfoWindowViewModel
    {
        private readonly FirebaseService _firebaseService = new FirebaseService();
        private readonly NotificationViewModel _notificationViewModel;
        
        public Friend Friend { get; }
        public ICommand AddFriendCommand { get; }
        
        private string _buttonText = "Kết bạn";
        public string ButtonText
        {
            get => _buttonText;
            set
            {
                _buttonText = value;
                // Nếu có PropertyChanged event thì trigger ở đây
            }
        }

        public FriendInfoWindowViewModel(Friend friend, NotificationViewModel notificationViewModel)
        {
            Friend = friend;
            _notificationViewModel = notificationViewModel;
            AddFriendCommand = new RelayCommand(SendFriendRequest);
            
            // Cập nhật text button dựa trên trạng thái
            UpdateButtonText();
        }

        private async void UpdateButtonText()
        {
            try
            {
                var currentUserId = AuthService.GetUserId();
                if (await _firebaseService.AreAlreadyFriendsAsync(currentUserId, Friend.Id))
                {
                    ButtonText = "Đã kết bạn";
                }
                else if (await _firebaseService.HasPendingRequestAsync(currentUserId, Friend.Id))
                {
                    ButtonText = "Đã gửi lời mời";
                }
                else
                {
                    ButtonText = "Kết bạn";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FriendInfoWindowVM] UpdateButtonText error: {ex.Message}");
                ButtonText = "Kết bạn";
            }
        }private async void SendFriendRequest()
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FriendInfoWindowVM] SendFriendRequest error: {ex.Message}");
                MessageBox.Show("Có lỗi xảy ra khi gửi lời mời kết bạn. Vui lòng thử lại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
