using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Globalization;
using LiveCharts;
using LiveCharts.Wpf;
using Learnify.Services;
using System.Diagnostics;
using Learnify.Models;

namespace Learnify.ViewModels
{
    public class AnalystViewModel : ViewModelBase
    {
        private readonly FirebaseService _firebaseService;

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

        public AnalystViewModel()
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
        }        private async void UpdateChart()
        {
            try
            {
                var userId = AuthService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    Debug.WriteLine("[ANALYST] User not authenticated");
                    return;
                }

                Debug.WriteLine($"[ANALYST] Loading data for user: {userId}");

                // Lấy dữ liệu từ studyTime (cấu trúc chính xác theo JSON)
                var studyTimeData = await _firebaseService.GetStudyTimeDataAsync(userId);

                Debug.WriteLine($"[ANALYST] StudyTimeData: {studyTimeData?.TotalMinutes ?? 0} total minutes, {studyTimeData?.Sessions?.Count ?? 0} sessions");
                
                if (studyTimeData?.Sessions != null)
                {
                    Debug.WriteLine("[ANALYST] Session details:");
                    foreach (var session in studyTimeData.Sessions.Take(5)) // Log first 5 sessions
                    {
                        Debug.WriteLine($"  - {session.Timestamp}: {session.Duration} minutes");
                    }
                }

                if (SelectedMode == "Tuần")
                {
                    var weekDates = GetLastNDates(7);
                    Labels = weekDates.Select(d => d.ToString("dd/MM")).ToList();
                    
                    var values = new List<double>();
                    
                    foreach (var day in weekDates)
                    {
                        // Sử dụng StudyTimeService thống nhất
                        double minutesForDay = StudyTimeService.CalculateStudyTimeForDate(studyTimeData?.Sessions, day);
                        
                        Debug.WriteLine($"[ANALYST] {day:dd/MM}: Total {minutesForDay} minutes");
                        values.Add(minutesForDay);
                    }

                    Debug.WriteLine($"[ANALYST] Week total: {values.Sum()} minutes across 7 days");

                    Series = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = "Phút học",
                            Values = new ChartValues<double>(values),
                            DataLabels = false,
                            LabelPoint = point => FormatMinutes(point.Y)
                        }
                    };
                }
                else if (SelectedMode == "Tháng")
                {
                    var monthDates = GetLastNDates(30);
                    Labels = monthDates.Select(d => d.ToString("dd/MM")).ToList();
                    
                    var values = new List<double>();
                    
                    foreach (var day in monthDates)
                    {
                        // Sử dụng StudyTimeService thống nhất
                        double minutesForDay = StudyTimeService.CalculateStudyTimeForDate(studyTimeData?.Sessions, day);
                        
                        values.Add(minutesForDay);
                    }

                    Debug.WriteLine($"[ANALYST] Month total: {values.Sum()} minutes across 30 days");

                    Series = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = "Phút học",
                            Values = new ChartValues<double>(values),
                            DataLabels = false,
                            LabelPoint = point => FormatMinutes(point.Y)
                        }
                    };
                }

                OnPropertyChanged(nameof(Series));
                OnPropertyChanged(nameof(Labels));
                
                Debug.WriteLine($"[ANALYST] Chart updated successfully for mode: {SelectedMode}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ANALYST] Error updating chart: {ex.Message}");
                Debug.WriteLine($"[ANALYST] StackTrace: {ex.StackTrace}");
            }
        }

        private string FormatMinutes(double minutes)
        {
            int min = (int)minutes;
            int h = min / 60;
            int m = min % 60;
            if (h > 0)
                return $"{h} giờ {m} phút";
            return $"{m} phút";
        }
    }
}
