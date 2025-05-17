using System.ComponentModel;

namespace Learnify.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private string _userName = "Peter Pan";
        private string _email = "peter1990@gmail.com";
        private string _phone = "0991234589";
        private string _country = "Việt Nam";

        public string UserName
        {
            get => _userName;
            set { _userName = value; OnPropertyChanged(nameof(UserName)); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(nameof(Email)); }
        }

        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(nameof(Phone)); }
        }

        public string Country
        {
            get => _country;
            set { _country = value; OnPropertyChanged(nameof(Country)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
