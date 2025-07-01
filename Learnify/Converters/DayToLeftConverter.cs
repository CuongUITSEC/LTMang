using System;
using System.Globalization;
using System.Windows.Data;

namespace Learnify.Converters
{
    public class DayToLeftConverter : IValueConverter
    {
        private const double ColumnWidth = 100; // chiều rộng mỗi ngày trên Canvas

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DayOfWeek day)
            {
                // Ví dụ: Sunday = 0, Monday = 1 ...
                return (int)day * ColumnWidth;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
