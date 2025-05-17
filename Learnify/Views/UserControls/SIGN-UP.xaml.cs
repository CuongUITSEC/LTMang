using Learnify.ViewModels.Login;
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

namespace Learnify.Views.UserControls
{
    /// <summary>
    /// Interaction logic for SIGN_UP.xaml
    /// </summary>
    public partial class SIGN_UP : UserControl
    {
        public SIGN_UP()
        {
            InitializeComponent();
            
            // Khởi tạo ViewModel với callback khi login thành công
            this.DataContext = new SIGN_UP_ViewModel(() => 
            {
                // Xử lý khi đăng nhập thành công
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    var mainView = new MainView();
                    mainView.Show();
                    window.Close();
                }
            });
        }
        


    }
}
