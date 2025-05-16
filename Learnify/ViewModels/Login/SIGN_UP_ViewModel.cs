using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Learnify.Commands;




namespace Learnify.ViewModels.Login
{
    public class SIGN_UP_ViewModel : ViewModelBase
    {
        private readonly Action _onLoginSuccess;
        public string Username { get; set; }
        public string Password { get; set; }

        public SIGN_UP_ViewModel(Action onLoginSuccess)
        {
            _onLoginSuccess = onLoginSuccess;
            LoginCommand = new ViewModelCommand(ExecuteLogin);
        }

        public ICommand LoginCommand { get; }

        private void ExecuteLogin(object obj)
        {
            // TODO: Thay bằng xác thực thực tế
            if (Username == "admin" && Password == "123")
            {
                _onLoginSuccess?.Invoke();
            }
            else
            {
                MessageBox.Show("Tên đăng nhập hoặc mật khẩu không đúng!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
