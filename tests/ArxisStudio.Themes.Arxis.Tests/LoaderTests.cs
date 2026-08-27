using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Лоадер: кольцо и цветная четверть на нём.
/// </summary>
/// <remarks>
/// Карточка «Loader» рисует круг целиком серым и красит в нём только верхнюю
/// четверть. Голая дуга без кольца читалась бы как обрывок: в покое неясно,
/// докуда идёт круг.
///
/// Малый идёт 16 вторичным цветом иконки, крупный — 24 акцентом. Обводка у
/// крупного выходит 3 сама: клетка 16 с обводкой 2 растягивается до 24, а это
/// те же полтора раза.
/// </remarks>
public class LoaderTests
{
    /// <summary>Малый лоадер: 16, кольцо AxBg4, четверть вторичным цветом.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Small_loader_turns_in_the_icon_colour(string variant)
    {
        var (spinner, window) = Shown(large: false, variant);

        Assert.Equal(16d, spinner.Width);
        Assert.Equal(16d, spinner.Height);
        Assert.Equal(Resource(window, "AxBg4Color", variant), Colour(Ring(spinner).Stroke));
        Assert.Equal(Resource(window, "AxFg2Color", variant), Colour(Arc(spinner).Stroke));

        window.Close();
    }

    /// <summary>Крупный лоадер: 24 и акцентная четверть.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Large_loader_turns_in_the_accent(string variant)
    {
        var (spinner, window) = Shown(large: true, variant);

        Assert.Equal(24d, spinner.Width);
        Assert.Equal(24d, spinner.Height);
        Assert.Equal(Resource(window, "AxBg4Color", variant), Colour(Ring(spinner).Stroke));
        Assert.Equal(Resource(window, "AxAccColor", variant), Colour(Arc(spinner).Stroke));

        window.Close();
    }

    /// <summary>
    /// Кольцо — полный круг, четверть — дуга по нему той же обводкой.
    /// </summary>
    /// <remarks>
    /// Обводка задана в клетке 16 и растягивается вместе с ней, поэтому в теме
    /// она одна на оба размера.
    /// </remarks>
    [AvaloniaFact]
    public void Ring_and_arc_share_one_stroke_in_the_cell_of_sixteen()
    {
        var (spinner, window) = Shown(large: true, "Dark");

        var ring = Ring(spinner);
        var arc = Arc(spinner);

        Assert.Equal(2d, ring.StrokeThickness);
        Assert.Equal(2d, arc.StrokeThickness);
        Assert.Equal(14d, ring.Width);
        Assert.Equal(14d, ring.Height);

        // Четверть лежит поверху и потому куда шире, чем выше. Дуга в три
        // четверти обошла бы круг, и высота у неё сравнялась бы с шириной.
        Assert.True(
            arc.Bounds.Height * 2 < arc.Bounds.Width,
            $"дуга обходит круг, а не лежит поверху: {arc.Bounds.Width} на {arc.Bounds.Height}");

        window.Close();
    }

    /// <summary>Оборот за 0.8 секунды, линейно и без конца.</summary>
    [AvaloniaFact]
    public void Turn_takes_eight_tenths_of_a_second()
    {
        var (spinner, window) = Shown(large: false, "Dark");

        var cell = (Canvas)Part(spinner, "PART_Cell");

        // Крутится клетка целиком и вокруг своей середины: у одной дуги центр
        // вращения пришёлся бы на середину её границ, а не круга.
        Assert.IsType<RotateTransform>(cell.RenderTransform);
        Assert.Equal(16d, cell.Bounds.Width);
        Assert.Equal(16d, cell.Bounds.Height);
        Assert.Equal(RelativePoint.Center, cell.RenderTransformOrigin);

        var theme = (ControlTheme)Application.Current!.FindResource(typeof(AxSpinner))!;
        var animation = theme.Children
            .OfType<Style>()
            .SelectMany(style => style.Animations)
            .OfType<Animation>()
            .Single();

        Assert.Equal(TimeSpan.FromSeconds(0.8), animation.Duration);
        Assert.Equal(IterationCount.Infinite, animation.IterationCount);
        // Линейное — то, что стоит по умолчанию: разгона у оборота нет.
        Assert.IsType<LinearEasing>(animation.Easing);

        window.Close();
    }

    private static (AxSpinner Spinner, Window Window) Shown(bool large, string variant)
    {
        var spinner = new AxSpinner();

        if (large)
            spinner.Classes.Add("large");

        var window = new Window
        {
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = spinner,
        };

        window.Show();
        window.UpdateLayout();

        return (spinner, window);
    }

    private static Ellipse Ring(AxSpinner spinner) =>
        spinner.GetVisualDescendants().OfType<Ellipse>().First();

    private static Path Arc(AxSpinner spinner) =>
        spinner.GetVisualDescendants().OfType<Path>().First();

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
