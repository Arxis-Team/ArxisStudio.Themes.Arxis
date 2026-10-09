using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Поле цвета: запись, образец и выбор под полем.
/// </summary>
/// <remarks>
/// Запись правит текст разметки и фиксируется с набранным текстом — хозяин решает, писать ли «Red»; выбор
/// под полем открывается с цветом поля, его события идут к хозяину сквозь поле, а Esc закрывает его и
/// возвращает каретку к образцу.
/// </remarks>
public class ColorFieldTests
{
    [AvaloniaFact]
    public void A_typed_name_commits_with_its_text_and_shows_the_code()
    {
        var (field, window) = Shown(0xFF3C6ECF);
        var committed = Committed(field);

        Type(window, field, "Red");
        Press(window, Key.Enter);

        var (color, text) = Assert.Single(committed);

        Assert.Equal(Colors.Red, color);
        Assert.Equal("Red", text);
        Assert.Equal(Colors.Red, field.Color);
        Assert.Equal("#FF0000", Input(field).Text);

        window.Close();
    }

    [AvaloniaFact]
    public void An_unreadable_code_stays_marked_and_the_color_stays()
    {
        var (field, window) = Shown(0xFF3C6ECF);
        var committed = Committed(field);

        Type(window, field, "zzz");
        Press(window, Key.Enter);

        Assert.Equal(Color.FromUInt32(0xFF3C6ECF), field.Color);
        Assert.Equal("zzz", Input(field).Text);
        Assert.True(Input(field).Classes.Contains(":validation-error"), "непрочитанное не отмечено ошибкой");
        Assert.Empty(committed);

        Press(window, Key.Escape);
        Assert.Equal("#3C6ECF", Input(field).Text);

        window.Close();
    }

    /// <summary>Каретка, прошедшая сквозь запись, ничего не фиксирует: показанное полем — не набранное.</summary>
    [AvaloniaFact]
    public void Passing_through_the_code_commits_nothing()
    {
        var (field, window) = Shown(0xFF3C6ECF);
        var committed = Committed(field);

        Input(field).Focus();
        field.Color = Color.FromUInt32(0xFF20A060);
        window.GetVisualDescendants().OfType<AxButton>().First(button => Equals(button.Content, "beside")).Focus();

        Assert.Empty(committed);
        Assert.Equal("#20A060", Input(field).Text);

        window.Close();
    }

    /// <summary>Без прозрачности цвет всегда плотный, а запись — шесть цифр.</summary>
    [AvaloniaFact]
    public void Without_alpha_the_color_is_opaque()
    {
        var (field, window) = Shown(0xFF20A060, setup: field => field.AllowsAlpha = false);

        Type(window, field, "#80FF0000");
        Press(window, Key.Enter);

        Assert.Equal(Colors.Red, field.Color);
        Assert.Equal("#FF0000", Input(field).Text);

        window.Close();
    }

    /// <summary>Разные значения — пустая запись с прочерком и образец без цвета.</summary>
    [AvaloniaFact]
    public void Mixed_shows_a_dash_and_no_swatch()
    {
        var (field, window) = Shown(0xFF3C6ECF, setup: field => field.IsMixed = true);

        Assert.Equal(string.Empty, Input(field).Text);
        Assert.Equal("—", Input(field).PlaceholderText);
        Assert.Null(field.SwatchBrush);

        field.IsMixed = false;
        Assert.Equal(Color.FromUInt32(0xFF3C6ECF), Assert.IsAssignableFrom<ISolidColorBrush>(field.SwatchBrush).Color);

        window.Close();
    }

    /// <summary>
    /// Образец открывает выбор с цветом поля и кареткой в спектре; шаг стрелкой там — фиксация, которую слышит
    /// хозяин поля, а Esc закрывает выбор и возвращает каретку к образцу.
    /// </summary>
    [AvaloniaFact]
    public void The_swatch_opens_the_picker_whose_steps_reach_the_owner_and_escape_comes_back()
    {
        var (field, window) = Shown(0xFF3C6ECF);
        var committed = Committed(field);
        var swatch = Swatch(field);

        Assert.True(swatch.Focus(NavigationMethod.Tab), "образец не взял каретку");
        Press(window, Key.Enter);
        Pump();

        Assert.True(field.IsPickerOpen, "Enter на образце не открыл выбор");

        var picker = Assert.IsType<AxColorPicker>(field.Picker);
        var spectrum = picker.GetVisualDescendants().OfType<AxColorSpectrum>().Single();

        Assert.Equal(field.Color, picker.Color);
        Assert.True(spectrum.IsKeyboardFocusWithin, "каретка не пришла в спектр");

        var root = Assert.IsAssignableFrom<TopLevel>(TopLevel.GetTopLevel(spectrum));

        root.KeyPress(Key.Down, RawInputModifiers.Shift, PhysicalKey.None, string.Empty);

        var (color, text) = Assert.Single(committed);

        Assert.Null(text);
        Assert.Equal(color, field.Color);
        Assert.NotEqual(Color.FromUInt32(0xFF3C6ECF), field.Color);

        root.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, string.Empty);
        Pump();

        Assert.False(field.IsPickerOpen, "Esc не закрыл выбор");
        Assert.True(swatch.IsFocused, "каретка не вернулась к образцу");

        window.Close();
    }

    [AvaloniaFact]
    public void A_read_only_field_does_not_open_the_picker()
    {
        var (field, window) = Shown(0xFFE0A040, setup: field => field.IsReadOnly = true);

        Assert.False(Swatch(field).IsEnabled, "образец поля только для чтения нажимается");

        field.Focus();
        Input(field).Focus();
        Press(window, Key.Down, RawInputModifiers.Alt);

        Assert.False(field.IsPickerOpen, "Alt+↓ открыл выбор у поля только для чтения");

        window.Close();
    }

    private static (AxColorField Field, Window Window) Shown(uint color, Action<AxColorField>? setup = null)
    {
        var field = new AxColorField { Color = Color.FromUInt32(color), Width = 220 };

        setup?.Invoke(field);

        var window = new Window
        {
            Width = 500,
            Height = 600,
            Content = new StackPanel { Margin = new Thickness(20), Children = { field, new AxButton { Content = "beside" } } },
        };

        window.Show();
        window.UpdateLayout();

        // Отложенное Avalonia — запоздалые уведомления о тексте — приходит здесь, как в живом окне.
        Pump();

        return (field, window);
    }

    private static List<(Color Color, string? Text)> Committed(AxColorField field)
    {
        var committed = new List<(Color, string?)>();

        field.ColorCommitted += (_, e) => committed.Add((e.Color, e.Text));

        return committed;
    }

    private static TextBox Input(AxColorField field) => Assert.IsAssignableFrom<TextBox>(field.Input);

    private static Button Swatch(AxColorField field) =>
        field.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_Swatch");

    private static void Type(Window window, AxColorField field, string text)
    {
        Input(field).Focus();
        Input(field).SelectAll();
        window.KeyTextInput(text);
    }

    private static void Press(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, string.Empty);
        window.UpdateLayout();
    }

    /// <summary>Прокачивает диспетчер: каретку в спектр выбор ставит отложенно, когда карточка на экране.</summary>
    private static void Pump()
    {
        for (var at = 0; at < 5; at++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }
}
