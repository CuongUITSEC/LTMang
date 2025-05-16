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
    /// Interaction logic for SIGN_IN.xaml
    /// </summary>
    public partial class SIGN_IN : Window
    {
        public SIGN_IN()
        {
            InitializeComponent();
        }
        private void SignUpButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            // đưa vô cơ sở dữ liệu ...
            MessageBox.Show($"Sign Up\nUsername: {username}\nEmail: {email}");
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            // kiểm tra dữ liệu các tài khoản đã đăng ký
            MessageBox.Show($"Sign In\nUsername: {username}");
        }
    }
}
