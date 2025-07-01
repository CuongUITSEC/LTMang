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
using Learnify.ViewModels;

namespace Learnify.Views.UserControls
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        private HomeViewModel _viewModel;

        public HomeView()
        {
            InitializeComponent();
            _viewModel = new HomeViewModel();
            this.DataContext = _viewModel;
            this.Loaded += HomeView_Loaded;
        }        private void HomeView_Loaded(object sender, RoutedEventArgs e)
        {
            // Kích hoạt tải dữ liệu ranking và reward
            _viewModel.RankingVm.OnViewActivated();
            _viewModel.RewardVm.OnViewActivated();
        }
    }
}
