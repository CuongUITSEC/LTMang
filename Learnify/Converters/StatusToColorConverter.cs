using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Learnify.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status.ToLower())
                {
                    case "online":
                    case "trực tuyến":
                        return new SolidColorBrush(Colors.Green);
                    case "offline":
                    case "ngoại tuyến":
                        return new SolidColorBrush(Colors.Gray);
                    case "away":
                    case "vắng mặt":
                        return new SolidColorBrush(Colors.Orange);
                    default:
                        return new SolidColorBrush(Colors.Gray);
                }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
