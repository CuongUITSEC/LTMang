using System;
using System.Globalization;
using System.Windows.Data;

namespace Learnify.Converters
{
    public class StatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status.ToLower())
                {
                    case "online":
                        return "Trực tuyến";
                    case "offline":
                        return "Ngoại tuyến";
                    case "away":
                        return "Vắng mặt";
                    case "busy":
                        return "Bận";
                    default:
                        return "Ngoại tuyến";
                }
            }
            return "Ngoại tuyến";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
