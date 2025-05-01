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
using LiveCharts.Wpf;
using LiveCharts;

namespace Learnify.Views
{
    /// <summary>
    /// Interaction logic for MainViewControl.xaml
    /// </summary>
    public partial class MainViewControl : UserControl
    {
        public SeriesCollection PieValues { get; set; }
        public Brush PieFill { get; set; }

        public MainViewControl()
        {
            InitializeComponent();

            PieValues = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "A",
                    Values = new ChartValues<double> { 3 },
                    Fill = Brushes.Red,
                    DataLabels = true
                },
                new PieSeries
                {
                    Title = "B",
                    Values = new ChartValues<double> { 7 },
                    Fill = Brushes.Blue,
                    DataLabels = true
                }
            };


            //PieFill = Brushes.Red; // Không cần nếu dùng PieSeries trực tiếp như trên

            DataContext = this;
        }
    }

}
