using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

public class CartesianChartViewModel : INotifyPropertyChanged
{
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
        SelectedMode = "Tuần";
        UpdateChart();
    }

    private void UpdateChart()
    {
        var studyLogs = LoadStudyLogs(); // bạn cần implement

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

    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private List<StudyLog> LoadStudyLogs()
    {
        // TODO: Load từ DB hoặc danh sách mẫu
        return new List<StudyLog>
        {
            new StudyLog { Date = DateTime.Now.AddDays(-1), Hours = 2 },
            new StudyLog { Date = DateTime.Now.AddDays(-3), Hours = 3 },
            new StudyLog { Date = DateTime.Now.AddDays(-10), Hours = 5 },
            new StudyLog { Date = DateTime.Now.AddDays(-20), Hours = 4 },
            new StudyLog { Date = DateTime.Now.AddMonths(-1), Hours = 6 },
            new StudyLog { Date = DateTime.Now.AddMonths(-2), Hours = 7 },
        };
    }
}

public class StudyLog
{
    public DateTime Date { get; set; }
    public double Hours { get; set; }
}
