using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Рамки сообщений стоят на ступенях шкал, а не на своих цветах.
/// </summary>
/// <remarks>
/// Рамка баннера закрыта не цветом, а <b>ступенью шкалы</b>: AxBlue10 в
/// светлой и AxBlue3 в тёмной, и так все четыре. Цвет, взятый мимо шкалы,
/// разошёлся бы с ней, а на шкале держится весь набор. Тест закрепляет
/// правило, а не снимок.
/// </remarks>
public class ScaleStepTests
{
    [AvaloniaTheory]
    [InlineData("AxInfoBorder", "AxBlue10", "AxBlue3")]
    [InlineData("AxSuccessBorder", "AxGreen9", "AxGreen4")]
    [InlineData("AxWarningBorder", "AxYellow6", "AxYellow4")]
    [InlineData("AxErrorBorder", "AxRed9", "AxRed4")]
    public void Token_sits_on_its_scale_step(string token, string light, string dark)
    {
        var window = new Window();
        window.Show();

        Assert.Equal(Colour(window, light, ThemeVariant.Light), Colour(window, token + "Color", ThemeVariant.Light));
        Assert.Equal(Colour(window, dark, ThemeVariant.Dark), Colour(window, token + "Color", ThemeVariant.Dark));

        window.Close();
    }

    private static Color Colour(Window window, string key, ThemeVariant variant)
    {
        Assert.True(window.TryFindResource(key, variant, out var value), key);

        return (Color)value!;
    }
}
