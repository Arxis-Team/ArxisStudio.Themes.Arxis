using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace ArxisStudio.Themes.Arxis;

/// <summary>
/// Отступ строки дерева: уровень вложенности, умноженный на шаг лестницы.
/// </summary>
/// <remarks>
/// Живёт в теме и снаружи не виден: им пользуется только шаблон дерева. В библиотеке
/// контролов он был публичным, и его числа оказались обещанием плагинам — сдвинуть шаг
/// лестницы нельзя было без старшего номера SDK.
/// <para>
/// Своих чисел у него больше нет. Шаг приходит вторым значением, и берётся он у самой
/// клетки шеврона: её ширина — <c>AxTreeIndent</c>, ключ плотности. Пока шаг был
/// константой здесь, лестница не шла за плотностью вовсе, а число жило в двух местах —
/// в коде и в ширине колонки, — и разъехаться им было нечем помешать.
/// </para>
/// </remarks>
internal sealed class AxTreeIndentConverter : IMultiValueConverter
{
    /// <summary>Общий экземпляр для шаблонов.</summary>
    public static AxTreeIndentConverter Instance { get; } = new();

    /// <inheritdoc/>
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var level = values.Count > 0 && values[0] is int depth ? depth : 0;
        var step = values.Count > 1 && values[1] is double width && !double.IsNaN(width) ? width : 0d;

        return new Thickness(level * step, 0, 0, 0);
    }
}
