using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Прогресс: полоса в четыре пикселя и бегунок бесконечного режима.
/// </summary>
/// <remarks>
/// Карточка «Progress» кладёт заполнение на дорожку цвета нажатия. Наведение
/// сюда не годится: под заполнением дорожка обязана читаться как жёлоб, а не
/// как подсветка строки, и в карточке она на шаг темнее.
///
/// Бесконечный режим проходит полосу за 1.2 секунды — это второе и последнее
/// разрешённое движение рядом с лоадером.
/// </remarks>
public class ProgressTests
{
    /// <summary>Полоса: 4 в высоту с радиусом 2.</summary>
    [AvaloniaFact]
    public void Bar_is_four_pixels_of_a_rounded_track()
    {
        var (bar, window) = Shown(indeterminate: false, "Dark");

        Assert.Equal(4d, bar.Bounds.Height);
        Assert.Equal(new CornerRadius(2), bar.CornerRadius);

        window.Close();
    }

    /// <summary>Дорожка — ступень нажатия, заполнение — акцент.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Fill_lies_on_the_darker_step(string variant)
    {
        var (bar, window) = Shown(indeterminate: false, variant);

        Assert.Equal(Resource(window, "AxBg4Color", variant), Colour(bar.Background));
        Assert.Equal(Resource(window, "AxAccColor", variant), Colour(bar.Foreground));
        Assert.Equal(
            Resource(window, "AxAccColor", variant),
            Colour(((Border)Part(bar, "PART_Indicator")).Background));

        window.Close();
    }

    /// <summary>Доля видна шириной заполнения, а не цветом.</summary>
    [AvaloniaFact]
    public void Indicator_shows_the_share_by_its_width()
    {
        var (bar, window) = Shown(indeterminate: false, "Dark");

        var indicator = Part(bar, "PART_Indicator");
        var share = indicator.Bounds.Width / bar.Bounds.Width;

        Assert.InRange(share, 0.60, 0.64);

        window.Close();
    }

    /// <summary>
    /// Бесконечный бегунок проходит полосу за 1.2 секунды, линейно и без конца.
    /// </summary>
    [AvaloniaFact]
    public void Endless_run_takes_one_and_two_tenths()
    {
        var (bar, window) = Shown(indeterminate: true, "Dark");

        var theme = (ControlTheme)Application.Current!.FindResource(typeof(AxProgressBar))!;
        var animation = theme.Children
            .OfType<Style>()
            .SelectMany(style => style.Animations)
            .OfType<Animation>()
            .Single();

        Assert.Equal(TimeSpan.FromSeconds(1.2), animation.Duration);
        Assert.Equal(IterationCount.Infinite, animation.IterationCount);
        Assert.IsType<LinearEasing>(animation.Easing);

        // Бегунок уже не во всю дорожку: иначе движения не видно.
        Assert.True(
            Part(bar, "PART_Indicator").Bounds.Width < bar.Bounds.Width,
            "бегунок растянут во всю дорожку");

        window.Close();
    }

    private static (AxProgressBar Bar, Window Window) Shown(bool indeterminate, string variant)
    {
        var bar = new AxProgressBar
        {
            Width = 340,
            Minimum = 0,
            Maximum = 100,
            Value = 62,
            IsIndeterminate = indeterminate,
        };

        var window = new Window
        {
            Width = 400,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = bar,
        };

        window.Show();
        window.UpdateLayout();

        return (bar, window);
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
