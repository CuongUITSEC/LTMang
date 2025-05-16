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
    /// Interaction logic for TextBox_PlaceHoder.xaml
    /// </summary>
    public partial class TextBox_PlaceHoder : UserControl
    {
        public TextBox_PlaceHoder()
        {
            InitializeComponent();
        }

        private string PlaceHoder;

        public string Place_Hoder
        {
            get { return PlaceHoder; }
            set { PlaceHoder = value;
                TB_PlaceHoder.Text = PlaceHoder;
            }
        }


        private void Input_TextChanged(object sender, TextChangedEventArgs e)
        {
            Text = TB_Input.Text;

            if (string.IsNullOrEmpty(TB_Input.Text))
            {
                TB_PlaceHoder.Visibility = Visibility.Visible;
            }
            else
            {
                TB_PlaceHoder.Visibility = Visibility.Hidden;
            }
        }

        public static readonly DependencyProperty TextProperty =
    DependencyProperty.Register(
        "Text",
        typeof(string),
        typeof(TextBox_PlaceHoder),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as TextBox_PlaceHoder;
            if (control != null)
            {
                control.TB_Input.Text = e.NewValue as string;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TB_Input.Clear();
        }
    }
}
