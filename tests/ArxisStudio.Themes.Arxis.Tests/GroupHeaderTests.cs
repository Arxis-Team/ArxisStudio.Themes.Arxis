using ArxisStudio.Controls;
using ArxisStudio.Icons;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Заголовок группы: подпись, линия до конца строки, стрелка у сворачиваемого — и само
/// сворачивание.
/// </summary>
/// <remarks>
/// Вида три: обычный, сворачиваемый раскрытый со счётчиком и сворачиваемый
/// свёрнутый. Заголовок везде один контрол — в
/// инспекторе группы стоят подряд, и разъезжаться в разметке им нельзя.
///
/// Зазор до линии разный: 10 у обычного и 8 у сворачиваемого, где
/// его задаёт общий зазор ряда, в который попадает и стрелка.
/// <para>
/// Стрелка рисовалась с первой вёрстки, а делала ровно ничего: группу складывал хозяин снаружи, и
/// щелчок по заголовку не делал ничего. Теперь складывает он сам — и мышью, и с клавиатуры. Цель —
/// вся строка, а не стрелка в двенадцать точек: промах здесь ничем не грозит, а попасть в подпись
/// проще, чем в глиф.
/// </para>
/// </remarks>
public class GroupHeaderTests
{
    /// <summary>Подпись — основной текст усиленного начертания базового кегля.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Label_is_the_main_text_in_semibold(string variant)
    {
        var (header, window) = Shown(variant: variant);

        Assert.Equal(Resource(window, "AxTextPrimaryColor", variant), Colour(header.Foreground));
        Assert.Equal(FontWeight.SemiBold, header.FontWeight);
        Assert.Equal(13d, header.FontSize);

        window.Close();
    }

    /// <summary>Линия — пиксель цвета разделителя, с зазором 10 от подписи.</summary>
    /// <remarks>
    /// Ведёт её разделитель, а не рамка: пиксель устройства он меряет сам, и при 150 % линия
    /// остаётся линией, а не кантом в два пикселя.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Rule_runs_to_the_end_of_the_row(string variant)
    {
        var (header, window) = Shown(variant: variant);

        var rule = (AxDivider)Part(header, "PART_Rule");

        Assert.Equal(1d, rule.Bounds.Height);
        Assert.Equal(new Thickness(10, 1, 0, 0), rule.Margin);
        Assert.Equal(Resource(window, "AxStrokeSubtleColor", variant), Colour(rule.Fill));
        Assert.True(rule.Bounds.Width > 100, "линия не дотянулась до конца строки");

        window.Close();
    }

    /// <summary>У обычного заголовка ни стрелки, ни счётчика нет.</summary>
    [AvaloniaFact]
    public void Plain_header_shows_neither_chevron_nor_counter()
    {
        var (header, window) = Shown();

        Assert.False(Part(header, "PART_Chevron").IsVisible);
        Assert.False(Part(header, "PART_Counter").IsVisible);

        window.Close();
    }

    /// <summary>
    /// Стрелка сворачиваемого: мелкая иконка набора в AxTextSecondary, вниз у раскрытого.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Chevron_points_the_way_the_group_stands(string variant)
    {
        var (header, window) = Shown(collapsible: true, variant: variant);

        var chevron = (AxIcon)Part(header, "PART_Chevron");

        Assert.True(chevron.IsVisible);
        Assert.Equal(12d, chevron.Bounds.Width);
        Assert.Equal(Resource(window, "AxTextSecondaryColor", variant), Colour(chevron.Foreground));
        Assert.Same(AxIcons.ChevronDown, chevron.Data);

        header.IsExpanded = false;
        window.UpdateLayout();

        Assert.Same(AxIcons.ChevronRight, chevron.Data);

        window.Close();
    }

    /// <summary>У сворачиваемого зазор до линии — 8, а не 10.</summary>
    [AvaloniaFact]
    public void Collapsible_header_tightens_the_gap_before_the_rule()
    {
        var (header, window) = Shown(collapsible: true);

        Assert.Equal(new Thickness(8, 1, 0, 0), ((AxDivider)Part(header, "PART_Rule")).Margin);

        window.Close();
    }

    /// <summary>Щелчок по заголовку складывает группу и раскладывает обратно.</summary>
    [AvaloniaFact]
    public void A_click_folds_the_group_and_unfolds_it_again()
    {
        var (header, window) = Shown(collapsible: true);

        Assert.True(header.IsExpanded, "группа начинается сложенной");

        Click(header, window);

        Assert.False(header.IsExpanded, "щелчок не сложил группу");

        Click(header, window);

        Assert.True(header.IsExpanded, "щелчок не разложил группу обратно");

        window.Close();
    }

    /// <summary>Пробел и Enter делают то же, что щелчок.</summary>
    [AvaloniaTheory]
    [InlineData(Key.Space, PhysicalKey.Space, " ")]
    [InlineData(Key.Enter, PhysicalKey.Enter, "\r")]
    public void Space_and_enter_fold_the_group(Key key, PhysicalKey physical, string text)
    {
        var (header, window) = Shown(collapsible: true);

        Assert.True(header.Focus(), "заголовок не взял фокус");

        window.UpdateLayout();
        window.KeyPress(key, RawInputModifiers.None, physical, text);
        window.UpdateLayout();

        Assert.False(header.IsExpanded, "клавиша не сложила группу");

        window.Close();
    }

    /// <summary>
    /// Несворачиваемый заголовок фокуса не берёт и на щелчок не отзывается.
    /// </summary>
    /// <remarks>
    /// Остановка Tab у подписи, которая ничего не делает, — лишний шаг на дороге к тому, что
    /// делает.
    /// </remarks>
    [AvaloniaFact]
    public void A_plain_header_is_not_a_stop_on_the_way()
    {
        var (header, window) = Shown();

        Assert.False(header.Focusable, "подпись без дела берёт фокус");

        Click(header, window);

        Assert.True(header.IsExpanded, "щелчок по подписи что-то сложил");

        window.Close();
    }

    /// <summary>
    /// Счётчик виден, только когда задан, и набран вторичным текстом помельче.
    /// </summary>
    /// <remarks>
    /// Начертание у него обычное: подпись уже усилена, и счётчик рядом с ней
    /// таким же весом читался бы как часть имени группы.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Counter_speaks_softer_than_the_label(string variant)
    {
        var (header, window) = Shown(collapsible: true, counter: "3", variant: variant);

        var counter = Part(header, "PART_Counter");

        Assert.True(counter.IsVisible);

        var text = counter.GetVisualDescendants().OfType<TextBlock>().First();

        Assert.Equal(Resource(window, "AxTextSecondaryColor", variant), Colour(text.Foreground));
        Assert.Equal(12d, text.FontSize);
        Assert.Equal(FontWeight.Normal, text.FontWeight);

        window.Close();
    }

    /// <summary>Щёлкает по середине контрола — так, как это делает человек.</summary>
    private static void Click(Control control, Window window)
    {
        var at = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        window.UpdateLayout();
    }

    private static (AxGroupHeader Header, Window Window) Shown(
        bool collapsible = false, object? counter = null, string variant = "Dark")
    {
        var header = new AxGroupHeader
        {
            Content = "Привязки",
            IsCollapsible = collapsible,
            Counter = counter,
        };

        var window = new Window
        {
            Width = 360,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = header,
        };

        window.Show();
        window.UpdateLayout();

        return (header, window);
    }

    private static Control Part(Control control, string name)
    {
        var part = control.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Name == name);

        Assert.True(part is not null, $"в шаблоне нет части {name}");
        return part!;
    }

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key, string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        Assert.True(window.TryFindResource(key, theme, out var value), key);

        return (Color)value!;
    }
}
