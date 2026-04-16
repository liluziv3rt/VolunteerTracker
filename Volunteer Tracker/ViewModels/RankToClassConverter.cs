using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Volunteer_Tracker.ViewModels
{
    public class RankToClassConverter : IValueConverter
    {
        public static RankToClassConverter Instance { get; } = new RankToClassConverter();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
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