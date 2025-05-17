using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Learnify.Converters
{
    public class BooleanToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Nếu là thông báo chưa đọc (IsRead = false) -> màu nền nhạt
            // Nếu đã đọc (IsRead = true) -> trong suốt
            return (bool)value ? Brushes.Transparent : new SolidColorBrush(Color.FromArgb(30, 0, 120, 215));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}