using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Примитивы: контролы M0, которым карточки в проекте не досталось.
/// </summary>
/// <remarks>
/// Аватар, ползунок, карточка-контейнер и бейдж стоят в списке файлов M0
/// раздела 6 приёмки, но ни карточки, ни строки инвентаря у них нет. Сверять
/// их не с чем — кроме одного решения того же раздела 6, про монограммы, и
/// оно проверяется здесь.
/// </remarks>
public class PrimitiveTests
{
    /// <summary>
    /// Цветная монограмма берёт шаг 1 шкалы, а не цвет заливки.
    /// </summary>
    /// <remarks>
    /// Раздел 6 приёмки: «Аватар на AxOrg / AxGrn / AxPur / AxRed, белые
    /// инициалы» даёт 2,68…3,69:1 — решено брать шаг 1 шкалы, где белый держит
    /// 5,9:1 и выше в обоих вариантах. Значения потому и одни на оба варианта.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("orange", "#FFA14916")]
    [InlineData("green", "#FF1E6B33")]
    [InlineData("purple", "#FF55339C")]
    [InlineData("red", "#FFAD2B38")]
    public void Monogram_takes_the_first_step_of_the_scale(string tint, string expected)
    {
        foreach (var variant in new[] { "Light", "Dark" })
        {
            var (avatar, window) = Shown(tint, variant);

            Assert.Equal(Color.Parse(expected), Colour(avatar.TileBrush));

            window.Close();
        }
    }

    /// <summary>Белые инициалы на монограмме держат порог читаемого текста.</summary>
    [AvaloniaTheory]
    [InlineData("orange")]
    [InlineData("green")]
    [InlineData("purple")]
    [InlineData("red")]
    public void White_initials_stay_readable_on_the_monogram(string tint)
    {
        var (avatar, window) = Shown(tint, "Light");

        var ratio = Contrast(Colour(avatar.Foreground)!.Value, Colour(avatar.TileBrush)!.Value);

        Assert.True(ratio >= 4.5, $"{tint}: инициалы дают {ratio:F2}:1");

        window.Close();
    }

    /// <summary>
    /// Плитка без цвета идёт акцентом, круглая — той же плиткой со скруглением.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Plain_avatar_is_an_accent_tile(string variant)
    {
        var (avatar, window) = Shown(null, variant);

        Assert.Equal(36d, avatar.Width);
        Assert.Equal(36d, avatar.Height);
        Assert.Equal(new CornerRadius(8), avatar.CornerRadius);
        Assert.Equal(Resource(window, "AxAccStrongColor", variant), Colour(avatar.TileBrush));
        Assert.Equal(Resource(window, "AxOnAccColor", variant), Colour(avatar.Foreground));

        avatar.Classes.Add("round");
        window.UpdateLayout();

        Assert.Equal(new CornerRadius(1000), avatar.CornerRadius);

        window.Close();
    }

    private static (AxAvatar Avatar, Window Window) Shown(string? tint, string variant)
    {
        var avatar = new AxAvatar { Initials = "ВЧ" };

        if (tint is not null)
            avatar.Classes.Add(tint);

        var window = new Window
        {
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = avatar,
        };

        window.Show();
        window.UpdateLayout();

        return (avatar, window);
    }

    /// <summary>Отношение контраста по WCAG 2.1.</summary>
    private static double Contrast(Color a, Color b)
    {
        var la = Luminance(a);
        var lb = Luminance(b);

        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(Color c) =>
        0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);

    private static double Channel(byte value)
    {
        var v = value / 255d;

        return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
    }

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key, string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        Assert.True(window.TryFindResource(key, theme, out var value), key);

        return (Color)value!;
    }
}
