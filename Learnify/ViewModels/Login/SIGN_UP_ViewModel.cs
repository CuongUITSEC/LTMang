using Learnify.Commands;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Net.Http; 
using System.Text;     
using Newtonsoft.Json;
using Learnify.Services;


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
            LoginCommand = new ViewModelCommand(async o => await ExecuteLoginAsync());
            ForgotPasswordCommand = new ViewModelCommand(ExecuteForgotPassword);
        }

        private bool CanExecuteLogin(object parameter)
        {
            return string.IsNullOrEmpty(this[nameof(Username)]) &&
                   string.IsNullOrEmpty(this[nameof(Password)]);
        }

        private async Task ExecuteLoginAsync()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var usernameError = this[nameof(Username)];
                var passwordError = this[nameof(Password)];

                if (!string.IsNullOrEmpty(usernameError) || !string.IsNullOrEmpty(passwordError))
                {
                    string errorMessage = $"{usernameError}\n{passwordError}".Trim();
                    ShowErrorMessage(errorMessage, "Dữ liệu không hợp lệ");
                    return;
                }

                // Xác thực với Firebase
                var result = await AuthenticateUserWithFirebase(Username, Password);
                if (result)
                {
                    try
                    {
                        // Lưu token và userId vào AuthService
                        AuthService.SetToken(FirebaseIdToken);
                        AuthService.SetUserId(FirebaseUserId);
                        
                        // Kiểm tra và tạo username nếu chưa có
                        var firebaseService = new FirebaseService();
                        var existingUsername = await firebaseService.GetUsernameAsync(FirebaseUserId);
                        
                        if (string.IsNullOrEmpty(existingUsername) || existingUsername == "null" || existingUsername == FirebaseUserId)
                        {
                            // Nếu chưa có username, tạo mới từ email
                            var defaultUsername = Username.Split('@')[0];
                            var saveResult = await firebaseService.SaveUsernameAsync(FirebaseUserId, defaultUsername);
                            if (!saveResult)
                            {
                                throw new Exception("Không thể lưu thông tin người dùng");
                            }
                            existingUsername = defaultUsername;
                        }
                        
                        AuthService.SetUsername(existingUsername);
                        _onLoginSuccess?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage("Đã xảy ra lỗi khi lưu thông tin người dùng: " + ex.Message, "Lỗi hệ thống");
                        return;
                    }
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
                    MessageBoxImage.Error);
            });
        }

        private async void ExecuteForgotPassword(object parameter)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                if (string.IsNullOrWhiteSpace(Username))
                {
                    ShowErrorMessage("Vui lòng nhập email để đặt lại mật khẩu.", "Thiếu thông tin");
                    return;
                }

                // Gửi yêu cầu đặt lại mật khẩu qua Firebase REST API
                const string apiKey = "AIzaSyAhTPGYk6qxu_t-RXT3F3LOxgBk65LicIY"; // <-- Thay bằng API Key của bạn
                var url = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={apiKey}";

                var payload = new
                {
                    requestType = "PASSWORD_RESET",
                    email = Username
                };

                using (var client = new HttpClient())
                {
                    var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(
                                "Đã gửi email đặt lại mật khẩu. Vui lòng kiểm tra hộp thư của bạn.",
                                "Quên mật khẩu",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        });
                    }
                    else
                    {
                        // Lấy thông báo lỗi từ Firebase nếu có
                        string errorMsg = "Không thể gửi email đặt lại mật khẩu. Vui lòng kiểm tra lại email.";
                        try
                        {
                            dynamic errorObj = JsonConvert.DeserializeObject(responseString);
                            if (errorObj != null && errorObj.error != null && errorObj.error.message != null)
                                errorMsg = (string)errorObj.error.message;
                        }
                        catch { }
                        ShowErrorMessage(errorMsg, "Lỗi");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Đã xảy ra lỗi khi gửi yêu cầu đặt lại mật khẩu: " + ex.Message, "Lỗi hệ thống");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        // Firebase authentication via REST API
        private string _firebaseIdToken;
        private string _firebaseUserId;
        public string FirebaseIdToken
        {
            get => _firebaseIdToken;
            private set
            {
                _firebaseIdToken = value;
                OnPropertyChanged(nameof(FirebaseIdToken));
            }
        }

        public string FirebaseUserId
        {
            get => _firebaseUserId;
            private set
            {
                _firebaseUserId = value;
                OnPropertyChanged(nameof(FirebaseUserId));
            }
        }

        // Update AuthenticateUserWithFirebase to retrieve and store the ID token and userId
        private async Task<bool> AuthenticateUserWithFirebase(string email, string password)
        {
            const string apiKey = "AIzaSyAhTPGYk6qxu_t-RXT3F3LOxgBk65LicIY"; // <-- Replace with your API Key
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";

            var payload = new
            {
                email = email,
                password = password,
                returnSecureToken = true
            };

            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Parse the ID token and userId from the response
                    try
                    {
                        dynamic resultObj = JsonConvert.DeserializeObject(responseString);
                        if (resultObj != null)
                        {
                            if (resultObj.idToken != null)
                            {
                                FirebaseIdToken = (string)resultObj.idToken;
                            }
                            if (resultObj.localId != null)
                            {
                                FirebaseUserId = (string)resultObj.localId;
                            }
                        }
                    }
                    catch
                    {
                        FirebaseIdToken = null;
                        FirebaseUserId = null;
                    }
                    return true;
                }
                else
                {
                    FirebaseIdToken = null;
                    FirebaseUserId = null;
                    return false;
                }
            }
        }
    }
}