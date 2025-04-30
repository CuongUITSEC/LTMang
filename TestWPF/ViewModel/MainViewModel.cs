using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using LoginDemo.Service;
using LoginDemo.Model;

namespace LoginDemo.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly UserService _userService = new UserService();

        private string _username;
        private string _password;

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

        public MainViewModel()
        {
            LoginCommand = new RelayCommand(Login);
        }

        private void Login()
        {
            User user = _userService.GetUserByUsername(Username);

            if (user != null && user.Password == Password)
            {
                MessageBox.Show("Login successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Invalid username or password.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
