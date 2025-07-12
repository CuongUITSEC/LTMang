using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Learnify.Services;
using Learnify.ViewModels;

namespace Learnify.Views
{
    /// <summary>
    /// Interaction logic for NotificationView.xaml
    /// </summary>
    public partial class NotificationView : UserControl
    {
        private FirebaseService _firebaseService;
        
        public NotificationView()
        {
            InitializeComponent();
            
            // Initialize FirebaseService
            _firebaseService = new FirebaseService();
            
            // System.Diagnostics.Debug.WriteLine("[NotificationView] Initialized");
        }

        /// <summary>
        /// Test scroll notifications button click
        /// </summary>
        private async void TestScrollButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                button.IsEnabled = false;
                button.Content = "⏳ Loading...";
                
                // Ensure FirebaseService has NotificationVM
                if (_firebaseService.NotificationVM == null && DataContext is NotificationViewModel notificationVM)
                {
                    _firebaseService.NotificationVM = notificationVM;
                }
                
                var result = await _firebaseService.TestScrollNotificationsAsync();
                
                // Show result temporarily
                var originalContent = button.Content;
                button.Content = "✅ Done!";
                
                // System.Diagnostics.Debug.WriteLine($"[NotificationView] Test scroll result: {result}");
                
                // Reset button after 2 seconds
                await Task.Delay(2000);
                button.Content = "📝 Test Scroll";
                button.IsEnabled = true;
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[NotificationView] Test scroll error: {ex.Message}");
                
                var button = sender as Button;
                button.Content = "❌ Error";
                await Task.Delay(2000);
                button.Content = "📝 Test Scroll";
                button.IsEnabled = true;
            }
        }

        /// <summary>
        /// Test all notification types button click
        /// </summary>
        private async void TestAllTypesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                button.IsEnabled = false;
                button.Content = "⏳ Loading...";
                
                // Ensure FirebaseService has NotificationVM
                if (_firebaseService.NotificationVM == null && DataContext is NotificationViewModel notificationVM)
                {
                    _firebaseService.NotificationVM = notificationVM;
                }
                
                var result = await _firebaseService.TestAllNotificationTypesAsync();
                
                // Show result temporarily
                button.Content = "✅ Done!";
                
                // System.Diagnostics.Debug.WriteLine($"[NotificationView] Test all types result: {result}");
                
                // Reset button after 2 seconds
                await Task.Delay(2000);
                button.Content = "🎯 Test All Types";
                button.IsEnabled = true;
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[NotificationView] Test all types error: {ex.Message}");
                
                var button = sender as Button;
                button.Content = "❌ Error";
                await Task.Delay(2000);
                button.Content = "🎯 Test All Types";
                button.IsEnabled = true;
            }
        }

        /// <summary>
        /// Show stats button click
        /// </summary>
        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ensure FirebaseService has NotificationVM
                if (_firebaseService.NotificationVM == null && DataContext is NotificationViewModel notificationVM)
                {
                    _firebaseService.NotificationVM = notificationVM;
                }
                
                var stats = _firebaseService.GetNotificationStats();
                
                MessageBox.Show(stats, "Thống kê thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // System.Diagnostics.Debug.WriteLine($"[NotificationView] Stats: {stats}");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[NotificationView] Stats error: {ex.Message}");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Clear all notifications button click
        /// </summary>
        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ensure FirebaseService has NotificationVM
                if (_firebaseService.NotificationVM == null && DataContext is NotificationViewModel notificationVM)
                {
                    _firebaseService.NotificationVM = notificationVM;
                }
                
                var result = MessageBox.Show("Bạn có chắc chắn muốn xóa tất cả thông báo?", 
                                            "Xác nhận xóa", 
                                            MessageBoxButton.YesNo, 
                                            MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    var clearResult = _firebaseService.ClearAllNotifications();
                    // System.Diagnostics.Debug.WriteLine($"[NotificationView] Clear all result: {clearResult}");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[NotificationView] Clear all error: {ex.Message}");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Auto scroll to top when new notification added
        /// </summary>
        private void NotificationsList_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            try
            {
                // Auto scroll to top when new notifications are added
                NotificationScrollViewer.ScrollToTop();
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[NotificationView] Auto scroll error: {ex.Message}");
            }
        }
    }
}
