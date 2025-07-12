using System;
using System.Windows;
using System.Windows.Controls;
using Learnify.Services;

namespace Learnify.Views
{
    public partial class ChangePasswordWindow : Window
    {
        private readonly FirebaseService _firebaseService;
        private bool _isCurrentPasswordVisible = false;
        private bool _isNewPasswordVisible = false;

        public ChangePasswordWindow()
        {
            InitializeComponent();
            _firebaseService = new FirebaseService();
        }

        private void CurrentPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidateInput();
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidateInput();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ValidateInput()
        {
            string currentPassword = CurrentPasswordBox.Password;
            string newPassword = NewPasswordBox.Password;

            bool isValid = (string.IsNullOrEmpty(currentPassword) == false)
                        && (string.IsNullOrEmpty(newPassword) == false)
                        && (newPassword.Length >= 6)
                        && (currentPassword != newPassword);

            ChangePasswordButton.IsEnabled = isValid;

            if (isValid)
            {
                ErrorMessage.Visibility = Visibility.Collapsed;
            }
            else if ((string.IsNullOrEmpty(currentPassword) == false) && (string.IsNullOrEmpty(newPassword) == false))
            {
                if (newPassword.Length < 6)
                {
                    ShowError("Mật khẩu mới phải có ít nhất 6 ký tự.");
                }
                else if (currentPassword == newPassword)
                {
                    ShowError("Mật khẩu mới phải khác mật khẩu hiện tại.");
                }
            }
        }

        private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disable the button to prevent multiple clicks
                ChangePasswordButton.IsEnabled = false;
                ChangePasswordButton.Content = "Đang thay đổi...";
                
                // Clear any previous error messages
                ErrorMessage.Visibility = Visibility.Collapsed;

                string currentPassword = CurrentPasswordBox.Password;
                string newPassword = NewPasswordBox.Password;

                // Validate password strength
                if (!IsPasswordStrong(newPassword))
                {
                    ShowError("Mật khẩu mới phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường và số.");
                    return;
                }

                // Check if new password is different from current
                if (currentPassword == newPassword)
                {
                    ShowError("Mật khẩu mới phải khác mật khẩu hiện tại.");
                    return;
                }

                // Lấy email thực từ database để xác thực đổi mật khẩu
                var userId = AuthService.GetUserId();
                string email = null;
                if (!string.IsNullOrEmpty(userId))
                {
                    try
                    {
                        var userInfo = await _firebaseService.GetUserInfoAsync(userId);
                        email = userInfo?.Email;
                    }
                    catch { }
                }
                if (string.IsNullOrEmpty(email))
                {
                    ShowError("Không thể lấy email từ hệ thống. Vui lòng đăng nhập lại.");
                    return;
                }

                // Gán email vào AuthService để hàm ChangePasswordAsync dùng đúng email
                AuthService.SetUsername(email);

                // Call the Firebase service to change password
                bool success = await _firebaseService.ChangePasswordAsync(currentPassword, newPassword);
                
                // System.Diagnostics.Debug.WriteLine($"Password change result: {success}");
                
                if (success)
                {
                    MessageBox.Show("Mật khẩu đã được thay đổi thành công!", "Thành công", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    ShowError("Không thể thay đổi mật khẩu. Vui lòng kiểm tra mật khẩu hiện tại và thử lại.");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"Error in ChangePasswordButton_Click: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                ShowError($"Đã xảy ra lỗi: {ex.Message}");
            }
            finally
            {
                // Re-enable the button
                ChangePasswordButton.IsEnabled = true;
                ChangePasswordButton.Content = "Thay đổi mật khẩu";
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }

        private bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            bool hasUpper = false;
            bool hasLower = false;
            bool hasDigit = false;

            foreach (char c in password)
            {
                if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsLower(c)) hasLower = true;
                else if (char.IsDigit(c)) hasDigit = true;
            }

            return hasUpper && hasLower && hasDigit;
        }

        private void ToggleCurrentPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            _isCurrentPasswordVisible = !_isCurrentPasswordVisible;
            if (_isCurrentPasswordVisible)
            {
                CurrentPasswordTextBox.Text = CurrentPasswordBox.Password;
                CurrentPasswordTextBox.Visibility = Visibility.Visible;
                CurrentPasswordBox.Visibility = Visibility.Collapsed;
                ToggleCurrentPasswordButton.Content = "🙈";
            }
            else
            {
                CurrentPasswordBox.Password = CurrentPasswordTextBox.Text;
                CurrentPasswordBox.Visibility = Visibility.Visible;
                CurrentPasswordTextBox.Visibility = Visibility.Collapsed;
                ToggleCurrentPasswordButton.Content = "👁";
            }
        }

        private void ToggleNewPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            _isNewPasswordVisible = !_isNewPasswordVisible;
            if (_isNewPasswordVisible)
            {
                NewPasswordTextBox.Text = NewPasswordBox.Password;
                NewPasswordTextBox.Visibility = Visibility.Visible;
                NewPasswordBox.Visibility = Visibility.Collapsed;
                ToggleNewPasswordButton.Content = "🙈";
            }
            else
            {
                NewPasswordBox.Password = NewPasswordTextBox.Text;
                NewPasswordBox.Visibility = Visibility.Visible;
                NewPasswordTextBox.Visibility = Visibility.Collapsed;
                ToggleNewPasswordButton.Content = "👁";
            }
        }

        private void CurrentPasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isCurrentPasswordVisible)
            {
                CurrentPasswordBox.Password = CurrentPasswordTextBox.Text;
            }
            ValidateInput();
        }

        private void NewPasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isNewPasswordVisible)
            {
                NewPasswordBox.Password = NewPasswordTextBox.Text;
            }
            ValidateInput();
        }
    }
}
