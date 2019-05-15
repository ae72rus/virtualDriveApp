using System;
using System.Globalization;
using System.Windows.Data;
using DemoApp.Enums;

namespace DemoApp.ViewData.Converters
{
    public class EntityTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is EntityType type))
                return null;

            return type == EntityType.Directory ? "[_]" : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}