using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Input;

namespace ArxisStudio.Themes.Arxis;

/// <summary>
/// Жест команды — словами той платформы, на которой его читают.
/// </summary>
/// <remarks>
/// <c>KeyGesture.ToString()</c> отдаёт инвариантную запись: <c>Ctrl+Shift+P</c> — и на macOS, где
/// той же командой правит <c>⌘⇧P</c>, а клавиши с именем Ctrl под пальцем нет вовсе. Шаблон меню
/// показывал именно её, потому что брал жест привязкой к строке. Формат <c>"p"</c> — тот самый,
/// который <c>KeyGesture</c> держит для платформы: на macOS он собирает строку из знаков
/// модификаторов, на остальных оставляет имена.
/// <para>
/// Преобразователь живёт в сборке темы, а не в наборе контролов: это часть показа, и плагину его
/// не обещали.
/// </para>
/// </remarks>
public sealed class AxGestureText : IValueConverter
{
    /// <summary>Единственный экземпляр: состояния у преобразователя нет.</summary>
    public static readonly AxGestureText Platform = new();

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is KeyGesture gesture ? gesture.ToString("p", culture) : value?.ToString();

    /// <inheritdoc/>
    /// <remarks>Обратно жест не разбирается: подпись только показывают.</remarks>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("Жест из подписи не собирается: преобразование одностороннее.");
}
