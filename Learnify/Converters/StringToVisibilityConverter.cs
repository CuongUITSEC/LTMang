using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Learnify.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                bool isVisible = !string.IsNullOrEmpty(stringValue);
                
                // Nếu có parameter "invert", đảo ngược kết quả
                if (parameter != null && parameter.ToString().ToLower() == "invert")
                {
                    isVisible = !isVisible;
                }
                
                return isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
