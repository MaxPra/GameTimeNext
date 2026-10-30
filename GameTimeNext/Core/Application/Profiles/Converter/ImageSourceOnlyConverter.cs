using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GameTimeNext.Core.Application.Profiles.Converter
{
    /// <summary>
    /// Passes an ImageSource on and anything else as null. For bindings that hand a cover on as object (the Tag of the
    /// game case templates): when a list removes its items, WPF sets the placeholder {DisconnectedItem}, and converting
    /// it into an ImageSource throws and logs a binding error for every image binding of every tile (seconds while
    /// reloading the library with a debugger attached, the UI thread and with it the loader animation stood still).
    /// </summary>
    public class ImageSourceOnlyConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value as ImageSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
