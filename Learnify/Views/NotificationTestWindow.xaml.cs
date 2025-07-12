using System;
using System.Windows;
using Learnify.ViewModels;
using Learnify.Views;

namespace Learnify.Views
{
    /// <summary>
    /// Demo window for testing scrollable notifications
    /// </summary>
    public partial class NotificationTestWindow : Window
    {
        private NotificationViewModel _notificationViewModel;
        
        public NotificationTestWindow()
        {
            InitializeComponent();
            
            // Create NotificationViewModel
            _notificationViewModel = new NotificationViewModel();
            
            // Set DataContext for the NotificationView
            NotificationViewContainer.DataContext = _notificationViewModel;
            
            // System.Diagnostics.Debug.WriteLine("[NotificationTestWindow] Initialized with NotificationViewModel");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
