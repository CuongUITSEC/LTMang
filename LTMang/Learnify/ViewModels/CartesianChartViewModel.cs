using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Windows.Threading;

public class CartesianChartViewModel
{
    public SeriesCollection Series { get; }

    private readonly DispatcherTimer _timer;
    private int _idx;

    public CartesianChartViewModel()
    {
        // Khởi tạo một line series với dữ liệu ban đầu
        var lineSeries = new LineSeries
        {
            Values = new ChartValues<double> { 3, 5, 7, 4 }
        };
        Series = new SeriesCollection { lineSeries };

        // Timer cập nhật real-time
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) =>
        {
            // Thêm giá trị mới (ví dụ sin wave)
            double newVal = Math.Sin(_idx++ * 0.2) * 5 + 5;
            lineSeries.Values.Add(newVal);
            if (lineSeries.Values.Count > 20)
                lineSeries.Values.RemoveAt(0);
        };
        _timer.Start();
    }
}
