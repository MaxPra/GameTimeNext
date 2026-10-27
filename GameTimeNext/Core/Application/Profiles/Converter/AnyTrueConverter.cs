using System.Globalization;
using System.Windows.Data;

namespace GameTimeNext.Core.Application.Profiles.Converter
{
    /// <summary>
    /// True when at least one of the bound values is true (logical OR, e.g. hovered or selected).
    /// </summary>
    public class AnyTrueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Any(v => v is bool b && b);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
