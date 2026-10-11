using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Выбор цвета — окно Color у Unity: модель HSV, кольцо оттенков с квадратом, каналы в трёх видах, код, «было».
/// </summary>
/// <remarks>
/// Один жест — одна фиксация: протяжку видно на ходу, а фиксируется она при отпускании, и Esc посреди неё
/// возвращает цвет начала. Оттенок живёт в модели HSV и переживает серый и чёрный.
/// </remarks>
public class ColorPickerTests
{
    /// <summary>Строки каналов окна: три цветом и прозрачность.</summary>
    private static readonly string[] Rows = ["0", "1", "2", "A"];

    [AvaloniaTheory]
    [InlineData("#3C6ECF", 0xFF3C6ECFu)]
    [InlineData("3C6ECF", 0xFF3C6ECFu)]
    [InlineData("#F80", 0xFFFF8800u)]
    [InlineData("#80FF0000", 0x80FF0000u)]
    [InlineData("Red", 0xFFFF0000u)]
    [InlineData(" #3c6ecf ", 0xFF3C6ECFu)]
    public void Color_text_is_read_as_avalonia_reads_it(string text, uint expected)
    {
        Assert.True(AxColorField.TryParse(text, out var color), $"«{text}» не прочлось");
        Assert.Equal(Color.FromUInt32(expected), color);
    }

    [AvaloniaTheory]
    [InlineData("")]
    [InlineData("zzz")]
    [InlineData("#12345")]
    [InlineData("rgb(")]
    public void Nonsense_is_not_a_color(string text) =>
        Assert.False(AxColorField.TryParse(text, out _), $"«{text}» прочлось цветом");

    [AvaloniaFact]
    public void A_color_is_written_short_when_opaque_and_long_when_not()
    {
        Assert.Equal("#3C6ECF", AxColorField.Format(Color.FromUInt32(0xFF3C6ECF)));
        Assert.Equal("#803C6ECF", AxColorField.Format(Color.FromUInt32(0x803C6ECF)));
        Assert.Equal("#3C6ECF", AxColorField.Format(Color.FromUInt32(0x803C6ECF), alpha: false));
        Assert.Equal("3C6ECF", AxColorPicker.Hex(Color.FromUInt32(0x803C6ECF)));
    }

    /// <summary>
    /// Оттенок — в модели HSV: цвет, ставший чёрным или серым, оттенка не теряет, а чёрный — и насыщенности:
    /// поднимешь яркость — вернётся тот же цвет.
    /// </summary>
    [AvaloniaFact]
    public void The_hue_survives_black_and_gray()
    {
        var (picker, window) = Shown();

        picker.Hsv = new HsvColor(1, 200, 0.5, 0.8);
        picker.Color = Colors.Black;
        Assert.Equal(200, picker.Hsv.H, 6);
        Assert.Equal(0.5, picker.Hsv.S, 6);

        picker.Color = Color.FromRgb(0x80, 0x80, 0x80);
        Assert.Equal(200, picker.Hsv.H, 6);
        Assert.Equal(0, picker.Hsv.S, 6);

        window.Close();
    }

    /// <summary>Протяжка по спектру видна на каждом движении, а фиксируется одним цветом при отпускании.</summary>
    [AvaloniaFact]
    public void A_drag_on_the_spectrum_shows_each_step_and_commits_once()
    {
        var (picker, window) = Shown();
        var (changing, committed) = Listen(picker);
        var spectrum = Part<AxColorSpectrum>(picker, "PART_Spectrum");
        var at = Point(spectrum, window, 0.2, 0.2);

        window.MouseDown(at, MouseButton.Left);
        window.MouseMove(Point(spectrum, window, 0.5, 0.4));
        window.MouseMove(Point(spectrum, window, 0.8, 0.3));

        Assert.True(changing.Count >= 2, $"протяжка показала {changing.Count} шагов");
        Assert.Empty(committed);

        window.MouseUp(Point(spectrum, window, 0.8, 0.3), MouseButton.Left);

        var color = Assert.Single(committed);

        Assert.Equal(color, picker.Color);
        Assert.Equal(0.8, picker.Hsv.S, 2);
        Assert.Equal(0.7, picker.Hsv.V, 2);

        window.Close();
    }

    /// <summary>
    /// Квадрат стоит внутри кольца: его углы вместе с кольцом фокуса не задевают полосу оттенков, а нажатие в середине
    /// достаётся квадрату, не кольцу.
    /// </summary>
    [AvaloniaFact]
    public void The_square_sits_inside_the_ring()
    {
        var (picker, window) = Shown();
        var wheel = Part<AxColorWheel>(picker, "PART_Wheel");
        var spectrum = Part<AxColorSpectrum>(picker, "PART_Spectrum");
        var focus = Application.Current!.TryFindResource("AxFocusOutlineWidth", out var width) && width is double outline
            ? outline
            : 0;
        var inner = Math.Min(wheel.Bounds.Width, wheel.Bounds.Height) / 2 - wheel.RingThickness;
        var center = wheel.TranslatePoint(new Point(wheel.Bounds.Width / 2, wheel.Bounds.Height / 2), window)!.Value;

        Assert.True(wheel.RingThickness > 0, "у кольца нет толщины");

        foreach (var corner in new[] { new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(1, 1) })
        {
            var at = Point(spectrum, window, corner.X, corner.Y);
            var reach = Math.Sqrt(Math.Pow(at.X - center.X, 2) + Math.Pow(at.Y - center.Y, 2)) + focus;

            Assert.True(reach <= inner, $"угол квадрата {corner} с кольцом фокуса заходит на кольцо: {reach} > {inner}");
        }

        Assert.False(wheel.HitTest(new Point(wheel.Bounds.Width / 2, wheel.Bounds.Height / 2)), "середина кольца нажимается");
        Assert.True(wheel.HitTest(wheel.PointAt(90)), "полоса кольца не нажимается");

        window.Close();
    }

    /// <summary>
    /// Кольцо: красный справа, оттенок растёт против часовой стрелки; протяжка видна на ходу и фиксируется одной правкой,
    /// стрелки шагают на градус, с Shift — на десять, и круг замкнут.
    /// </summary>
    [AvaloniaFact]
    public void The_ring_turns_the_hue_by_drag_and_by_keys()
    {
        var (picker, window) = Shown(hsv: new HsvColor(1, 30, 0.8, 0.9));
        var (changing, committed) = Listen(picker);
        var wheel = Part<AxColorWheel>(picker, "PART_Wheel");

        // Красный справа, оттенок растёт против часовой стрелки: сверху — 90°, слева — 180°.
        Assert.Equal(0, wheel.HueAt(new Point(wheel.Bounds.Width, wheel.Bounds.Height / 2)), 6);
        Assert.Equal(90, wheel.HueAt(new Point(wheel.Bounds.Width / 2, 0)), 6);

        window.MouseDown(OnRing(wheel, window, 100), MouseButton.Left);
        window.MouseMove(OnRing(wheel, window, 140));
        window.MouseMove(OnRing(wheel, window, 120));

        Assert.True(changing.Count >= 2, $"протяжка кольца показала {changing.Count} шагов");
        Assert.Empty(committed);

        window.MouseUp(OnRing(wheel, window, 120), MouseButton.Left);

        Assert.Single(committed);
        Assert.Equal(120, picker.Hsv.H, 0);
        Assert.Equal(0.8, picker.Hsv.S, 6);
        Assert.Equal(0.9, picker.Hsv.V, 6);

        committed.Clear();
        Assert.True(wheel.Focus(NavigationMethod.Tab), "кольцо не взяло каретку");

        Press(window, Key.Home);
        Press(window, Key.Left);
        Assert.Equal(359, picker.Hsv.H, 6);

        Press(window, Key.Right, RawInputModifiers.Shift);
        Assert.Equal(9, picker.Hsv.H, 6);
        Assert.Equal(3, committed.Count);

        window.Close();
    }

    [AvaloniaFact]
    public void Escape_cancels_a_drag_and_gives_back_the_color_it_started_from()
    {
        var (picker, window) = Shown();
        var (_, committed) = Listen(picker);
        var cancelled = 0;
        var before = picker.Color;

        picker.ColorCancelled += (_, _) => cancelled++;

        var wheel = Part<AxColorWheel>(picker, "PART_Wheel");

        window.MouseDown(OnRing(wheel, window, 10), MouseButton.Left);
        window.MouseMove(OnRing(wheel, window, 120));

        Assert.NotEqual(before, picker.Color);

        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, string.Empty);
        window.MouseUp(OnRing(wheel, window, 120), MouseButton.Left);

        Assert.Equal(before, picker.Color);
        Assert.Equal(1, cancelled);
        Assert.Empty(committed);

        window.Close();
    }

    /// <summary>Стрелки по спектру: ←/→ — насыщенность, ↑/↓ — яркость, с Shift вдесятеро; каждый шаг фиксируется.</summary>
    [AvaloniaFact]
    public void Arrows_step_the_spectrum_and_each_step_commits()
    {
        var (picker, window) = Shown(hsv: new HsvColor(1, 120, 0.5, 0.5));
        var (_, committed) = Listen(picker);
        var spectrum = Part<AxColorSpectrum>(picker, "PART_Spectrum");

        Assert.True(spectrum.Focus(NavigationMethod.Tab), "спектр не взял каретку");

        Press(window, Key.Right);
        Press(window, Key.Up, RawInputModifiers.Shift);

        Assert.Equal(0.51, picker.Hsv.S, 6);
        Assert.Equal(0.6, picker.Hsv.V, 6);
        Assert.Equal(120, picker.Hsv.H, 6);
        Assert.Equal(2, committed.Count);

        window.Close();
    }

    /// <summary>
    /// Один цвет в трёх видах каналов: RGB 0…255 — целыми, RGB 0…1 — долями, HSV — градусами и сотыми; подписи и
    /// ползунки идут за видом, выбранным в списке.
    /// </summary>
    [AvaloniaFact]
    public void Three_modes_show_one_color()
    {
        var (picker, window) = Shown(color: 0xFF3366CC);
        var mode = Part<AxComboBox>(picker, "PART_Mode");

        Assert.Equal(AxColorPickerMode.Rgb255, picker.Mode);
        Assert.Equal(["R", "G", "B", "A"], Labels(picker));
        Assert.Equal([51, 102, 204, 255], Numbers(picker));

        mode.SelectedIndex = 1;
        window.UpdateLayout();

        Assert.Equal(AxColorPickerMode.Rgb1, picker.Mode);
        Assert.Equal([0.2, 0.4, 0.8, 1], Numbers(picker));

        picker.Mode = AxColorPickerMode.Hsv;
        window.UpdateLayout();

        Assert.Equal(2, mode.SelectedIndex);
        Assert.Equal(["H", "S", "V", "A"], Labels(picker));
        Assert.Equal([220, 75, 80, 255], Numbers(picker));
        Assert.Equal(AxColorChannel.Hue, Part<AxColorSlider>(picker, "PART_Slider0").Channel);
        Assert.Equal(Color.FromUInt32(0xFF3366CC), picker.Color);

        window.Close();
    }

    /// <summary>
    /// Дорожка канала — его градиент при остальных каналах: у красного при G и B — от (0, G, B) до (255, G, B), как у
    /// Unity, а ручка стоит на значении канала.
    /// </summary>
    [AvaloniaFact]
    public void A_channel_track_runs_through_its_channel_at_the_others()
    {
        var (picker, window) = Shown(color: 0xFF3366CC);
        var red = Part<AxColorSlider>(picker, "PART_Slider0");
        var gradient = Assert.IsType<LinearGradientBrush>(Part<Border>(red, "PART_Gradient").Background);

        Assert.Equal(AxColorChannel.Red, red.Channel);
        Assert.Equal(Color.FromRgb(0, 0x66, 0xCC), gradient.GradientStops[0].Color);
        Assert.Equal(Color.FromRgb(0xFF, 0x66, 0xCC), gradient.GradientStops[^1].Color);
        Assert.Equal(0x33 / 255d, red.Value, 6);

        window.Close();
    }

    /// <summary>Шаг ползунка канала RGB — единица из 255, и каждый шаг фиксируется; число строки идёт за ним.</summary>
    [AvaloniaFact]
    public void An_rgb_slider_steps_by_one_level_and_the_number_follows()
    {
        var (picker, window) = Shown(color: 0xFF3366CC);
        var (_, committed) = Listen(picker);
        var green = Part<AxColorSlider>(picker, "PART_Slider1");

        Assert.True(green.Focus(NavigationMethod.Tab), "ползунок зелёного не взял каретку");

        Press(window, Key.Right);
        Press(window, Key.Right, RawInputModifiers.Shift);

        Assert.Equal(0x66 + 11, picker.Color.G);
        Assert.Equal(0x33, picker.Color.R);
        Assert.Equal(0xCC, picker.Color.B);
        Assert.Equal(2, committed.Count);
        Assert.Equal(0x66 + 11, Part<AxNumberField>(picker, "PART_Number1").Value);

        window.Close();
    }

    /// <summary>Диктору ползунок — диапазон: значение в единицах канала ставится и фиксируется.</summary>
    [AvaloniaFact]
    public void The_slider_is_a_range_for_the_screen_reader()
    {
        var (picker, window) = Shown();
        var (_, committed) = Listen(picker);
        var alpha = Part<AxColorSlider>(picker, "PART_SliderA");
        var range = Assert.IsAssignableFrom<IRangeValueProvider>(ControlAutomationPeer.CreatePeerForElement(alpha));

        Assert.Equal(1, range.Maximum);
        Assert.Equal("Opacity", ControlAutomationPeer.CreatePeerForElement(alpha).GetName());

        range.SetValue(0.25);

        Assert.InRange(picker.Color.A, (byte)63, (byte)64);
        Assert.Single(committed);

        window.Close();
    }

    /// <summary>
    /// Под ползунком непрозрачности — шахматка: прозрачный конец иначе не отличить от фона. Без прозрачности строки
    /// непрозрачности нет вовсе.
    /// </summary>
    [AvaloniaFact]
    public void Only_the_alpha_slider_lies_on_the_checker()
    {
        var (picker, window) = Shown();

        Assert.True(Part<Border>(Part<AxColorSlider>(picker, "PART_SliderA"), "PART_Checker").IsVisible, "у непрозрачности нет шахматки");
        Assert.False(Part<Border>(Part<AxColorSlider>(picker, "PART_Slider0"), "PART_Checker").IsVisible, "у красного шахматка");

        picker.AllowsAlpha = false;
        window.UpdateLayout();

        Assert.False(Part<AxColorSlider>(picker, "PART_SliderA").IsVisible, "без прозрачности ползунок непрозрачности остался");
        Assert.False(Part<AxNumberField>(picker, "PART_NumberA").IsVisible, "без прозрачности число непрозрачности осталось");

        window.Close();
    }

    /// <summary>
    /// Число канала и код правят цвет одной фиксацией. Код — шесть цифр без прозрачности, как у Unity: набранные шесть
    /// цифр оставляют прозрачность прежней, восемь ставят и её. Хозяину набранное уходит текстом, только если разметка
    /// прочтёт его тем же цветом: «Navy» — да, цифры без решётки — нет.
    /// </summary>
    [AvaloniaFact]
    public void Numbers_and_the_code_commit_one_color_each()
    {
        var (picker, window) = Shown(color: 0x803C6ECF);
        var (_, committed) = Listen(picker);
        var texts = new List<string?>();

        picker.ColorCommitted += (_, e) => texts.Add(e.Text);

        var code = Part<TextBox>(picker, "PART_Hex");

        Assert.Equal("3C6ECF", code.Text);

        Type(window, Part<AxNumberField>(picker, "PART_Number0"), "255");
        Assert.Equal(byte.MaxValue, picker.Color.R);

        Type(window, code, "112233");
        Assert.Equal(Color.FromUInt32(0x80112233), picker.Color);

        Type(window, code, "#FF000080");
        Assert.Equal(Colors.Navy, picker.Color);

        // Имя того же цвета — тоже фиксация: цвет прежний, а записать его просят словом.
        Type(window, code, "Navy");
        Type(window, code, "Red");

        Assert.Equal(Colors.Red, picker.Color);
        Assert.Equal("FF0000", code.Text);
        Assert.Equal(5, committed.Count);
        Assert.Equal([null, null, "#FF000080", "Navy", "Red"], texts);

        window.Close();
    }

    /// <summary>
    /// Образцы «стало | было»: «было» — цвет начала, щелчок по нему возвращает его одной фиксацией. Без цвета начала
    /// образец перечёркнут и не нажимается.
    /// </summary>
    [AvaloniaFact]
    public void The_original_swatch_brings_the_starting_color_back()
    {
        var (picker, window) = Shown(color: 0xFF3C6ECF);
        var (_, committed) = Listen(picker);
        var original = Part<Button>(picker, "PART_Original");

        picker.OriginalColor = Color.FromUInt32(0xFF3C6ECF);
        picker.Color = Colors.Orange;
        window.UpdateLayout();

        Assert.Equal(Colors.Orange, Part<AxColorSwatch>(picker, "PART_New").Color);
        Assert.Equal(Color.FromUInt32(0xFF3C6ECF), Part<AxColorSwatch>(picker, "PART_OriginalSwatch").Color);

        original.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.Equal(Color.FromUInt32(0xFF3C6ECF), picker.Color);
        Assert.Single(committed);

        picker.OriginalColor = null;
        window.UpdateLayout();

        Assert.True(Part<AxColorSwatch>(picker, "PART_OriginalSwatch").IsNone, "без цвета начала «было» не перечёркнуто");
        Assert.False(original.IsEnabled, "без цвета начала «было» нажимается");

        window.Close();
    }

    /// <summary>
    /// Крупный текст раздвигает выбор, а не выталкивает код за край: ширина выбора — наименьшая, а не прибитая. Живая
    /// проверка с текстом в 200 % нашла код за краем окна (запись 375).
    /// </summary>
    [AvaloniaFact]
    public void Large_text_widens_the_picker_instead_of_pushing_the_code_out()
    {
        var (picker, window) = Shown();

        // В окне «Цвет» выбор стоит по своему размеру, а не растянутым на всю ширину.
        picker.HorizontalAlignment = HorizontalAlignment.Left;
        window.UpdateLayout();

        var width = picker.Bounds.Width;

        picker.FontSize *= 2;
        window.UpdateLayout();

        var code = Part<TextBox>(picker, "PART_Hex");
        var edge = code.TranslatePoint(new Point(code.Bounds.Width, 0), picker)!.Value.X;

        Assert.True(picker.Bounds.Width > width, $"выбор не стал шире: {picker.Bounds.Width}");
        Assert.True(edge <= picker.Bounds.Width + 0.5, $"код вытолкнут за край выбора: {edge} > {picker.Bounds.Width}");

        window.Close();
    }

    private static (AxColorPicker Picker, Window Window) Shown(HsvColor? hsv = null, uint color = 0xFF3C6ECF)
    {
        var picker = new AxColorPicker();

        if (hsv is { } given)
            picker.Hsv = given;
        else
            picker.Color = Color.FromUInt32(color);

        var window = new Window
        {
            Width = 400,
            Height = 700,
            Content = new Border { Padding = new Thickness(20), Child = picker },
        };

        window.Show();
        window.UpdateLayout();

        return (picker, window);
    }

    private static (List<Color> Changing, List<Color> Committed) Listen(AxColorPicker picker)
    {
        var changing = new List<Color>();
        var committed = new List<Color>();

        picker.ColorChanging += (_, e) => changing.Add(e.Color);
        picker.ColorCommitted += (_, e) => committed.Add(e.Color);

        return (changing, committed);
    }

    private static string[] Labels(AxColorPicker picker) =>
        Rows.Select(row => Part<TextBlock>(picker, "PART_Label" + row).Text ?? string.Empty).ToArray();

    private static double[] Numbers(AxColorPicker picker) =>
        Rows.Select(row => Part<AxNumberField>(picker, "PART_Number" + row).Value).ToArray();

    private static T Part<T>(Control owner, string name)
        where T : Control =>
        owner.GetVisualDescendants().OfType<T>().First(part => part.Name == name);

    /// <summary>Точка внутри части в долях её ширины и высоты — в координатах окна.</summary>
    private static Point Point(Control part, Window window, double x, double y) =>
        part.TranslatePoint(new Point(part.Bounds.Width * x, part.Bounds.Height * y), window)!.Value;

    /// <summary>Точка на середине полосы кольца у оттенка — в координатах окна.</summary>
    private static Point OnRing(AxColorWheel wheel, Window window, double hue) =>
        wheel.TranslatePoint(wheel.PointAt(hue), window)!.Value;

    private static void Type(Window window, TextBox box, string text)
    {
        box.Focus();
        box.SelectAll();
        window.KeyTextInput(text);
        Press(window, Key.Enter);
    }

    private static void Press(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, string.Empty);
        window.UpdateLayout();
    }
}
