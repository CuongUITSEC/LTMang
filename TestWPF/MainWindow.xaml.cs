using System.Windows;
using System.Windows.Controls;
using LoginDemo.ViewModel; // nhớ thêm using namespace của ViewModel

namespace LoginDemo.View
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();  // Gán ViewModel cho View
        }
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is MainViewModel viewModel)
            {
                viewModel.Password = (sender as PasswordBox)?.Password;
            }
        }

       
    }

}
