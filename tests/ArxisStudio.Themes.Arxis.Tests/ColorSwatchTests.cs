using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Образец цвета: что он рисует — цвет, полоску прозрачности, шахматку и «нет цвета».
/// </summary>
/// <remarks>
/// Безголовый прогон темы не рисует пикселей, поэтому рисунок записывается в <see cref="DrawingGroup"/> и читается
/// прямоугольниками: какой кистью, где и какой ширины.
/// </remarks>
public class ColorSwatchTests
{
    /// <summary>
    /// Как у Unity: цвет — непрозрачным над полоской, а полоска делит ширину по прозрачности — белое слева, чёрное
    /// справа.
    /// </summary>
    [AvaloniaFact]
    public void The_bar_shows_the_color_solid_and_its_alpha_as_white_then_black()
    {
        var (swatch, window) = Shown(new AxColorSwatch { Color = Color.FromArgb(0x80, 0x3C, 0x6E, 0xCF) });
        var bar = Token("AxColorAlphaBarHeight");
        var fills = Fills(swatch);

        var solid = Assert.Single(fills, fill => fill.Color == Color.FromRgb(0x3C, 0x6E, 0xCF));

        Assert.Equal(new Rect(0, 0, 100, 20 - bar), solid.Area);

        var white = Assert.Single(fills, fill => fill.Color == Colors.White);
        var black = Assert.Single(fills, fill => fill.Color == Colors.Black);

        Assert.Equal(new Rect(0, 20 - bar, 50, bar), white.Area);
        Assert.Equal(new Rect(50, 20 - bar, 50, bar), black.Area);

        window.Close();
    }

    /// <summary>Непрозрачный цвет — полоска вся белая.</summary>
    [AvaloniaFact]
    public void An_opaque_color_has_an_all_white_bar()
    {
        var (swatch, window) = Shown(new AxColorSwatch { Color = Colors.Teal });
        var fills = Fills(swatch);

        Assert.Equal(100, Assert.Single(fills, fill => fill.Color == Colors.White).Area.Width);
        Assert.DoesNotContain(fills, fill => fill.Color == Colors.Black);

        window.Close();
    }

    /// <summary>Шахматка — цвет как есть поверх неё: прозрачное видно насквозь.</summary>
    [AvaloniaFact]
    public void A_checker_swatch_lays_the_color_over_the_checker()
    {
        var color = Color.FromArgb(0x80, 0xFF, 0x00, 0x00);
        var (swatch, window) = Shown(new AxColorSwatch { Color = color, AlphaDisplay = AxColorAlphaDisplay.Checker });
        var fills = All(swatch);

        Assert.IsAssignableFrom<DrawingBrush>(fills[0].Brush);
        Assert.Equal(color, Assert.IsAssignableFrom<ISolidColorBrush>(fills[1].Brush).Color);

        window.Close();
    }

    /// <summary>Нет цвета — шахматка, перечёркнутая из нижнего левого угла в верхний правый цветом ошибки.</summary>
    [AvaloniaFact]
    public void No_color_is_a_checker_crossed_by_the_error_color()
    {
        var (swatch, window) = Shown(new AxColorSwatch { IsNone = true });
        var line = Assert.Single(Flatten(Record(swatch)), item => item.Pen is not null);

        Assert.Equal(new Rect(0, 0, 100, 20), line.Geometry!.Bounds);
        var application = Application.Current!;

        Assert.True(application.TryFindResource("AxErrorBrush", application.ActualThemeVariant, out var error));
        Assert.Equal(Assert.IsAssignableFrom<ISolidColorBrush>(error).Color,
            Assert.IsAssignableFrom<ISolidColorBrush>(line.Pen!.Brush).Color);
        Assert.IsAssignableFrom<DrawingBrush>(All(swatch)[0].Brush);

        window.Close();
    }

    /// <summary>Кисть вместо цвета — градиент — рисуется как есть, поверх шахматки.</summary>
    [AvaloniaFact]
    public void A_brush_is_drawn_as_it_is_over_the_checker()
    {
        var gradient = new LinearGradientBrush { GradientStops = { new GradientStop(Colors.Red, 0), new GradientStop(Colors.Blue, 1) } };
        var (swatch, window) = Shown(new AxColorSwatch { Brush = gradient });
        var fills = All(swatch);

        Assert.IsAssignableFrom<DrawingBrush>(fills[0].Brush);
        Assert.Same(gradient, fills[1].Brush);

        window.Close();
    }

    private static (AxColorSwatch Swatch, Window Window) Shown(AxColorSwatch swatch)
    {
        swatch.Width = 100;
        swatch.Height = 20;

        var window = new Window { Width = 300, Height = 200, Content = new StackPanel { Children = { swatch } } };

        window.Show();
        window.UpdateLayout();

        return (swatch, window);
    }

    private static List<(Color Color, Rect Area)> Fills(AxColorSwatch swatch) =>
        [.. All(swatch).Where(fill => fill.Brush is ISolidColorBrush)
                       .Select(fill => (((ISolidColorBrush)fill.Brush!).Color, fill.Area))];

    /// <summary>Заливки по порядку рисования; запись хранит фигуры платформы, и место у них — границы.</summary>
    private static List<(IBrush? Brush, Rect Area)> All(AxColorSwatch swatch) =>
        [.. Flatten(Record(swatch)).Where(item => item.Pen is null && item.Geometry is not null)
                                  .Select(item => (item.Brush, item.Geometry!.Bounds))];

    private static DrawingGroup Record(AxColorSwatch swatch)
    {
        var drawing = new DrawingGroup();

        using (var context = drawing.Open())
            swatch.Render(context);

        return drawing;
    }

    private static IEnumerable<GeometryDrawing> Flatten(Drawing drawing) => drawing switch
    {
        DrawingGroup group => group.Children.SelectMany(Flatten),
        GeometryDrawing geometry => [geometry],
        _ => [],
    };

    private static double Token(string key)
    {
        Assert.True(Application.Current!.TryFindResource(key, out var value), $"в теме нет {key}");

        return Assert.IsType<double>(value);
    }
}
