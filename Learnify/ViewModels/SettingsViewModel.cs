using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Linq;
using Learnify.Models;
using Learnify.Services;
using Learnify.Commands;
using Learnify.Views;

namespace Learnify.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly FirebaseService _firebaseService;
        private bool _isLoading;
        private string _userName = "Đang tải..."; // Thêm giá trị mặc định
        private string _email = "Đang tải...";
        private string _phone = "Đang tải...";
        private string _country = "Đang tải...";
        private string _userId = "Đang tải...";
        public string UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(nameof(UserId)); }
        }

        // Edit mode properties
        private bool _isEditingUsername;

        public SettingsViewModel()
        {
            _firebaseService = new FirebaseService();
            LoadUserInfoCommand = new RelayCommand(async () => await LoadUserInfoAsync());
            LogoutCommand = new RelayCommand(Logout);

            // Username editing commands
            EditUsernameCommand = new RelayCommand(StartEditUsername);
            SaveUsernameCommand = new RelayCommand(async () => await SaveUsernameAsync());
            CancelEditUsernameCommand = new RelayCommand(CancelEditUsername);
            ChangePasswordCommand = new RelayCommand(OpenChangePasswordDialog);

            // Tải thông tin user khi khởi tạo
            _ = LoadUserInfoAsync();
        }

        public string UserName
        {
            get => _userName;
            set { _userName = value; OnPropertyChanged(nameof(UserName)); }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged(nameof(Email));
            }
        }

        public string Phone
        {
            get => _phone;
            set
            {
                _phone = value;
                OnPropertyChanged(nameof(Phone));
            }
        }
        public string Country
        {
            get => _country;
            set
            {
                _country = value;
                OnPropertyChanged(nameof(Country));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        // Edit mode properties
        public bool IsEditingUsername
        {
            get => _isEditingUsername;
            set { _isEditingUsername = value; OnPropertyChanged(nameof(IsEditingUsername)); }
        }

        public ICommand LoadUserInfoCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand EditUsernameCommand { get; }
        public ICommand SaveUsernameCommand { get; }
        public ICommand CancelEditUsernameCommand { get; }
        public ICommand ChangePasswordCommand { get; }

        private async Task LoadUserInfoAsync()
        {
            try
            {
                IsLoading = true;
                // System.Diagnostics.Debug.WriteLine($"[SETTINGS] Loading user info...");

                var userId = AuthService.GetUserId();
                UserId = userId ?? "N/A";
                // System.Diagnostics.Debug.WriteLine($"[SETTINGS] userId: {userId}");

                if (string.IsNullOrEmpty(userId))
                {
                    // System.Diagnostics.Debug.WriteLine($"[SETTINGS] No userId found, using guest info");
                    UserName = "Guest User";
                    Email = "guest@example.com";
                    Phone = "0000000000";
                    Country = "Việt Nam";
                    return;
                }

                // Thử lấy username trực tiếp trước
                var directUsername = await _firebaseService.GetUsernameAsync(userId);
                // System.Diagnostics.Debug.WriteLine($"[SETTINGS] Direct username: {directUsername}");

                var user = await _firebaseService.GetUserInfoAsync(userId);
                // System.Diagnostics.Debug.WriteLine($"[SETTINGS] GetUserInfoAsync result:");
                // System.Diagnostics.Debug.WriteLine($"[SETTINGS] - user is null: {user == null}");

                if (user != null)
                {
                    // System.Diagnostics.Debug.WriteLine($"[SETTINGS] - user.Username: '{user.Username}'");
                    // System.Diagnostics.Debug.WriteLine($"[SETTINGS] - user.Email: '{user.Email}'");
                    // System.Diagnostics.Debug.WriteLine($"[SETTINGS] - user.PhoneNumber: '{user.PhoneNumber}'");
                    // System.Diagnostics.Debug.WriteLine($"[SETTINGS] - user.Country: '{user.Country}'");

                    // Ưu tiên username trực tiếp nếu có
                    if (!string.IsNullOrEmpty(directUsername) && directUsername != "null")
                    {
                        UserName = directUsername;
                    }
                    else if (!string.IsNullOrEmpty(user.Username))
                    {
                        UserName = user.Username;
                    }
                    else
                    {
                        UserName = "Chưa đặt tên";
                    }

                    Email = user.Email ?? "N/A";
                    Phone = user.PhoneNumber ?? "N/A";
                    Country = user.Country ?? "N/A";
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"[SETTINGS] User object is null, setting default values");
                    UserName = !string.IsNullOrEmpty(directUsername) && directUsername != "null" ? directUsername : "Chưa đặt tên";
                    Email = "N/A";
                    Phone = "N/A";
                    Country = "N/A";
                }

                // System.Diagnostics.Debug.WriteLine($"[SETTINGS] Final values:");
                // System.Diagnostics.Debug.WriteLine($"[SETTINGS] - UserName: '{UserName}'");
                // System.Diagnostics.Debug.WriteLine($"[SETTINGS] - Email: '{Email}'");
                // System.Diagnostics.Debug.WriteLine($"[SETTINGS] - Phone: '{Phone}'");
                // System.Diagnostics.Debug.WriteLine($"[SETTINGS] - Country: '{Country}'");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[SETTINGS] Error loading user info: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"[SETTINGS] StackTrace: {ex.StackTrace}");
                UserName = "Error Loading";
                Email = "N/A";
                Phone = "N/A";
                Country = "N/A";
            }
            finally
            {
                IsLoading = false;
                // System.Diagnostics.Debug.WriteLine($"[SETTINGS] Loading completed. IsLoading = {IsLoading}");
            }
        }
        private void Logout()
        {
            try
            {
                // Xóa token và thông tin đăng nhập
                AuthService.ClearToken();

                // Tìm MainView hiện tại và thay thế bằng Start window
                var mainWindow = Application.Current.Windows.OfType<MainView>().FirstOrDefault();
                if (mainWindow != null)
                {
                    // Tạo và hiển thị Start window
                    var startWindow = new Start();
                    startWindow.Show();

                    // Đóng MainView
                    mainWindow.Close();
                }

                // System.Diagnostics.Debug.WriteLine("User logged out successfully");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"Error during logout: {ex.Message}");
                MessageBox.Show($"Đã xảy ra lỗi khi đăng xuất: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartEditUsername()
        {
            IsEditingUsername = true;
        }

        private async Task SaveUsernameAsync()
        {
            // Validate và lưu UserName lên server/Firebase
            if (string.IsNullOrWhiteSpace(UserName) || UserName.Length < 3)
            {
                MessageBox.Show("Tên người dùng không hợp lệ.");
                return;
            }
            var userId = AuthService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _firebaseService.SaveUsernameAsync(userId, UserName);
            }
            IsEditingUsername = false;
        }

        private void CancelEditUsername()
        {
            IsEditingUsername = false;
            // Optionally reload UserName từ server nếu cần
        }
        private void OpenChangePasswordDialog()
        {
            try
            {
                var changePasswordWindow = new Views.ChangePasswordWindow();
                changePasswordWindow.Owner = Application.Current.Windows.OfType<Views.MainView>().FirstOrDefault();

                var result = changePasswordWindow.ShowDialog();

                if (result == true && (changePasswordWindow.DialogResult == true))
                {
                    MessageBox.Show(
                        "Mật khẩu đã được thay đổi thành công!",
                        "Thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                // System.Diagnostics.Debug.WriteLine($"Error in change password: {ex.Message}");
            }
        }
    }
}
