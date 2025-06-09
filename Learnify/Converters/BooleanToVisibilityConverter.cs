using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Learnify.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {                if (parameter != null && (parameter.ToString() == "invert" || parameter.ToString() == "inverse"))
                {
                    boolValue = !boolValue;
                }
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool result = visibility == Visibility.Visible;                if (parameter != null && (parameter.ToString() == "invert" || parameter.ToString() == "inverse"))
                {
                    result = !result;
                }
                return result;
            }
            return false;
        }
    }
}
