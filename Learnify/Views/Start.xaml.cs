using System;
using System.Windows;
using Learnify.ViewModels.Login;

namespace Learnify.Views
{
    public partial class Start : Window
    {
        public Start()
        {
            InitializeComponent();
            var viewModel = new StartViewModel();
            this.DataContext = viewModel;
            
            // Đăng ký sự kiện khi đăng nhập thành công
            viewModel.LoginSucceeded += OpenMainView;
        }

        private void OpenMainView()
        {
            // Tạo và hiển thị MainView
            var mainView = new MainView();
            mainView.Show();
            
            // Đóng cửa sổ hiện tại
            this.Close();
        }
    }
}