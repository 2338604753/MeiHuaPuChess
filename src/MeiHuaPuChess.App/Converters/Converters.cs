using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MeiHuaPuChess.App.Converters;

/// <summary>
/// Side → 棋子文字颜色
/// </summary>
public class SideToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(0xC4, 0x1E, 0x3A));   // 朱红
    private static readonly SolidColorBrush BlackBrush = new(Color.FromRgb(0x1A, 0x1A, 0x2E)); // 墨黑

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Core.Enums.Side side)
        {
            return side == Core.Enums.Side.Red ? RedBrush : BlackBrush;
        }
        return BlackBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// bool → Visibility
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        bool invert = parameter is string s && s == "Invert";
        boolValue = invert ? !boolValue : boolValue;
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Side → "红方" / "黑方"
/// </summary>
public class SideToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Core.Enums.Side side)
        {
            return side == Core.Enums.Side.Red ? "红方" : "黑方";
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
