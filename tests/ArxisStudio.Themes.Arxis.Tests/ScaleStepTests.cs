using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Токены, заведённые разделом 4, стоят на названных им ступенях шкал.
/// </summary>
/// <remarks>
/// Раздел 4 «Основ» перечисляет значения, которых в палитре нет, и закрывает
/// каждое не цветом, а <b>ступенью шкалы</b>: рамка баннера — AxBlue10 в
/// светлой и AxBlue3 в тёмной, и так все четыре. Это сильнее любой карточки:
/// карточка показывает цвет, а раздел 4 говорит, откуда он берётся.
///
/// Разница не умозрительная. Галерея зоны 20 рисует тёмные рамки успеха и
/// предупреждения другими цветами, чем дают эти ступени, — и если бы тема шла
/// за карточкой, она разошлась бы со шкалой, на которой держится весь набор.
/// Тест закрепляет правило, а не снимок.
/// </remarks>
public class ScaleStepTests
{
    [AvaloniaTheory]
    [InlineData("AxInfoBorder", "AxBlue10", "AxBlue3")]
    [InlineData("AxSuccessBorder", "AxGreen9", "AxGreen4")]
    [InlineData("AxWarningBorder", "AxYellow6", "AxYellow4")]
    [InlineData("AxErrorBorder", "AxRed9", "AxRed4")]
    public void Token_sits_on_the_step_the_specification_names(string token, string light, string dark)
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
