using ArxisStudio.Controls;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Разделитель: линия в пиксель, обе ориентации.
/// </summary>
/// <remarks>
/// Своей карточки у разделителя в проекте нет: его задают строка инвентаря
/// контролов — «линия в пиксель, обе ориентации» — и роль токена AxBrd из
/// раздела 3, «разделители и слабые рамки». Всё, что здесь проверяется, идёт
/// оттуда, а не с витрины.
///
/// Цвет остаётся переопределяемым на месте: в полосе окна карточка «Toolbar»
/// ставит разделитель на ступень заметнее — AxBg4 вместо AxBrd.
/// </remarks>
public class DividerTests
{
    /// <summary>Горизонтальный: пиксель в высоту, тянется по ширине.</summary>
    [AvaloniaFact]
    public void Horizontal_divider_is_a_pixel_tall()
    {
        var (divider, window) = Shown(Orientation.Horizontal);

        Assert.Equal(1d, divider.Bounds.Height);
        Assert.Equal(HorizontalAlignment.Stretch, divider.HorizontalAlignment);
        Assert.True(divider.Bounds.Width > 100, "линия не растянулась по ширине");

        window.Close();
    }

    /// <summary>Вертикальный: пиксель в ширину, тянется по высоте.</summary>
    [AvaloniaFact]
    public void Vertical_divider_is_a_pixel_wide()
    {
        var (divider, window) = Shown(Orientation.Vertical);

        Assert.Equal(1d, divider.Bounds.Width);
        Assert.Equal(VerticalAlignment.Stretch, divider.VerticalAlignment);
        Assert.True(divider.Bounds.Height > 100, "линия не растянулась по высоте");

        window.Close();
    }

    /// <summary>Цвет — токен разделителей и слабых рамок.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Divider_takes_the_separator_token(string variant)
    {
        var (divider, window) = Shown(Orientation.Horizontal, variant);

        Assert.Equal(Resource(window, "AxBrdColor", variant), Colour(divider.Background));

        window.Close();
    }

    /// <summary>Цвет переопределяется на месте: в полосе окна он заметнее.</summary>
    [AvaloniaFact]
    public void Colour_set_on_the_spot_wins()
    {
        var (divider, window) = Shown(Orientation.Vertical);

        Assert.True(window.TryFindResource("AxBg4Brush", ThemeVariant.Dark, out var brush));
        divider.Background = (IBrush)brush!;
        window.UpdateLayout();

        Assert.Equal(Resource(window, "AxBg4Color", "Dark"), Colour(divider.Background));

        window.Close();
    }

    private static (AxDivider Divider, Window Window) Shown(Orientation orientation, string variant = "Dark")
    {
        var divider = new AxDivider { Orientation = orientation };

        var window = new Window
        {
            Width = 240,
            Height = 240,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = divider,
        };

        window.Show();
        window.UpdateLayout();

        return (divider, window);
    }

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key, string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        Assert.True(window.TryFindResource(key, theme, out var value), key);

        return (Color)value!;
    }
}
