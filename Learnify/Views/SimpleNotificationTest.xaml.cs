using System;
using System.Windows;
using System.Windows.Controls;
using Learnify.ViewModels;

namespace Learnify.Views
{
    /// <summary>
    /// Simple test window for notifications without complex dependencies
    /// </summary>
    public partial class SimpleNotificationTest : Window
    {
        private NotificationViewModel _notificationViewModel;
        
        public SimpleNotificationTest()
        {
            InitializeComponent();
            
            // Create NotificationViewModel
            _notificationViewModel = new NotificationViewModel();
            
            // Set DataContext
            this.DataContext = _notificationViewModel;
            
            // System.Diagnostics.Debug.WriteLine("[SimpleNotificationTest] Initialized");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Add a simple test notification
                var testNotification = new Learnify.Models.Notification
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "Test",
                    Title = "Test Notification",
                    Message = "This is a test notification for scrollable interface",
                    Time = DateTime.Now.ToString("HH:mm"),
                    Timestamp = DateTime.Now,
                    IsRead = false
                };
                
                _notificationViewModel.AddNotification(testNotification);
                // System.Diagnostics.Debug.WriteLine("[SimpleNotificationTest] Added test notification");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[SimpleNotificationTest] Error: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
