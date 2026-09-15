using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace ArxisStudio.Themes.Arxis;

/// <summary>
/// Превращает отступ слева в <see cref="Thickness"/>: шаблону полосы заголовка
/// нужно поле только с одной стороны — под системные кнопки macOS.
/// </summary>
/// <remarks>
/// Живёт в теме и снаружи не виден: им пользуется только шаблон полосы заголовка.
/// </remarks>
internal sealed class AxInsetConverter : IValueConverter
{
    /// <summary>Общий экземпляр для шаблонов.</summary>
    public static AxInsetConverter Instance { get; } = new();

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        new Thickness(value is double inset ? inset : 0, 0, 0, 0);

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
