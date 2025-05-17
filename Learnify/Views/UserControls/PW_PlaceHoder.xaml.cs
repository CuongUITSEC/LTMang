using System.Windows;
using System.Windows.Controls;

namespace Learnify.Views.UserControls
{
    public partial class PW_PlaceHoder : UserControl
    {
        private bool isPasswordVisible = false;

        // Dependency Property for Password
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(nameof(Password), typeof(string), typeof(PW_PlaceHoder),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordChanged));

        // Password Property
        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        // Callback when Password property is changed
        private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (PW_PlaceHoder)d;
            var newPassword = e.NewValue as string ?? string.Empty;

            if (control.isPasswordVisible)
            {
                if (control.TB_Input.Text != newPassword)
                    control.TB_Input.Text = newPassword;
            }
            else
            {
                if (control.PW_Input.Password != newPassword)
                    control.PW_Input.Password = newPassword;
            }
        }

        // Dependency Property for PlaceHolder
        public static readonly DependencyProperty Place_HoderProperty =
            DependencyProperty.Register(nameof(Place_Hoder), typeof(string), typeof(PW_PlaceHoder),
                new PropertyMetadata("Enter Password"));

        // PlaceHolder Property
        public string Place_Hoder
        {
            get => (string)GetValue(Place_HoderProperty);
            set => SetValue(Place_HoderProperty, value);
        }

        public PW_PlaceHoder()
        {
            InitializeComponent();
            TB_Input.Visibility = Visibility.Collapsed;
            PW_Input.Visibility = Visibility.Visible;
        }

        // PasswordBox event
        private void PW_Input_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isPasswordVisible)
            {
                Password = PW_Input.Password;
            }

            PWPlaceHoder.Visibility = string.IsNullOrEmpty(PW_Input.Password)
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        // TextBox event
        private void TB_Input_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isPasswordVisible)
            {
                Password = TB_Input.Text;
            }

            PWPlaceHoder.Visibility = string.IsNullOrEmpty(TB_Input.Text)
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        // Toggle password visibility
        private void Btn_ToggleVisibility_Click(object sender, RoutedEventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;

            if (isPasswordVisible)
            {
                TB_Input.Text = Password;
                TB_Input.Visibility = Visibility.Visible;
                PW_Input.Visibility = Visibility.Collapsed;
                EyeIcon.Icon = FontAwesome.Sharp.IconChar.Eye;
            }
            else
            {
                PW_Input.Password = Password;
                PW_Input.Visibility = Visibility.Visible;
                TB_Input.Visibility = Visibility.Collapsed;
                EyeIcon.Icon = FontAwesome.Sharp.IconChar.EyeSlash;
            }
        }
    }
}
