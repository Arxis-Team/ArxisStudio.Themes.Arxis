using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Скроллбар: полоса без дорожки, один полупрозрачный ползунок.
/// </summary>
/// <remarks>
/// Карточка «Скроллбар» показывает две области — в покое и под курсором. Ни
/// дорожки, ни кнопок в них нет: полосу составляет один ползунок, накладка
/// поверх содержимого. Он обязан содержимое пропускать, поэтому цвет здесь
/// полупрозрачный, а не ступень серой шкалы.
///
/// Поперёк полоса всегда 12, и меняется в ней только ползунок: в покое 6 с
/// отбивкой 3, под курсором 8 с отбивкой 2. Так содержимое не дёргается,
/// когда указатель входит в область.
/// </remarks>
public class ScrollBarTests
{
    /// <summary>Полоса — 12 поперёк в обоих состояниях.</summary>
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Bar_keeps_its_width_through_the_states(bool expanded)
    {
        var (bar, thumb, window) = Shown(Orientation.Vertical, expanded);

        Assert.Equal(12d, bar.Bounds.Width);
        // Просветы по сторонам равны: ползунок стоит посреди полосы.
        Assert.Equal(thumb.Margin.Right, bar.Bounds.Width - thumb.Bounds.Width - thumb.Margin.Right);

        window.Close();
    }

    /// <summary>Ползунок в покое: 6 с отбивкой 3 и малым радиусом.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Resting_thumb_is_a_thin_overlay(string variant)
    {
        var (_, thumb, window) = Shown(Orientation.Vertical, expanded: false, variant);

        Assert.Equal(6d, thumb.Width);
        Assert.Equal(new Thickness(0, 0, 3, 0), thumb.Margin);
        Assert.Equal(new CornerRadius(3), thumb.CornerRadius);
        Assert.Equal(Resource(window, "AxScrollThumbColor", variant), Colour(thumb.Background));

        window.Close();
    }

    /// <summary>Под курсором ползунок толстеет до 8 и темнеет.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Expanded_thumb_thickens_and_darkens(string variant)
    {
        var (_, thumb, window) = Shown(Orientation.Vertical, expanded: true, variant);

        Assert.Equal(8d, thumb.Width);
        Assert.Equal(new Thickness(0, 0, 2, 0), thumb.Margin);
        Assert.Equal(new CornerRadius(4), thumb.CornerRadius);
        Assert.Equal(Resource(window, "AxScrollThumbHoverColor", variant), Colour(thumb.Background));

        window.Close();
    }

    /// <summary>Ползунок полупрозрачен: сплошным он закрыл бы содержимое.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Thumb_lets_the_content_through(string variant)
    {
        var (_, thumb, window) = Shown(Orientation.Vertical, expanded: false, variant);

        var rest = Colour(thumb.Background)!.Value;
        var hover = Resource(window, "AxScrollThumbHoverColor", variant);

        Assert.Equal(51, rest.A);
        Assert.Equal(89, hover.A);
        // Накладка одного тона: в светлой чёрная, в тёмной белая.
        var tone = variant == "Light" ? (byte)0 : (byte)255;
        Assert.Equal((tone, tone, tone), (rest.R, rest.G, rest.B));

        window.Close();
    }

    /// <summary>Горизонтальная полоса — та же, повёрнутая.</summary>
    [AvaloniaTheory]
    [InlineData(false, 6d, 3d)]
    [InlineData(true, 8d, 2d)]
    public void Horizontal_bar_mirrors_the_vertical_one(bool expanded, double thickness, double gap)
    {
        var (bar, thumb, window) = Shown(Orientation.Horizontal, expanded);

        Assert.Equal(12d, bar.Bounds.Height);
        Assert.Equal(thickness, thumb.Height);
        Assert.Equal(new Thickness(0, 0, 0, gap), thumb.Margin);

        window.Close();
    }

    private static (ScrollBar Bar, Thumb Thumb, Window Window) Shown(
        Orientation orientation, bool expanded, string variant = "Dark")
    {
        var bar = new ScrollBar
        {
            Orientation = orientation,
            Maximum = 100,
            ViewportSize = 20,
        };

        // Длину задаём, толщину — нет: её ставит тема, и своё значение здесь
        // перекрыло бы ровно то, что проверяется.
        if (orientation == Orientation.Vertical)
        {
            bar.Height = 200;
            bar.HorizontalAlignment = HorizontalAlignment.Left;
        }
        else
        {
            bar.Width = 200;
            bar.VerticalAlignment = VerticalAlignment.Top;
        }

        var window = new Window
        {
            Width = 240,
            Height = 240,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = bar,
        };

        window.Show();
        window.UpdateLayout();

        if (expanded)
        {
            // Разворот поднимает область прокрутки под курсором; здесь области
            // нет, и состояние ставится тем же псевдоклассом, что и она.
            ((IPseudoClasses)bar.Classes).Set(":expanded", true);
            window.UpdateLayout();
        }

        var thumb = bar.GetVisualDescendants().OfType<Thumb>().First(t => t.Name == "PART_Thumb");

        return (bar, thumb, window);
    }

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key, string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        Assert.True(window.TryFindResource(key, theme, out var value), key);

        return (Color)value!;
    }
}
