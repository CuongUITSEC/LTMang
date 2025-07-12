using Learnify.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Learnify.Views.UserControls
{
    /// <summary>
    /// Interaction logic for PomodoroView.xaml
    /// </summary>
    public partial class PomodoroView : UserControl
    {
        public PomodoroView()
        {
            InitializeComponent();
            // Đăng ký sự kiện Checked cho 2 nút
            TimerMode.Checked += Mode_Checked;
            PomoMode.Checked += Mode_Checked;

            // Đảm bảo luôn có 1 nút được chọn
            TimerMode.Unchecked += Mode_Unchecked;
            PomoMode.Unchecked += Mode_Unchecked;

            // Gán sự kiện cho cả hai toggle
            PomoMode.Click += (s, e) => AnimateBorder(toRight: true);
            TimerMode.Click += (s, e) => AnimateBorder(toRight: false);

            // Khởi tạo: chọn mặc định TimerMode
            TimerMode.IsChecked = true;


        }

        private void AnimateBorder(bool toRight)
        {
            double targetX = toRight ? 110 : 10; // giá trị cần điều chỉnh theo layout thực tế

            ThicknessAnimation moveAnimation = new ThicknessAnimation
            {
                To = new Thickness(targetX, 0, 0, 3),
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            boder1.BeginAnimation(MarginProperty, moveAnimation);
        }


        private void Mode_Checked(object sender, RoutedEventArgs e)
        {
            var btn = sender as ToggleButton;
            if (btn == null) return;

            if (btn == TimerMode)
                ActivateTimerMode();
            else if (btn == PomoMode)
                ActivatePomoMode();
        }

        private void Mode_Unchecked(object sender, RoutedEventArgs e)
        {
            // Nếu người dùng uncheck cả hai, ta sẽ tự chọn lại nút vừa bị bỏ
            // (để không có trạng thái không chọn)
            if (TimerMode.IsChecked == false && PomoMode.IsChecked == false)
            {
                // sender là nút vừa uncheck → check lại nó
                ((ToggleButton)sender).IsChecked = true;
            }
        }

        private void ActivateTimerMode()
        {
            // Chuyển sang chế độ Timer
            TimerMode.IsChecked = true;
            PomoMode.IsChecked = false;

            // Thay ảnh cho nút nhỏ

            PomoMode.Background = CreateImageBrush("Images/pomodoro-technique 1 (1).png");
            TimerMode.Background = CreateImageBrush("Images/timer 1.png");
            // (nếu bạn có Image lớn ở ngoài, cũng đổi ở đây)
            // MainImage.Source = new BitmapImage(new Uri("timer 1.png", UriKind.Relative));
        }

        private void ActivatePomoMode()
        {
            // Chuyển sang chế độ Pomodoro
            PomoMode.IsChecked = true;
            TimerMode.IsChecked = false;

            // Thay ảnh cho nút nhỏ

            TimerMode.Background = CreateImageBrush("Images/timer 1 (1).png");
            PomoMode.Background = CreateImageBrush("Images/pomodoro-technique 1.png");

            // (nếu bạn có Image lớn ở ngoài, cũng đổi ở đây)
            // MainImage.Source = new BitmapImage(new Uri("pomodoro-technique 1 (1).png", UriKind.Relative));
        }

        private ImageBrush CreateImageBrush(string fileName)
        {
            // Đường dẫn fileName là đường dẫn tương đối từ thư mục project / bin
            var uri = new Uri($"pack://application:,,,/{fileName}", UriKind.Absolute);
            var img = new BitmapImage(uri);
            return new ImageBrush(img);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
