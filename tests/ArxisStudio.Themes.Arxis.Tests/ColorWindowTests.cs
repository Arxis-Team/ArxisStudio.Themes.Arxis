using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Окно «Цвет» поля цвета — сеанс правки, как у Unity: на ходу видно всё, а пишется одно значение.
/// </summary>
/// <remarks>
/// Сеанс фиксируется, когда окно закрывают крестиком или Enter и когда оно переходит к другому полю; Esc, уход поля
/// из дерева и закрытие окна поля его отменяют. Окно одно на процесс, встаёт рядом с полем и носит одну кнопку —
/// «закрыть».
/// </remarks>
public sealed class ColorWindowTests : IDisposable
{
    /// <summary>Окно цвета одно на процесс: тест, упавший с открытым, не оставляет его следующему.</summary>
    public void Dispose() => AxColorPickerWindow.Current?.Close();

    /// <summary>
    /// Шаги в окне поле показывает на ходу, а пишет одно значение — когда окно закрывают Enter или крестиком.
    /// </summary>
    [AvaloniaFact]
    public void Closing_the_window_writes_the_session_once()
    {
        var (fields, window) = Shown(0xFF3C6ECF);
        var field = fields[0];
        var log = Log.Of(field);

        var colors = Open(field);

        Step(colors, Key.Down);
        Step(colors, Key.Right);

        Assert.Equal(2, log.Changing.Count);
        Assert.Empty(log.Committed);

        var shown = colors.Picker.Color;

        colors.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, string.Empty);
        Pump();

        Assert.Null(AxColorPickerWindow.Current);
        Assert.False(field.IsPickerOpen, "окно закрылось, а поле считает выбор открытым");
        Assert.Equal(shown, Assert.Single(log.Committed).Color);
        Assert.Equal(shown, field.Color);

        // Крестик — та же фиксация: окно закрывается само, а не отменяет.
        colors = Open(field);
        Step(colors, Key.Up);
        colors.Close();
        Pump();

        Assert.Equal(2, log.Committed.Count);
        Assert.Equal(0, log.Cancelled);

        window.Close();
    }

    /// <summary>
    /// Сеанс, вернувшийся к цвету начала — щелчком по образцу «было», — ничего не пишет: показ на объекте отменяется.
    /// </summary>
    [AvaloniaFact]
    public void A_session_back_at_its_start_writes_nothing()
    {
        var (fields, window) = Shown(0xFF3C6ECF);
        var field = fields[0];
        var log = Log.Of(field);
        var colors = Open(field);

        Step(colors, Key.Down);

        colors.Picker.GetVisualDescendants().OfType<Button>().Single(part => part.Name == "PART_Original")
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        colors.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, string.Empty);
        Pump();

        Assert.Empty(log.Committed);
        Assert.Equal(1, log.Cancelled);
        Assert.Equal(Color.FromUInt32(0xFF3C6ECF), field.Color);

        window.Close();
    }

    /// <summary>
    /// Второе поле забирает окно: сеанс первого фиксируется одним значением, а окно остаётся тем же — с цветом
    /// второго поля.
    /// </summary>
    [AvaloniaFact]
    public void Another_field_commits_the_first_and_takes_the_window()
    {
        var (fields, window) = Shown(0xFF3C6ECF, 0xFFE0A040);
        var (first, second) = (fields[0], fields[1]);
        var log = Log.Of(first);
        var colors = Open(first);

        Step(colors, Key.Down);

        var shown = colors.Picker.Color;

        second.IsPickerOpen = true;
        Pump();

        Assert.Same(colors, AxColorPickerWindow.Current);
        Assert.Same(second, colors.Field);
        Assert.Equal(shown, Assert.Single(log.Committed).Color);
        Assert.False(first.IsPickerOpen, "первое поле осталось открытым");
        Assert.True(second.IsPickerOpen);
        Assert.Equal(Color.FromUInt32(0xFFE0A040), colors.Picker.Color);
        Assert.Equal(Color.FromUInt32(0xFFE0A040), colors.Picker.OriginalColor);

        window.Close();
    }

    /// <summary>Закрывается окно поля — сеанс отменяется: писать уже некуда.</summary>
    [AvaloniaFact]
    public void Closing_the_fields_window_cancels_the_session()
    {
        var (fields, window) = Shown(0xFF3C6ECF);
        var log = Log.Of(fields[0]);
        var colors = Open(fields[0]);

        Step(colors, Key.Down);
        window.Close();
        Pump();

        Assert.Null(AxColorPickerWindow.Current);
        Assert.Empty(log.Committed);
        Assert.Equal(1, log.Cancelled);
    }

    /// <summary>
    /// Поле, ушедшее из дерева насовсем, отменяет сеанс и закрывает окно; переставленное за один проход — как строку
    /// списка переносят в другую ячейку — сеанс не трогает.
    /// </summary>
    [AvaloniaFact]
    public void A_field_that_leaves_the_tree_cancels_and_one_moved_within_a_pass_keeps_the_session()
    {
        var (fields, window) = Shown(0xFF3C6ECF);
        var field = fields[0];
        var log = Log.Of(field);
        var panel = Assert.IsType<StackPanel>(field.Parent);
        var colors = Open(field);

        Step(colors, Key.Down);

        panel.Children.Remove(field);
        panel.Children.Add(field);
        Pump();

        Assert.Same(colors, AxColorPickerWindow.Current);
        Assert.Same(field, colors.Field);

        panel.Children.Remove(field);
        Pump();

        Assert.Null(AxColorPickerWindow.Current);
        Assert.False(field.IsPickerOpen);
        Assert.Empty(log.Committed);
        Assert.Equal(1, log.Cancelled);
        Assert.Equal(Color.FromUInt32(0xFF3C6ECF), field.Color);

        window.Close();
    }

    /// <summary>
    /// Вид каналов, выбранный в окне, возвращается полю; следующее окно открывается в виде поля, а вид, поставленный
    /// полю при открытом окне, встаёт в нём сразу.
    /// </summary>
    [AvaloniaFact]
    public void The_mode_travels_between_the_field_and_the_window()
    {
        var (fields, window) = Shown(0xFF3C6ECF);
        var field = fields[0];
        var colors = Open(field);

        colors.Picker.Mode = AxColorPickerMode.Hsv;

        Assert.Equal(AxColorPickerMode.Hsv, field.PickerMode);

        colors.Close();
        Pump();

        field.PickerMode = AxColorPickerMode.Rgb1;
        colors = Open(field);

        Assert.Equal(AxColorPickerMode.Rgb1, colors.Picker.Mode);

        field.PickerMode = AxColorPickerMode.Rgb255;

        Assert.Equal(AxColorPickerMode.Rgb255, colors.Picker.Mode);

        window.Close();
    }

    /// <summary>
    /// Окно встаёт слева от поля вровень с его верхом, а где слева места нет — справа; в рабочей области экрана.
    /// </summary>
    [AvaloniaFact]
    public void The_window_stands_beside_the_field()
    {
        var (fields, window) = Shown(0xFF3C6ECF);
        var field = fields[0];
        var screen = window.Screens?.ScreenFromVisual(field);

        Assert.NotNull(screen);

        var area = screen.WorkingArea;

        // Поле — у правого края окна во весь экран: слева от него окну место есть, справа — нет. Новый размер окна
        // безголовая платформа отдаёт постом — до открытия его прокачивают.
        window.Width = area.Width;
        field.HorizontalAlignment = HorizontalAlignment.Right;
        Pump();
        window.UpdateLayout();

        Assert.True(field.PointToScreen(default).X > area.Width / 2, "поле не встало у правого края");

        var colors = Open(field);
        var origin = field.PointToScreen(default);
        var size = PixelSize.FromSize(colors.FrameSize ?? colors.ClientSize, colors.DesktopScaling);

        Assert.True(colors.Position.X + size.Width <= origin.X, $"окно {colors.Position} шириной {size.Width} заходит на поле {origin}");
        Assert.Equal(origin.Y, colors.Position.Y);

        colors.Close();
        field.HorizontalAlignment = HorizontalAlignment.Left;
        window.UpdateLayout();
        Pump();

        colors = Open(field);

        var corner = field.PointToScreen(new Point(field.Bounds.Width, field.Bounds.Height));

        Assert.True(colors.Position.X >= corner.X, $"окно {colors.Position} без места слева не ушло вправо от поля {corner}");

        window.Close();
    }

    /// <summary>У окна цвета одна кнопка окна — «закрыть»: свернуть и развернуть ему нечего; заголовок — «Color».</summary>
    [AvaloniaFact]
    public void The_window_carries_only_the_close_button()
    {
        var (fields, window) = Shown(0xFF3C6ECF);
        var colors = Open(fields[0]);
        var buttons = colors.GetVisualDescendants().OfType<AxWindowControls>().Single()
            .GetVisualDescendants().OfType<Button>().ToDictionary(button => button.Name ?? string.Empty, button => button.IsVisible);

        Assert.False(buttons["PART_Minimize"], "у окна цвета есть «свернуть»");
        Assert.False(buttons["PART_Maximize"], "у окна цвета есть «развернуть»");
        Assert.True(buttons["PART_Close"], "у окна цвета нет «закрыть»");
        Assert.Equal("Color", colors.Title);

        window.Close();
    }

    private static (AxColorField[] Fields, Window Window) Shown(params uint[] colors)
    {
        var fields = colors.Select(color => new AxColorField { Color = Color.FromUInt32(color), Width = 220 }).ToArray();
        var panel = new StackPanel { Margin = new Thickness(20) };

        foreach (var field in fields)
            panel.Children.Add(field);

        var window = new Window { Width = 500, Height = 400, Content = panel };

        window.Show();
        window.UpdateLayout();
        Pump();

        return (fields, window);
    }

    /// <summary>Открывает окно цвета у поля и ждёт, пока в нём встанет каретка.</summary>
    private static AxColorPickerWindow Open(AxColorField field)
    {
        field.IsPickerOpen = true;
        Pump();

        var colors = Assert.IsType<AxColorPickerWindow>(AxColorPickerWindow.Current);

        Assert.Same(field, colors.Field);

        return colors;
    }

    /// <summary>Шаг стрелкой в квадрате окна — каретка стоит там с открытия.</summary>
    private static void Step(AxColorPickerWindow colors, Key key)
    {
        colors.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, string.Empty);
        Pump();
    }

    private static void Pump()
    {
        for (var at = 0; at < 5; at++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }

    /// <summary>Что поле сказало хозяину.</summary>
    private sealed class Log
    {
        public List<Color> Changing { get; } = [];

        public List<(Color Color, string? Text)> Committed { get; } = [];

        public int Cancelled { get; private set; }

        public static Log Of(AxColorField field)
        {
            var log = new Log();

            field.ColorChanging += (_, e) => log.Changing.Add(e.Color);
            field.ColorCommitted += (_, e) => log.Committed.Add((e.Color, e.Text));
            field.ColorCancelled += (_, _) => log.Cancelled++;

            return log;
        }
    }
}
