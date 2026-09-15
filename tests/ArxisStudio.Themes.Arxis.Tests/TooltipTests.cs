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
/// AxSurfacePanel, в тёмной светлеет от панели до AxSurfaceRaised. Одним токеном это не
/// выражается, и пока тема брала AxSurfacePanel в обоих, в тёмной подсказка сливалась
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
        Assert.NotEqual(Resource(window, "AxSurfaceBaseColor", theme), background);
        Assert.NotEqual(background, border);

        window.Close();
    }

    /// <summary>
    /// Над панелью подсказку очерчивает рамка.
    /// </summary>
    /// <remarks>
    /// В светлой теме заливка подсказки — цвет панели, и над панелью подсказка отделяется
    /// только рамкой и тенью. Рамка цвета панели оставила бы от неё одну тень.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Tooltip_border_stands_out_from_the_panel(string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        var window = new Window();
        window.Show();

        Assert.NotEqual(Resource(window, "AxSurfacePanelColor", theme), Resource(window, "AxToolTipStrokeColor", theme));

        window.Close();
    }

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key, ThemeVariant variant)
    {
        Assert.True(window.TryFindResource(key, variant, out var value), key);

        return (Color)value!;
    }
}
