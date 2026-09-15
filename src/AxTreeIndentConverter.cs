using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace ArxisStudio.Themes.Arxis;

/// <summary>
/// Отступ строки дерева по её уровню вложенности.
/// </summary>
/// <remarks>
/// Живёт в теме и снаружи не виден: им пользуется только шаблон дерева. В библиотеке
/// контролов он был публичным, и его числа оказались обещанием плагинам — сдвинуть шаг
/// лестницы нельзя было без старшего номера SDK.
/// </remarks>
internal sealed class AxTreeIndentConverter : IValueConverter
{
    /// <summary>
    /// Ширина одного уровня вложенности.
    /// </summary>
    /// <remarks>
    /// 18: уровни стоят лестницей 8 → 26 → 44, считая от левого края строки. Шаг меньше сбивает лестницу, и на глубине третьего
    /// уровня дерево перестаёт читаться как дерево. Ровно столько же занимает
    /// шеврон с зазором (12 + 6) — потому у строки без шеврона значок и встаёт
    /// в его колонку, а не правее.
    /// </remarks>
    public const double LevelWidth = 18;

    /// <summary>
    /// Отбивка первого уровня от левого края панели.
    /// </summary>
    /// <remarks>
    /// 8 — левая отбивка строки. Живёт здесь,
    /// а не отдельным Margin в шаблоне: у DockPanel строки один отступ на всех,
    /// и складывать его негде.
    /// </remarks>
    public const double BaseIndent = 8;

    /// <summary>Общий экземпляр для шаблонов.</summary>
    public static AxTreeIndentConverter Instance { get; } = new();

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        new Thickness(BaseIndent + (value is int level ? level * LevelWidth : 0), 0, 0, 0);

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
