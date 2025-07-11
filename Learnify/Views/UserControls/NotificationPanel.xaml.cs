﻿using Learnify.ViewModels;
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
using Learnify.Models; 


namespace Learnify.Views.UserControls
{
    /// <summary>
    /// Interaction logic for NotificationPanel.xaml
    /// </summary>
    public partial class NotificationPanel : UserControl
    {
        public NotificationPanel()
        {
            InitializeComponent();
            // XÓA DataContext = new NotificationViewModel();
            // Để DataContext tự nhận từ parent (MainViewModel.NotificationVM)
        }

        private void NotificationItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Notification notification)
            {
                var vm = (NotificationViewModel)DataContext;
                vm.MarkAsReadCommand.Execute(notification);
            }
        }

    }
}
