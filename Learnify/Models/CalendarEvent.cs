using System;
using System.Windows.Media;

namespace Learnify.Models
{
    public class CalendarEvent
    {
        public string Title { get; set; } // Thêm dòng này
        public DayOfWeek Day { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public Brush Color { get; set; }
    }

}