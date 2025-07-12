using System;
using System.Globalization;
using System.Windows.Data;

namespace Learnify.Converters
{
    public class TimeToTopConverter : IValueConverter
    {
        private const double PixelsPerHour = 60; // 60 pixel mỗi giờ

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan time)
            {
                return time.TotalHours * PixelsPerHour;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
