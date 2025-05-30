using System;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using Learnify.Services;

namespace Learnify.ViewModels.Login
{
    public class SIGN_IN_ViewModel : ViewModelBase, IDataErrorInfo
    {
        private string _email;
        private string _password;
        private readonly Action _onRegisterSuccess;
        private string _firebaseIdToken;
        private string _firebaseUserId;

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged(nameof(Email));
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

        public ICommand RegisterCommand { get; }

        public SIGN_IN_ViewModel(Action onRegisterSuccess = null)
        {
            _onRegisterSuccess = onRegisterSuccess;
            RegisterCommand = new ViewModelCommand(async o => await ExecuteRegisterAsync());
        }

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(Email):
                        if (string.IsNullOrWhiteSpace(Email))
                            return "Email không được để trống";
                        if (!Email.Contains("@"))
                            return "Email không hợp lệ";
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

        private async Task ExecuteRegisterAsync()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var emailError = this[nameof(Email)];
                var passwordError = this[nameof(Password)];

                if (!string.IsNullOrEmpty(emailError) || !string.IsNullOrEmpty(passwordError))
                {
                    string errorMessage = $"{emailError}\n{passwordError}".Trim();
                    ShowErrorMessage(errorMessage, "Dữ liệu không hợp lệ");
                    return;
                }

                var result = await RegisterUserWithFirebase(Email, Password);
                if (result)
                {
                    // Lưu token và userId vào AuthService
                    AuthService.SetToken(FirebaseIdToken);
                    AuthService.SetUserId(FirebaseUserId);
                    _onRegisterSuccess?.Invoke();
                }
                else
                {
                    ShowErrorMessage("Đăng ký thất bại. Email có thể đã tồn tại.", "Lỗi");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Đã xảy ra lỗi khi đăng ký: " + ex.Message, "Lỗi hệ thống");
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

        // Đăng ký tài khoản mới với Firebase
        private async Task<bool> RegisterUserWithFirebase(string email, string password)
        {
            const string apiKey = "AIzaSyAhTPGYk6qxu_t-RXT3F3LOxgBk65LicIY"; // Thay bằng API Key của bạn
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}";

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
