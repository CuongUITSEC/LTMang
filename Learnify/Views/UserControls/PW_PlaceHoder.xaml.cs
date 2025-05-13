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
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        // Password Property
        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set
            {
                SetValue(PasswordProperty, value);
                if (isPasswordVisible)
                {
                    TB_Input.Text = value ?? "";
                }
                else
                {
                    if (PW_Input.Password != value)
                        PW_Input.Password = value ?? "";
                }
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
        }

        // Handle Password changes
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

        // Handle TextBox visibility and password changes
        private void TB_Input_TextChanged(object sender, TextChangedEventArgs e)
        {
            PWPlaceHoder.Visibility = string.IsNullOrEmpty(TB_Input.Text)
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        // Toggle visibility between PasswordBox and TextBox
        private void Btn_ToggleVisibility_Click(object sender, RoutedEventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;

            if (isPasswordVisible)
            {
                TB_Input.Text = PW_Input.Password;
                PW_Input.Visibility = Visibility.Collapsed;
                TB_Input.Visibility = Visibility.Visible;
                EyeIcon.Icon = FontAwesome.Sharp.IconChar.Eye;
            }
            else
            {
                PW_Input.Password = TB_Input.Text;
                TB_Input.Visibility = Visibility.Collapsed;
                PW_Input.Visibility = Visibility.Visible;
                EyeIcon.Icon = FontAwesome.Sharp.IconChar.EyeSlash;
            }
        }
    }
}
