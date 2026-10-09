using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Выбор цвета: модель HSV, протяжка по спектру и ползунку, клавиши, каналы RGB.
/// </summary>
/// <remarks>
/// Один жест — одна фиксация: протяжку видно на ходу, а фиксируется она при отпускании, и Esc посреди неё
/// возвращает цвет начала. Оттенок живёт в модели HSV и переживает серый и чёрный.
/// </remarks>
public class ColorPickerTests
{
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

    [AvaloniaFact]
    public void Escape_cancels_a_drag_and_gives_back_the_color_it_started_from()
    {
        var (picker, window) = Shown();
        var (_, committed) = Listen(picker);
        var cancelled = 0;
        var before = picker.Color;

        picker.ColorCancelled += (_, _) => cancelled++;

        var hue = Part<AxColorSlider>(picker, "PART_Hue");

        window.MouseDown(Point(hue, window, 0.1, 0.5), MouseButton.Left);
        window.MouseMove(Point(hue, window, 0.6, 0.5));

        Assert.NotEqual(before, picker.Color);

        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, string.Empty);
        window.MouseUp(Point(hue, window, 0.6, 0.5), MouseButton.Left);

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

    /// <summary>Оттенок у правого края не доходит до 360: модель свела бы его к нулю, и ручка прыгнула бы в начало.</summary>
    [AvaloniaFact]
    public void The_hue_slider_keys_reach_both_ends_without_wrapping()
    {
        var (picker, window) = Shown();
        var hue = Part<AxColorSlider>(picker, "PART_Hue");

        Assert.True(hue.Focus(NavigationMethod.Tab), "ползунок оттенка не взял каретку");

        Press(window, Key.End);
        Assert.True(hue.Value > 359.9, $"End поставил оттенок {hue.Value}");

        Press(window, Key.Home);
        Press(window, Key.Right, RawInputModifiers.Shift);
        Assert.Equal(10, hue.Value, 6);

        window.Close();
    }

    /// <summary>Диктору ползунок — диапазон: значение в единицах канала ставится и фиксируется.</summary>
    [AvaloniaFact]
    public void The_slider_is_a_range_for_the_screen_reader()
    {
        var (picker, window) = Shown();
        var (_, committed) = Listen(picker);
        var alpha = Part<AxColorSlider>(picker, "PART_Alpha");
        var range = Assert.IsAssignableFrom<IRangeValueProvider>(ControlAutomationPeer.CreatePeerForElement(alpha));

        Assert.Equal(1, range.Maximum);
        Assert.Equal("Opacity", ControlAutomationPeer.CreatePeerForElement(alpha).GetName());

        range.SetValue(0.25);

        Assert.InRange(picker.Color.A, (byte)63, (byte)64);
        Assert.Single(committed);

        window.Close();
    }

    /// <summary>Под ползунком непрозрачности — шахматка: прозрачный конец иначе не отличить от фона.</summary>
    [AvaloniaFact]
    public void Only_the_alpha_slider_lies_on_the_checker()
    {
        var (picker, window) = Shown();

        Assert.True(Part<Border>(Part<AxColorSlider>(picker, "PART_Alpha"), "PART_Checker").IsVisible, "у непрозрачности нет шахматки");
        Assert.False(Part<Border>(Part<AxColorSlider>(picker, "PART_Hue"), "PART_Checker").IsVisible, "у оттенка шахматка");

        picker.AllowsAlpha = false;
        window.UpdateLayout();

        Assert.False(Part<AxColorSlider>(picker, "PART_Alpha").IsVisible, "без прозрачности ползунок непрозрачности остался");
        Assert.Equal(3, Part<AxVectorField>(picker, "PART_Channels").Cells.Count);

        window.Close();
    }

    /// <summary>Каналы RGB правят цвет одной фиксацией, а запись — с тем текстом, которым её набрали.</summary>
    [AvaloniaFact]
    public void Channels_and_the_code_commit_one_color_each()
    {
        var (picker, window) = Shown();
        var (_, committed) = Listen(picker);
        var texts = new List<string?>();

        picker.ColorCommitted += (_, e) => texts.Add(e.Text);

        var red = Part<AxVectorField>(picker, "PART_Channels").Cells[0];

        red.Focus();
        red.SelectAll();
        window.KeyTextInput("255");
        Press(window, Key.Enter);

        Assert.Equal(byte.MaxValue, picker.Color.R);

        var code = Part<TextBox>(picker, "PART_Hex");

        code.Focus();
        code.SelectAll();
        window.KeyTextInput("Navy");
        Press(window, Key.Enter);

        Assert.Equal(Colors.Navy, picker.Color);
        Assert.Equal("#000080", code.Text);
        Assert.Equal(2, committed.Count);
        Assert.Equal([null, "Navy"], texts);

        window.Close();
    }

    private static (AxColorPicker Picker, Window Window) Shown(HsvColor? hsv = null)
    {
        var picker = new AxColorPicker();

        if (hsv is { } given)
            picker.Hsv = given;
        else
            picker.Color = Color.FromUInt32(0xFF3C6ECF);

        var window = new Window
        {
            Width = 400,
            Height = 500,
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

    private static T Part<T>(Control owner, string name)
        where T : Control =>
        owner.GetVisualDescendants().OfType<T>().First(part => part.Name == name);

    /// <summary>Точка внутри части в долях её ширины и высоты — в координатах окна.</summary>
    private static Point Point(Control part, Window window, double x, double y) =>
        part.TranslatePoint(new Point(part.Bounds.Width * x, part.Bounds.Height * y), window)!.Value;

    private static void Press(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, string.Empty);
        window.UpdateLayout();
    }
}
