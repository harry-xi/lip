using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Lip.GUI.Lite.Helpers
{
    public class StringNotEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            bool isNotEmpty = !string.IsNullOrWhiteSpace(str);

            bool invert = false;
            if (parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase))
                invert = true;
            else if (parameter is bool b && b)
                invert = true;

            if (invert)
                isNotEmpty = !isNotEmpty;

            return isNotEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}