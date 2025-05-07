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

namespace Learnify.Views.UserControls
{
    public class CalendarEvent
    {
        public string Title { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public Brush Color { get; set; }
    }
    /// <summary>
    /// Interaction logic for TableTime.xaml
    /// </summary>
    public partial class TableTime : UserControl
    {
        private const int StartHour = 0;
        private const int EndHour = 24;
        private const int HourHeight = 60;
        private const int DayWidth = 100;

        public TableTime()
        {
            InitializeComponent();
            DrawGrid();

            var sampleEvents = new List<CalendarEvent>
            {
                new CalendarEvent
                {
                    Title = "TH Nhập môn mạng máy tính",
                    Day = DayOfWeek.Monday,
                    StartTime = new TimeSpan(7, 30, 0),
                    EndTime = new TimeSpan(11, 30, 0),
                    Color = Brushes.Red
                },
                new CalendarEvent
                {
                    Title = "Nhập môn mạng máy tính",
                    Day = DayOfWeek.Monday,
                    StartTime = new TimeSpan(13, 0, 0),
                    EndTime = new TimeSpan(15, 0, 0),
                    Color = Brushes.Green
                }
            };

            DrawEvents(sampleEvents);
        }

        private void DrawGrid()
        {
            CalendarCanvas.Children.Clear();
            DayHeaderCanvas.Children.Clear();

            // Vẽ khung giờ (trong phần cuộn)
            for (int hour = StartHour; hour <= EndHour; hour++)
            {
                double y = (hour - StartHour) * HourHeight;

                // Nhãn giờ
                var timeLabel = new TextBlock
                {
                    Text = $"{hour}:00",
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(5)
                };
                Canvas.SetTop(timeLabel, y);
                Canvas.SetLeft(timeLabel, 0);
                CalendarCanvas.Children.Add(timeLabel);

                // Đường kẻ ngang
                var line = new Line
                {
                    X1 = 50,
                    X2 = 800,
                    Y1 = y,
                    Y2 = y,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1
                };
                CalendarCanvas.Children.Add(line);
            }

            // Vẽ tiêu đề các ngày (trong phần cố định)
            for (int i = 0; i < 7; i++)
            {
                string dayName = ((DayOfWeek)i).ToString();

                var dayLabel = new TextBlock
                {
                    Text = dayName,
                    FontWeight = FontWeights.Bold,
                    FontSize = 16, // 👈 Tăng kích thước chữ ở đây
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Canvas.SetTop(dayLabel, 5);
                Canvas.SetLeft(dayLabel, 60 + i * DayWidth);
                DayHeaderCanvas.Children.Add(dayLabel);
            }
            DayHeaderCanvas.Height = 40;
            DayHeaderCanvas.Width = 60 + 7 * DayWidth; // Ví dụ: 60 + 7 * 90 = 690
            CalendarCanvas.Width = 60 + 7 * DayWidth;
            CalendarCanvas.Height = (EndHour - StartHour) * HourHeight; // Ví dụ: 24 * 60 = 1440
        }


        private void DrawEvents(List<CalendarEvent> events)
        {
            foreach (var ev in events)
            {
                int dayIndex = (int)ev.Day;

                double top = (ev.StartTime.TotalHours - StartHour) * HourHeight;
                double height = (ev.EndTime - ev.StartTime).TotalHours * HourHeight;
                double left = 60 + dayIndex * DayWidth;

                var rect = new Border
                {
                    Width = DayWidth - 10,
                    Height = height,
                    Background = ev.Color,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Child = new TextBlock
                    {
                        Text = ev.Title,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = Brushes.White,
                        Margin = new Thickness(4)
                    }
                };

                Canvas.SetTop(rect, top);
                Canvas.SetLeft(rect, left);
                CalendarCanvas.Children.Add(rect);
            }
        }

    }
    public class ScheduleItem
    {
        public string Day { get; set; }
        public string TimeSlot { get; set; }
        public string Subject { get; set; }

        public ScheduleItem(string day, string timeSlot, string subject)
        {
            Day = day;
            TimeSlot = timeSlot;
            Subject = subject;
        }
    }
}
