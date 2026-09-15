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
/// Разделитель — линия в пиксель в обеих ориентациях цвета AxStrokeSubtle, токена
/// разделителей и слабых рамок.
///
/// Цвет остаётся переопределяемым на месте: в полосе окна разделитель стоит
/// на ступень заметнее — AxPressed вместо AxStrokeSubtle.
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

        Assert.Equal(Resource(window, "AxStrokeSubtleColor", variant), Colour(divider.Fill));

        window.Close();
    }

    /// <summary>Цвет переопределяется на месте: в полосе окна он заметнее.</summary>
    [AvaloniaFact]
    public void Colour_set_on_the_spot_wins()
    {
        var (divider, window) = Shown(Orientation.Vertical);

        Assert.True(window.TryFindResource("AxStrokeStrongBrush", ThemeVariant.Dark, out var brush));
        divider.Fill = (IBrush)brush!;
        window.UpdateLayout();

        Assert.Equal(Resource(window, "AxStrokeStrongColor", "Dark"), Colour(divider.Fill));

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
