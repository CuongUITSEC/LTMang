using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Learnify.Services;
using System.Diagnostics;
using Learnify.Models;

public class CartesianChartViewModel : INotifyPropertyChanged
{
    private readonly FirebaseService _firebaseService;

    public event PropertyChangedEventHandler PropertyChanged;

    public SeriesCollection Series { get; set; }
    public List<string> Labels { get; set; }

    private string _selectedMode;
    public string SelectedMode
    {
        get => _selectedMode;
        set
        {
            _selectedMode = value;
            OnPropertyChanged(nameof(SelectedMode));
            UpdateChart();
        }
    }

    public List<string> Modes { get; set; } = new List<string> { "Tuần", "Tháng" };

    public CartesianChartViewModel()
    {
        _firebaseService = new FirebaseService();
        SelectedMode = "Tuần";
        UpdateChart();
    }

    private List<DateTime> GetLastNDates(int n)
    {
        var today = DateTime.Today;
        return Enumerable.Range(0, n)
            .Select(i => today.AddDays(-n + 1 + i))
            .ToList();
    }

    private async void UpdateChart()
    {
        try
        {
            var userId = AuthService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                Debug.WriteLine("User not authenticated");
                return;
            }

            var studyLogs = await _firebaseService.GetUserStudyLogsAsync(userId);

            if (SelectedMode == "Tuần")
            {
                var weekDates = GetLastNDates(7);
                Labels = weekDates.Select(d => d.ToString("dd/MM")).ToList();
                var values = weekDates.Select(day =>
                {
                    var log = studyLogs.FirstOrDefault(l => l.Date.Date == day.Date);
                    return log != null ? log.Hours : 0;
                }).ToList();

                Series = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Giờ học",
                        Values = new ChartValues<double>(values),
                        DataLabels = false,
                        LabelPoint = point => FormatTime(point.Y)
                    }
                };
            }
            else if (SelectedMode == "Tháng")
            {
                var monthDates = GetLastNDates(30);
                Labels = monthDates.Select(d => d.ToString("dd/MM")).ToList();
                var values = monthDates.Select(day =>
                {
                    var log = studyLogs.FirstOrDefault(l => l.Date.Date == day.Date);
                    return log != null ? log.Hours : 0;
                }).ToList();

                Series = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Giờ học",
                        Values = new ChartValues<double>(values),
                        DataLabels = false,
                        LabelPoint = point => FormatTime(point.Y)
                    }
                };
            }

            OnPropertyChanged(nameof(Series));
            OnPropertyChanged(nameof(Labels));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating chart: {ex.Message}");
        }
    }

    private string FormatTime(double hours)
    {
        var totalSeconds = (int)(hours * 3600);
        var ts = TimeSpan.FromSeconds(totalSeconds);
        return $"{(int)ts.TotalHours} giờ {ts.Minutes} phút {ts.Seconds} giây";
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class StudyLog
{
    public DateTime Date { get; set; }
    public double Hours { get; set; }
}
