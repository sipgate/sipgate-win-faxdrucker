using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SipgateFaxdrucker.Converter
{
    public class BooleanToInvertedVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolToBeInverted)
            {
                return boolToBeInverted ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}