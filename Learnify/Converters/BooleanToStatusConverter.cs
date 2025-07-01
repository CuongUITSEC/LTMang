using System;
using System.Globalization;
using System.Windows.Data;

namespace Learnify.Converters
{
    public class BooleanToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isOnline)
            {
                return isOnline ? "Trực tuyến" : "Ngoại tuyến";
            }
            return "Ngoại tuyến";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
