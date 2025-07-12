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
using System.Runtime.InteropServices;
using System.Runtime;
using SharpVectors.Woffs;
using System.Windows.Interop;
using Learnify.Properties;
using Learnify.ViewModels;

namespace Learnify.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        private MainViewModel _viewModel;

        public MainView()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // Dừng polling khi đóng cửa sổ
            _viewModel?.Dispose();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void btnMinisize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        private void pnlControlBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            SendMessage(helper.Handle, 161, 2, 0);
        }

        private void pnlControlBar_MouseEnter(object sender, MouseEventArgs e)
        {
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Xóa nội dung tìm kiếm
                _viewModel.SearchText = string.Empty;
                
                // Focus lại vào TextBox
                txtBoxSearch.Focus();
                
                // System.Diagnostics.Debug.WriteLine("[MainView] Search text cleared");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[MainView] btnClearSearch_Click error: {ex.Message}");
            }
        }

        private void txtBoxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // Nếu nhấn Ctrl+A, xóa toàn bộ text
                if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    txtBoxSearch.SelectAll();
                    e.Handled = true;
                }
                // Nếu nhấn Escape, xóa text và focus
                else if (e.Key == Key.Escape)
                {
                    _viewModel.SearchText = string.Empty;
                    txtBoxSearch.Focus();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[MainView] txtBoxSearch_KeyDown error: {ex.Message}");
            }
        }
    }
}
