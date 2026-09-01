using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Метрики и дробный масштаб экрана.
/// </summary>
/// <remarks>
/// Правило проекта: длины в DIP и кратны 2 — «целые пиксели при 125 % и
/// 150 %». Половина этого обещания арифметически не выполнима: при 125 %
/// целый пиксель даёт кратность четырём, а не двум (2 × 1,25 = 2,5).
/// Резкость краёв на дробном масштабе на самом деле держит округление
/// раскладки Avalonia, а кратность двум гарантирует ровно одно — точные
/// пропорции при 150 %. Тест закрепляет это: каждая длина чётная, и три
/// исключения, которые ввела сама спецификация, названы поимённо, чтобы
/// новое исключение нельзя было добавить молча.
/// </remarks>
public class MetricsScaleTests
{
    /// <summary>Длины раскладки: высоты, ширины, размеры и радиусы.</summary>
    public static TheoryData<string> Lengths =>
    [
        "AxControlHeight",
        "AxControlHeightCompact",
        "AxRowHeight",
        "AxButtonMinWidth",
        "AxButtonMinWidthCompact",
        "AxCheckboxSize",
        "AxIconSize",
        "AxIconSizeSmall",
        "AxScrollBarLane",
        "AxScrollThumbSize",
        "AxScrollThumbSizeHover",
        "AxToggleWidth",
        "AxFocusOutlineWidth",
    ];

    /// <summary>
    /// Длины, которые спецификация задала нечётными и назвала явно: размер
    /// тумблера и зазор до подписи из раздела 5, решение «взять 30 × 17» из
    /// раздела 6. Менять их — правка спецификации, а не темы.
    /// </summary>
    public static TheoryData<string, double> MandatedOdd =>
        new() { { "AxToggleHeight", 17 }, { "AxToggleKnobSize", 13 }, { "AxCheckboxGap", 5 } };

    [AvaloniaTheory]
    [MemberData(nameof(Lengths))]
    public void Layout_lengths_are_even(string key)
    {
        var window = new Window();
        window.Show();

        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value));
        var length = Assert.IsType<double>(value);

        Assert.Equal(length, Math.Round(length));
        Assert.True(length % 2 == 0, $"{key} = {length}: длина раскладки должна быть чётной");

        // При 150 % чётная длина обязана дать целое число пикселей.
        Assert.Equal(length * 1.5, Math.Round(length * 1.5));

        window.Close();
    }

    [AvaloniaTheory]
    [MemberData(nameof(MandatedOdd))]
    public void Odd_lengths_are_the_ones_the_specification_named(string key, double expected)
    {
        var window = new Window();
        window.Show();

        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value));
        Assert.Equal(expected, value);

        window.Close();
    }

    /// <summary>
    /// Округление раскладки включено.
    /// </summary>
    /// <remarks>
    /// Именно оно, а не кратность двум, держит края резкими на дробном
    /// масштабе: Avalonia сажает границы на целый пиксель устройства. Свойство
    /// включено по умолчанию, и выключить его где-то в теме или разметке —
    /// значит получить размытые рамки при 125 %, ничего больше не сломав.
    /// Тест ловит такое выключение.
    /// </remarks>
    [AvaloniaFact]
    public void Layout_rounding_stays_on()
    {
        var button = new ArxisStudio.Controls.AxButton { Content = "Проверка" };
        var window = new Window { Content = button };
        window.Show();

        Assert.True(window.UseLayoutRounding);
        Assert.True(button.UseLayoutRounding);

        window.Close();
    }

    /// <summary>
    /// Отступы контролов закреплены значениями компонентов проекта.
    /// </summary>
    /// <remarks>
    /// Здесь проект спорит сам с собой. Таблица раздела 5 даёт 12,6 · 9,4 · 6,2,
    /// а компоненты — 0 12px · 0 9px · 0 6px 0 9px: вертикального отступа у них
    /// нет вовсе, текст стоит по центру фиксированной высоты 28.
    ///
    /// Прав компонент, и не по старшинству, а по проверке. Высота 28 задана
    /// жёстко: с отступом 6 сверху и снизу тексту остаётся 16, а строке 13-го
    /// кегля нужно больше — нижний край букв срезался, что и было видно на
    /// кнопках. Ноль отдаёт всю высоту, и текст встаёт по центру, как в
    /// компоненте. Комбобокс той же таблицей был разобран раньше и тоже в
    /// пользу компонента.
    ///
    /// Похоже, строки этой таблицы списаны с темы, а не написаны: история
    /// изменений так и говорит — «0.3, значения сверены с Metrics.axaml».
    /// Тогда таблица закрепила ошибку темы, а не задала значение.
    ///
    /// Горизонтальные 9 нечётны, и при 150 % это полпикселя — то же
    /// противоречие правила «кратно 2» с собственными значениями проекта.
    /// Округление раскладки половину съедает, край остаётся резким, но отступ
    /// слева и справа может разойтись на пиксель. Правку решает владелец зоны
    /// «Основы»: 8 вместо 9 закрыло бы вопрос.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("AxButtonPadding", 12, 0, 12, 0)]
    [InlineData("AxTextFieldPadding", 9, 0, 9, 0)]
    [InlineData("AxComboBoxPadding", 9, 0, 6, 0)]
    public void Paddings_keep_the_values_of_the_specification(
        string key, double left, double top, double right, double bottom)
    {
        var window = new Window();
        window.Show();

        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value));
        Assert.Equal(new Thickness(left, top, right, bottom), value);

        window.Close();
    }
}
