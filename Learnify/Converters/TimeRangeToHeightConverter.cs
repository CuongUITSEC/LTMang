using System;
using System.Globalization;
using System.Windows.Data;

namespace Learnify.Converters
{
    public class TimeRangeToHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is TimeSpan end && values[1] is TimeSpan start)
            {
                return (end - start).TotalHours * 60;
            }
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
