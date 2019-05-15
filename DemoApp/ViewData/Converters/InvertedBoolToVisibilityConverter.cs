using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DemoApp.ViewData.Converters
{
    public class InvertedBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool isNotVisible))
                return Visibility.Collapsed;

            return isNotVisible ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Visibility visibility))
                return false;

            return visibility == Visibility.Collapsed || visibility == Visibility.Hidden;
        }
    }
}