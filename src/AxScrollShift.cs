using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ArxisStudio.Themes.Arxis;

/// <summary>
/// Сдвиг шапки таблицы вслед за колонками.
/// </summary>
/// <remarks>
/// Шапка таблицы стоит над областью прокрутки, а не внутри неё: уехав внутрь, она уходила бы
/// вверх вместе со строками и пропадала с глаз на первом же обороте колеса. Но колонки шапки и
/// колонки строк — одна сетка, и стоит таблице поехать вбок, как они расходятся: строки уехали,
/// шапка осталась.
/// <para>
/// Поэтому шапка едет вбок сама — на столько же, на сколько уехало содержимое, — и обрезается по
/// краям своей рамкой. Вертикали в этом сдвиге нет: вверх и вниз шапка не двигается никогда.
/// </para>
/// </remarks>
public sealed class AxScrollShift : IValueConverter
{
    /// <summary>Единственный экземпляр: состояния у преобразователя нет.</summary>
    public static readonly AxScrollShift Horizontal = new();

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Vector offset ? new TranslateTransform(-offset.X, 0) : null;

    /// <inheritdoc/>
    /// <remarks>Шапку не двигают руками: сдвиг ей считает область прокрутки.</remarks>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("Сдвиг шапки обратно в смещение прокрутки не разбирается.");
}
