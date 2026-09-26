using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Input.Platform;

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

    // Имя клавиши, которое платформа оставила перечислению: Enter и Return у Avalonia — одно значение.
    private static readonly Dictionary<Key, string> EnterName = new() { [Key.Enter] = "Enter" };

    /// <summary>Жест словами платформы — так его пишут меню, палитра и страница клавиш.</summary>
    /// <param name="gesture">Жест.</param>
    /// <param name="provider">Откуда брать запись платформы; null — у самой платформы.</param>
    /// <remarks>
    /// Поправка к платформе одна — имя Enter. <c>Key.Enter</c> и <c>Key.Return</c> у Avalonia одно
    /// значение, и клавишу, для которой у платформы нет своего слова, она пишет именем перечисления:
    /// «Return». Так клавиша подписана только у Mac, а там у платформы своя запись — знак. На
    /// клавиатурах Windows и Linux написано Enter, и так же её пишут Visual Studio и Rider.
    /// </remarks>
    public static string Write(KeyGesture gesture, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(gesture);

        var platform = KeyGestureFormatInfo.GetInstance(provider);

        if (gesture.Key != Key.Enter || platform.FormatKey(Key.Enter) != nameof(Key.Return))
            return gesture.ToString("p", platform);

        var named = new KeyGestureFormatInfo(EnterName, platform.Meta, platform.Ctrl, platform.Alt, platform.Shift);

        return gesture.ToString("p", named);
    }

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is KeyGesture gesture ? Write(gesture, culture) : value?.ToString();

    /// <inheritdoc/>
    /// <remarks>Обратно жест не разбирается: подпись только показывают.</remarks>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("Жест из подписи не собирается: преобразование одностороннее.");
}
