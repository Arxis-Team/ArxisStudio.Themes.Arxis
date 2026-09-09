using ArxisStudio.Controls;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Разделитель областей: линия в пиксель и полоса захвата вокруг неё.
/// </summary>
/// <remarks>
/// Своей карточки у него в проекте нет — как и у разделителя, от которого он
/// отличается одним: за него берутся мышью. Отсюда и всё, что проверяется:
/// линия остаётся однопиксельной и цветом <c>AxBrd</c>, как у соседа, а полоса
/// захвата вокруг неё шире линии — иначе попасть в границу человек не сможет.
/// </remarks>
public class SplitterTests
{
    /// <summary>Горизонтальный: полоса захвата поперёк, линия в пиксель.</summary>
    [AvaloniaFact]
    public void A_horizontal_splitter_is_a_lane_with_a_pixel_line()
    {
        var (splitter, window) = Shown(Orientation.Horizontal);

        Assert.Equal(Lane(window), splitter.Bounds.Height);
        Assert.Equal(1d, Line(splitter).Bounds.Height);
        Assert.True(splitter.Bounds.Width > 100, "разделитель не растянулся по ширине");

        window.Close();
    }

    /// <summary>Вертикальный: полоса захвата вдоль, линия в пиксель.</summary>
    [AvaloniaFact]
    public void A_vertical_splitter_is_a_lane_with_a_pixel_line()
    {
        var (splitter, window) = Shown(Orientation.Vertical);

        Assert.Equal(Lane(window), splitter.Bounds.Width);
        Assert.Equal(1d, Line(splitter).Bounds.Width);
        Assert.True(splitter.Bounds.Height > 100, "разделитель не растянулся по высоте");

        window.Close();
    }

    /// <summary>
    /// Полоса захвата шире линии.
    /// </summary>
    /// <remarks>
    /// В этом весь смысл контрола: линия остаётся такой же тонкой, как у
    /// разделителя, а попасть в неё курсором всё-таки можно. Сойдись они, за
    /// границу пришлось бы браться с точностью до пикселя.
    /// </remarks>
    [AvaloniaFact]
    public void The_grab_lane_is_wider_than_the_line()
    {
        var (splitter, window) = Shown(Orientation.Horizontal);

        Assert.True(
            splitter.Bounds.Height > Line(splitter).Bounds.Height,
            "за линию нельзя взяться: полоса захвата не шире её самой");

        window.Close();
    }

    /// <summary>
    /// За границу берутся и с клавиатуры.
    /// </summary>
    /// <remarks>
    /// Стрелки и Home/End базовый контрол разбирает сам, но только в фокусе, а
    /// <c>Focusable</c> ему объявлял шаблон Fluent — свой шаблон его не
    /// наследует. Без объявления в теме клавиатура теряла разделитель целиком:
    /// границу областей нельзя было сдвинуть, не взявшись за неё мышью.
    /// </remarks>
    [AvaloniaFact]
    public void The_splitter_is_reachable_from_the_keyboard()
    {
        var (splitter, window) = Shown(Orientation.Horizontal);

        Assert.True(splitter.Focusable, "разделитель не берётся в фокус: клавиатурой границу не сдвинуть");
        Assert.True(splitter.Focus(), "разделитель не принял фокус");
        Assert.True(splitter.IsFocused, "фокус на разделителе не удержался");

        window.Close();
    }

    /// <summary>
    /// Стрелка с клавиатуры двигает границу.
    /// </summary>
    /// <remarks>
    /// Половина, ради которой фокус и нужен: доказывать, что контрол умеет его
    /// принять, толку мало, если доли от этого не меняются. Здесь разделитель
    /// стоит между двумя долями, как в доке, и проверяется то, что увидит
    /// человек, — что доли разъехались.
    /// </remarks>
    [AvaloniaFact]
    public void An_arrow_key_moves_the_border()
    {
        var splitter = new AxSplitter { Orientation = Orientation.Horizontal };

        var grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto,*") };

        grid.Children.Add(new Border());
        grid.Children.Add(splitter);
        grid.Children.Add(new Border());

        Grid.SetRow(grid.Children[0], 0);
        Grid.SetRow(splitter, 1);
        Grid.SetRow(grid.Children[2], 2);

        var window = new Window { Width = 240, Height = 240, Content = grid };

        window.Show();
        window.UpdateLayout();

        var before = grid.RowDefinitions[0].ActualHeight;

        Assert.True(splitter.Focus(), "разделитель не принял фокус");

        splitter.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down });

        window.UpdateLayout();

        Assert.True(
            grid.RowDefinitions[0].ActualHeight > before,
            $"доля не сдвинулась: было {before:F0}, стало {grid.RowDefinitions[0].ActualHeight:F0}");

        window.Close();
    }

    /// <summary>Линия — того же токена, что и у разделителя.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_line_takes_the_separator_token(string variant)
    {
        var (splitter, window) = Shown(Orientation.Vertical, variant);

        Assert.Equal(Resource(window, "AxBrdColor", variant), Colour(Line(splitter).Background));

        window.Close();
    }

    /// <summary>
    /// Сторона задаёт направление изменения размера.
    /// </summary>
    /// <remarks>
    /// Базовый класс умеет угадывать его по выравниванию, и здесь это выключено
    /// намеренно: угаданное меняется от того, растянут ли контрол, — а это
    /// свойство соседей по сетке.
    /// </remarks>
    [AvaloniaFact]
    public void The_side_decides_what_the_splitter_resizes()
    {
        Assert.Equal(GridResizeDirection.Rows, new AxSplitter().ResizeDirection);

        Assert.Equal(
            GridResizeDirection.Columns,
            new AxSplitter { Orientation = Orientation.Vertical }.ResizeDirection);

        Assert.Equal(
            GridResizeDirection.Rows,
            new AxSplitter { Orientation = Orientation.Horizontal }.ResizeDirection);
    }

    private static Border Line(AxSplitter splitter)
    {
        var line = splitter.GetVisualDescendants().OfType<Border>().FirstOrDefault(part => part.Name == "PART_Line");

        Assert.NotNull(line);

        return line!;
    }

    private static double Lane(Window window)
    {
        Assert.True(window.TryFindResource("AxSplitterLane", ThemeVariant.Dark, out var value));

        return (double)value!;
    }

    private static (AxSplitter Splitter, Window Window) Shown(Orientation orientation, string variant = "Dark")
    {
        var splitter = new AxSplitter { Orientation = orientation };

        var window = new Window
        {
            Width = 240,
            Height = 240,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = splitter,
        };

        window.Show();
        window.UpdateLayout();

        return (splitter, window);
    }

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key, string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        Assert.True(window.TryFindResource(key, theme, out var value), key);

        return (Color)value!;
    }
}
