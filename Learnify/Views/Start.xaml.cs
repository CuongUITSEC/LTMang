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

namespace Learnify.Views
{
    /// <summary>
    /// Interaction logic for Start.xaml
    /// </summary>
    public partial class Start : Window
    {
        public Start()
        {
            InitializeComponent();
            this.DataContext = new Learnify.ViewModels.Login.StartViewModel();
        }
        private void OpenMainView()
        {
            var mainView = new MainView(); 
            mainView.Show();
            this.Close();
        }
    }
}
