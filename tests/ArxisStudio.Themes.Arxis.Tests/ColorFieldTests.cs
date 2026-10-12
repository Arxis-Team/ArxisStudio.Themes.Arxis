using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Поле цвета, как у Unity: полоса цвета с полоской прозрачности, пипетка и окно «Цвет».
/// </summary>
/// <remarks>
/// Текста в поле нет: код читают подсказкой и диктором, а набирают в окне цвета. Окно открывается с цветом поля и
/// кареткой в квадрате; сеанс в нём — <see cref="ColorWindowTests"/>. Пипетка берёт пиксель экрана и фиксирует его
/// одним событием.
/// </remarks>
public sealed class ColorFieldTests : IDisposable
{
    /// <summary>Окно цвета одно на процесс: тест, упавший с открытым, не оставляет его следующему.</summary>
    public void Dispose() => AxColorPickerWindow.Current?.Close();

    /// <summary>Код цвета — подсказкой полосы и для диктора; без прозрачности — шесть цифр.</summary>
    [AvaloniaFact]
    public void The_code_is_the_swatchs_tip_and_without_alpha_it_has_six_digits()
    {
        var (field, window) = Shown(0x803C6ECF);

        Assert.Equal("#803C6ECF", field.ColorText);
        Assert.Equal("#803C6ECF", AutomationProperties.GetHelpText(Swatch(field)));
        Assert.Equal("#803C6ECF", ToolTip.GetTip(Swatch(field)));

        field.AllowsAlpha = false;

        Assert.Equal("#3C6ECF", field.ColorText);

        window.Close();
    }

    /// <summary>Разные значения — прочерк вместо цвета, и подсказка говорит почему.</summary>
    [AvaloniaFact]
    public void Mixed_shows_a_dash_and_no_swatch()
    {
        var (field, window) = Shown(0xFF3C6ECF, setup: field => field.IsMixed = true);

        Assert.Null(field.SwatchBrush);
        Assert.Null(field.ColorText);
        Assert.False(Part<AxColorSwatch>(field, "PART_Color").IsVisible, "у разных значений виден цвет");
        Assert.True(Part<TextBlock>(field, "PART_Mixed").IsVisible, "у разных значений нет прочерка");
        Assert.Equal(Resource("AxTextMixedValues"), AutomationProperties.GetHelpText(Swatch(field)));

        field.IsMixed = false;

        Assert.Equal(Color.FromUInt32(0xFF3C6ECF), Assert.IsAssignableFrom<ISolidColorBrush>(field.SwatchBrush).Color);
        Assert.True(Part<AxColorSwatch>(field, "PART_Color").IsVisible);

        window.Close();
    }

    /// <summary>Нет цвета — перечёркнутый образец, и диктор слышит «нет цвета»; выбранный цвет снимает отметку.</summary>
    [AvaloniaFact]
    public void No_color_is_a_crossed_swatch_until_a_color_is_chosen()
    {
        var (field, window) = Shown(0xFFFFFFFF, setup: field => field.IsNone = true);

        Assert.True(Part<AxColorSwatch>(field, "PART_Color").IsNone, "образец «нет цвета» не перечёркнут");
        Assert.Null(field.ColorText);
        Assert.Equal(Resource("AxTextColorNone"), AutomationProperties.GetHelpText(Swatch(field)));

        Assert.True(Swatch(field).Focus(NavigationMethod.Tab));
        Press(window, Key.Enter);
        Pump();

        var picker = Assert.IsType<AxColorPicker>(field.Picker);
        var original = picker.GetVisualDescendants().OfType<Button>().Single(part => part.Name == "PART_Original");

        Assert.Null(picker.OriginalColor);
        Assert.False(original.IsEnabled, "у «нет цвета» есть цвет начала, который можно вернуть");

        picker.RaiseEvent(new AxColorEventArgs(AxColorPicker.ColorCommittedEvent, Colors.Red));

        Assert.False(field.IsNone, "выбранный цвет не снял отметку «нет цвета»");
        Assert.Equal(Colors.Red, field.Color);

        window.Close();
    }

    /// <summary>
    /// У поля без цвета показанный цвет — лишь место: тот же белый, выбранный образцом палитры, — выбор, и поле получает
    /// цвет.
    /// </summary>
    [AvaloniaFact]
    public void The_shown_color_picked_from_the_palette_for_no_color_is_a_choice()
    {
        var (field, window) = Shown(0xFFFFFFFF, setup: field => field.IsNone = true);
        var committed = Committed(field);

        Assert.True(Swatch(field).Focus(NavigationMethod.Tab));
        Press(window, Key.Enter);
        Pump();

        var colors = Assert.IsType<AxColorPickerWindow>(AxColorPickerWindow.Current);
        var picker = Assert.IsType<AxColorPicker>(field.Picker);

        picker.IsPaletteExpanded = true;
        Pump();

        var white = picker.GetVisualDescendants().OfType<Button>().First(part => part.Tag is Color color && color == Colors.White);

        white.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.False(field.IsNone, "показанный белый, выбранный образцом, не снял отметку «нет цвета»");

        colors.Close();
        Pump();

        Assert.Equal(Colors.White, Assert.Single(committed).Color);

        window.Close();
    }

    /// <summary>
    /// У разных значений показан цвет первого: его же код, подтверждённый Enter, сводит их к нему.
    /// </summary>
    [AvaloniaFact]
    public void The_shown_code_confirmed_with_enter_for_mixed_values_is_a_choice()
    {
        var (field, window) = Shown(0xFF3C6ECF, setup: field => field.IsMixed = true);
        var committed = Committed(field);

        Assert.True(Swatch(field).Focus(NavigationMethod.Tab));
        Press(window, Key.Enter);
        Pump();

        var colors = Assert.IsType<AxColorPickerWindow>(AxColorPickerWindow.Current);
        var picker = Assert.IsType<AxColorPicker>(field.Picker);
        var hex = picker.GetVisualDescendants().OfType<TextBox>().Single(part => part.Name == "PART_Hex");

        Assert.True(hex.Focus(), "запись кода не взяла каретку");

        colors.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, string.Empty);

        Assert.False(field.IsMixed, "показанный код, подтверждённый Enter, не свёл разные значения");

        colors.Close();
        Pump();

        Assert.Equal(Color.FromUInt32(0xFF3C6ECF), Assert.Single(committed).Color);

        window.Close();
    }

    /// <summary>
    /// Полоса открывает окно «Цвет» — отдельным окном над окном поля, с цветом поля, его же цветом «было» и кареткой в
    /// квадрате; шаг стрелкой там поле показывает на ходу, а Esc возвращает цвет начала, закрывает окно и возвращает
    /// каретку к полосе.
    /// </summary>
    [AvaloniaFact]
    public void The_swatch_opens_the_color_window_and_escape_comes_back()
    {
        var (field, window) = Shown(0xFF3C6ECF);
        var committed = Committed(field);
        var changing = new List<Color>();
        var cancelled = 0;
        var swatch = Swatch(field);

        field.ColorChanging += (_, e) => changing.Add(e.Color);
        field.ColorCancelled += (_, _) => cancelled++;

        Assert.True(swatch.Focus(NavigationMethod.Tab), "полоса не взяла каретку");
        Press(window, Key.Enter);
        Pump();

        Assert.True(field.IsPickerOpen, "Enter на полосе не открыл окно цвета");

        var colors = Assert.IsType<AxColorPickerWindow>(AxColorPickerWindow.Current);
        var picker = Assert.IsType<AxColorPicker>(field.Picker);
        var spectrum = picker.GetVisualDescendants().OfType<AxColorSpectrum>().Single();

        Assert.Same(colors.Picker, picker);
        Assert.Same(window, colors.Owner);
        Assert.Equal(field.Color, picker.Color);
        Assert.Equal(field.Color, picker.OriginalColor);
        Assert.True(spectrum.IsKeyboardFocusWithin, "каретка не пришла в квадрат");

        colors.KeyPress(Key.Down, RawInputModifiers.Shift, PhysicalKey.None, string.Empty);

        Assert.Single(changing);
        Assert.Empty(committed);
        Assert.Equal(picker.Color, field.Color);
        Assert.NotEqual(Color.FromUInt32(0xFF3C6ECF), field.Color);

        colors.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, string.Empty);
        Pump();

        Assert.False(field.IsPickerOpen, "Esc не закрыл окно цвета");
        Assert.Null(AxColorPickerWindow.Current);
        Assert.Equal(Color.FromUInt32(0xFF3C6ECF), field.Color);
        Assert.Equal(1, cancelled);
        Assert.Empty(committed);
        Assert.True(swatch.IsFocused, "каретка не вернулась к полосе");

        window.Close();
    }

    /// <summary>
    /// Пипетка поля берёт пиксель под щелчком и фиксирует его одним событием — без выбора цвета; цвет экрана плотный.
    /// </summary>
    [AvaloniaFact]
    public void The_eyedropper_takes_the_clicked_pixel_and_commits_it_once()
    {
        var (field, window) = Shown(0x803C6ECF);
        var committed = Committed(field);

        using var screen = Eyedroppers.Screen(40, 30, (10, 5, Colors.OrangeRed));

        Eyedropper(field).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump();

        var overlay = Assert.Single(AxEyedropper.Overlays);

        overlay.MouseMove(new Point(10.5, 5.5));
        overlay.MouseDown(new Point(10.5, 5.5), MouseButton.Left);
        Pump();

        var (color, text) = Assert.Single(committed);

        Assert.Equal(Colors.OrangeRed, color);
        Assert.Null(text);
        Assert.Equal(Colors.OrangeRed, field.Color);
        Assert.False(field.IsPickerOpen, "пипетка открыла выбор цвета");
        Assert.Empty(AxEyedropper.Overlays);

        window.Close();
    }

    /// <summary>Esc отменяет пипетку: ни цвета, ни фиксации.</summary>
    [AvaloniaFact]
    public void Escape_cancels_the_eyedropper_and_commits_nothing()
    {
        var (field, window) = Shown(0xFF3C6ECF);
        var committed = Committed(field);

        using var screen = Eyedroppers.Screen(40, 30, (10, 5, Colors.OrangeRed));

        Eyedropper(field).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump();

        Assert.Single(AxEyedropper.Overlays).KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, string.Empty);
        Pump();

        Assert.Empty(committed);
        Assert.Equal(Color.FromUInt32(0xFF3C6ECF), field.Color);
        Assert.Empty(AxEyedropper.Overlays);

        window.Close();
    }

    /// <summary>Поле только для чтения не открывает ни выбор, ни пипетку.</summary>
    [AvaloniaFact]
    public void A_read_only_field_opens_neither_the_picker_nor_the_eyedropper()
    {
        var (field, window) = Shown(0xFFE0A040, setup: field => field.IsReadOnly = true);

        Assert.False(Swatch(field).IsEnabled, "полоса поля только для чтения нажимается");
        Assert.False(Eyedropper(field).IsEnabled, "пипетка поля только для чтения нажимается");

        field.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down, KeyModifiers = KeyModifiers.Alt });

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
        Pump();

        return (field, window);
    }

    private static List<(Color Color, string? Text)> Committed(AxColorField field)
    {
        var committed = new List<(Color, string?)>();

        field.ColorCommitted += (_, e) => committed.Add((e.Color, e.Text));

        return committed;
    }

    private static Button Swatch(AxColorField field) => Part<Button>(field, "PART_Swatch");

    private static Button Eyedropper(AxColorField field) => Part<Button>(field, "PART_Eyedropper");

    private static T Part<T>(AxColorField field, string name)
        where T : Control =>
        field.GetVisualDescendants().OfType<T>().Single(part => part.Name == name);

    private static object? Resource(string key) =>
        Application.Current!.TryFindResource(key, out var value) ? value : null;

    private static void Press(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, string.Empty);
        window.UpdateLayout();
    }

    /// <summary>Прокачивает диспетчер: каретку в спектр выбор ставит отложенно, окна пипетки — тоже.</summary>
    private static void Pump()
    {
        for (var at = 0; at < 5; at++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }
}
