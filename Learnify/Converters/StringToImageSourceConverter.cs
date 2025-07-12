using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Learnify.Converters
{
    public class StringToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrEmpty(path))
            {
                try
                {
                    // Nếu là đường dẫn tuyệt đối hoặc URI
                    if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
                        return new BitmapImage(new Uri(path, UriKind.Absolute));

                    // Nếu là đường dẫn tương đối trong project
                    var absPath = path;
                    if (!Path.IsPathRooted(path))
                        absPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path.TrimStart('/', '\\'));
                    if (File.Exists(absPath))
                        return new BitmapImage(new Uri(absPath, UriKind.Absolute));

                    // Nếu là resource pack URI
                    var packUri = $"pack://application:,,,/{path.TrimStart('/')}";
                    return new BitmapImage(new Uri(packUri));
                }
                catch { }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
