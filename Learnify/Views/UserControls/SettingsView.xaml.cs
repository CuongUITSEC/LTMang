using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.CompilerServices;
using System.ComponentModel;

namespace Learnify.Views
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private bool _notificationsEnabled;
        private string _selectedBackgroundColor;

        public bool NotificationsEnabled
        {
            get => _notificationsEnabled;
            set { _notificationsEnabled = value; OnPropertyChanged(); }
        }

        public string SelectedBackgroundColor
        {
            get => _selectedBackgroundColor;
            set
            {
                _selectedBackgroundColor = value;
                OnPropertyChanged();
                ChangeBackgroundColor();
            }
        }

        public ICommand SignOutCommand { get; }

        public SettingsViewModel()
        {
            SignOutCommand = new RelayCommand(SignOut);
            SelectedBackgroundColor = "White";
        }

        private void ChangeBackgroundColor()
        {
            // Example: Change the main window background
            var app = System.Windows.Application.Current.MainWindow;
            if (app != null)
            {
                switch (SelectedBackgroundColor)
                {
                    case "LightGray":
                        app.Background = Brushes.LightGray;
                        break;
                    case "LightBlue":
                        app.Background = Brushes.LightBlue;
                        break;
                    case "LightGreen":
                        app.Background = Brushes.LightGreen;
                        break;
                    default:
                        app.Background = Brushes.White;
                        break;
                }
            }
        }

        private void SignOut(object obj)
        {
            // Implement sign out logic here
            System.Windows.MessageBox.Show("Signed out!");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
