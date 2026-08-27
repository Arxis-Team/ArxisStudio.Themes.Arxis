using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Подсказка поднимается над своей поверхностью в обоих вариантах темы.
/// </summary>
/// <remarks>
/// Шаг подъёма в вариантах разный: в светлой подсказка темнеет от белого до
/// AxBg2, в тёмной светлеет от панели до AxBg3. Одним токеном это не
/// выражается, и пока тема брала AxBg2 в обоих, в тёмной подсказка сливалась
/// с панелью, над которой висит.
///
/// Тест спрашивает не имя токена, а результат: подсказка обязана отличаться
/// от той поверхности, над которой всплывает, — и в светлой, и в тёмной.
/// </remarks>
public class TooltipTests
{
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Tooltip_stands_out_from_the_surface_below_it(string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        var window = new Window { RequestedThemeVariant = theme };
        var tip = new ToolTip { Content = "Подсказка" };

        window.Content = new Border { Child = tip };
        window.Show();
        window.UpdateLayout();

        var background = Colour(tip.Background);
        var border = Colour(tip.BorderBrush);

        // Над окном подсказка обязана быть видна как отдельный слой: и заливкой
        // против поверхности под ней, и рамкой против собственной заливки.
        Assert.NotEqual(Resource(window, "AxBg1Color", theme), background);
        Assert.NotEqual(background, border);

        window.Close();
    }

    /// <summary>Значения токенов подсказки — те, что объявляет проект.</summary>
    [AvaloniaTheory]
    [InlineData("AxTooltipBackgroundColor", "Light", "#F7F8FA")]
    [InlineData("AxTooltipBackgroundColor", "Dark", "#393B40")]
    [InlineData("AxTooltipBorderColor", "Light", "#DFE1E5")]
    [InlineData("AxTooltipBorderColor", "Dark", "#4E5157")]
    public void Tooltip_token_matches_the_design_project(string key, string variant, string expected)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        var window = new Window();
        window.Show();

        var colour = Resource(window, key, theme);

        Assert.Equal(expected, $"#{colour.R:X2}{colour.G:X2}{colour.B:X2}");

        window.Close();
    }

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key, ThemeVariant variant)
    {
        Assert.True(window.TryFindResource(key, variant, out var value), key);

        return (Color)value!;
    }
}
