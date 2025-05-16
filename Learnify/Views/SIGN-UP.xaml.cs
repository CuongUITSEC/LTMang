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
using System.Windows.Shapes;

namespace Learnify.Views
{
    /// <summary>
    /// Interaction logic for SIGN_UP.xaml
    /// </summary>
    public partial class SIGN_UP : Window
    {
        public SIGN_UP()
        {
            InitializeComponent();
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            // so sánh với cơ sở dữ liệu đã đăng ký
            MessageBox.Show($"Username: {username}\nPassword: {password}", "Login Attempt");
        }

        private void SignUpButton_Click(object sender, RoutedEventArgs e)
        {
            // chuyển đến chỗ đăng kí
            MessageBox.Show("Sign up button clicked!", "Sign Up");
        }

    }
}
