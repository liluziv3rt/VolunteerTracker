using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Volunteer_Tracker.Converters
{
    public class BoolToActiveTabBgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? new SolidColorBrush(Color.Parse("#E8F0FE"))
                                 : new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToActiveTabFgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? new SolidColorBrush(Color.Parse("#1A73E8"))
                                 : new SolidColorBrush(Color.Parse("#5F6368"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}