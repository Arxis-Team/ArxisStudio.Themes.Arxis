using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Заголовок группы: подпись, линия до конца строки и стрелка у сворачиваемого.
/// </summary>
/// <remarks>
/// Карточка «Group Header» показывает три: обычный, сворачиваемый раскрытый со
/// счётчиком и сворачиваемый свёрнутый. Заголовок везде один контрол — в
/// инспекторе группы стоят подряд, и разъезжаться в разметке им нельзя.
///
/// Зазор до линии в карточке разный: 10 у обычного и 8 у сворачиваемого, где
/// его задаёт общий зазор ряда, в который попадает и стрелка.
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

        Assert.Equal(Resource(window, "AxFgColor", variant), Colour(header.Foreground));
        Assert.Equal(FontWeight.SemiBold, header.FontWeight);
        Assert.Equal(13d, header.FontSize);

        window.Close();
    }

    /// <summary>Линия — пиксель цвета разделителя, с зазором 10 от подписи.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Rule_runs_to_the_end_of_the_row(string variant)
    {
        var (header, window) = Shown(variant: variant);

        var rule = (Border)Part(header, "PART_Rule");

        Assert.Equal(1d, rule.Bounds.Height);
        Assert.Equal(new Thickness(10, 1, 0, 0), rule.Margin);
        Assert.Equal(Resource(window, "AxBrdColor", variant), Colour(rule.Background));
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
    /// Стрелка сворачиваемого: мелкая иконка набора в AxFg2, вниз у раскрытого.
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
        Assert.Equal(Resource(window, "AxFg2Color", variant), Colour(chevron.Foreground));
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

        Assert.Equal(new Thickness(8, 1, 0, 0), ((Border)Part(header, "PART_Rule")).Margin);

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

        Assert.Equal(Resource(window, "AxFg2Color", variant), Colour(text.Foreground));
        Assert.Equal(11.5, text.FontSize);
        Assert.Equal(FontWeight.Normal, text.FontWeight);

        window.Close();
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
