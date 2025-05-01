using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;

public class HomeViewModel
{
    public SeriesCollection PieSeries { get; set; }

    public HomeViewModel()
    {
        PieSeries = new SeriesCollection
        {
            new PieSeries
            {
                Title = "Số giờ học",
                Values = new ChartValues<double> { 60 },
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#06AAE9")), // Màu hồng
                DataLabels = true, // BẬT hiển thị nhãn
                LabelPoint = point => $"{point.Y}%" // Hiển thị giá trị phần trăm
                
            },
            new PieSeries
            {
                Title = "Thời gian còn lại",
                Values = new ChartValues<double> { 40 },
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBB03B")), // Màu hồng
                DataLabels = true, // BẬT hiển thị nhãn
                LabelPoint = point => $"{point.Y}%" // Hiển thị giá trị phần trăm
            }
        };
    }
}
