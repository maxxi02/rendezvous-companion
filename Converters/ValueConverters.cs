using System.Globalization;

namespace rendezvous_companion.Converters;

/// <summary>
/// Returns true when the bound bool is false, and vice versa.
/// Used to show the "Pair" button only when IsPaired = false.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}

/// <summary>
/// Returns 1.0 when the bound bool is true, 0.4 when false.
/// Used to visually grey-out disabled Receipt/Kitchen buttons.
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? 1.0 : 0.4;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
