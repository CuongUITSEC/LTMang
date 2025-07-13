using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Threading.Tasks;
using Learnify.Services; 


public class PieChartViewModel
{
    public SeriesCollection PieSeries { get; set; }

    public PieChartViewModel()
    {
        PieSeries = new SeriesCollection
        {
            new PieSeries
            {
                Title = "Thời gian học hôm nay",
                Values = new ChartValues<double> { 0 },
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#06AAE9")),
                DataLabels = true,
                LabelPoint = point => $"{Math.Round(point.Y, 1)} phút"
            },
            new PieSeries
            {
                Title = "Thời gian còn lại",
                Values = new ChartValues<double> { 1440 },
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBB03B")),
                DataLabels = true,
                LabelPoint = point => $"{Math.Round(point.Y, 1)} phút"
            }
        };
    }

    public async Task UpdateTodayPieAsync()
    {
        var userId = AuthService.GetUserId();
        var studyTimeData = await new FirebaseService().GetStudyTimeDataAsync(userId);
        
        // Sử dụng StudyTimeService thống nhất
        double todayMinutes = StudyTimeService.CalculateTodayStudyTime(studyTimeData?.Sessions);

        // Cập nhật giá trị cho biểu đồ
        PieSeries[0].Values[0] = todayMinutes;
        PieSeries[1].Values[0] = Math.Max(0, 1440 - todayMinutes); // 1440 phút = 24 giờ
    }
}
