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
                var grouped = studyLogs
                    .GroupBy(log => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                        log.Date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday))
                    .OrderBy(g => g.Key);

                Labels = grouped.Select(g => $"Tuần {g.Key}").ToList();
                var values = grouped.Select(g => g.Sum(log => log.Hours)).ToList();

                Series = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Giờ học",
                        Values = new ChartValues<double>(values)
                    }
                };
            }
            else if (SelectedMode == "Tháng")
            {
                var grouped = studyLogs
                    .GroupBy(log => log.Date.Month)
                    .OrderBy(g => g.Key);

                Labels = grouped.Select(g => $"Tháng {g.Key}").ToList();
                var values = grouped.Select(g => g.Sum(log => log.Hours)).ToList();

                Series = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Giờ học",
                        Values = new ChartValues<double>(values)
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
