using Learnify.Commands;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Learnify.ViewModels.Login
{
    public class SIGN_UP_ViewModel : ViewModelBase, IDataErrorInfo
    {
        private string _username;
        private string _password;
        private readonly Action _onLoginSuccess;

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        public ICommand LoginCommand { get; }
        public ICommand ForgotPasswordCommand { get; }

        // Implement IDataErrorInfo
        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(Username):
                        if (string.IsNullOrWhiteSpace(Username))
                            return "Tên đăng nhập không được để trống";
                        if (Username.Length < 5)
                            return "Tên đăng nhập phải có ít nhất 5 ký tự";
                        break;

                    case nameof(Password):
                        if (string.IsNullOrWhiteSpace(Password))
                            return "Mật khẩu không được để trống";
                        if (Password.Length < 6)
                            return "Mật khẩu phải có ít nhất 6 ký tự";
                        break;
                }
                return null;
            }
        }

        public string Error => null;

        public SIGN_UP_ViewModel(Action onLoginSuccess)
        {
            _onLoginSuccess = onLoginSuccess;
            LoginCommand = new ViewModelCommand(ExecuteLogin);
            ForgotPasswordCommand = new ViewModelCommand(ExecuteForgotPassword);
        }

        private bool CanExecuteLogin(object parameter)
        {
            return string.IsNullOrEmpty(this[nameof(Username)]) &&
                   string.IsNullOrEmpty(this[nameof(Password)]);
        }

        private void ExecuteLogin(object parameter)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Kiểm tra dữ liệu đầu vào trước khi xác thực
                var usernameError = this[nameof(Username)];
                var passwordError = this[nameof(Password)];

                if (!string.IsNullOrEmpty(usernameError) || !string.IsNullOrEmpty(passwordError))
                {
                    string errorMessage = $"{usernameError}\n{passwordError}".Trim();
                    ShowErrorMessage(errorMessage, "Dữ liệu không hợp lệ");
                    return;
                }

                // Nếu dữ liệu hợp lệ thì xác thực
                if (AuthenticateUser(Username, Password))
                {
                    _onLoginSuccess?.Invoke();
                }
                else
                {
                    ShowErrorMessage("Tên đăng nhập hoặc mật khẩu không đúng", "Đăng nhập thất bại");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Đã xảy ra lỗi khi đăng nhập: " + ex.Message, "Lỗi hệ thống");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }


        private void ShowErrorMessage(string message, string title)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    MessageBoxResult.OK);
            });
        }

        private void ExecuteForgotPassword(object parameter)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    "Vui lòng liên hệ quản trị hệ thống để đặt lại mật khẩu",
                    "Quên mật khẩu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information,
                    MessageBoxResult.OK);
            });
        }

        private bool AuthenticateUser(string username, string password)
        {
            // Thay thế bằng logic xác thực thực tế
            // Ví dụ đơn giản:
            return username == "admin" && password == "123456";
        }
    }
}