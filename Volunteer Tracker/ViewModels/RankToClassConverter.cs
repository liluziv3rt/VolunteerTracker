using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Volunteer_Tracker.ViewModels
{
    public class RankToClassConverter : IValueConverter
    {
        // Синглтон, чтобы можно было удобно использовать в XAML
        public static RankToClassConverter Instance { get; } = new RankToClassConverter();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // value — это ранг (int)
            // parameter — это строка "1", "2" или "3"
            if (value is int rank && parameter is string targetRank)
            {
                return rank.ToString() == targetRank;
            }

            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}