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
/// Поле цвета, как у Unity: полоса цвета с полоской прозрачности, пипетка и выбор под полем.
/// </summary>
/// <remarks>
/// Текста в поле нет: код читают подсказкой и диктором, а набирают в выборе цвета. Выбор под полем открывается с
/// цветом поля, его события идут к хозяину сквозь поле, Esc закрывает его и возвращает каретку к полосе. Пипетка
/// берёт пиксель экрана и фиксирует его одним событием.
/// </remarks>
public class ColorFieldTests
{
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

        picker.RaiseEvent(new AxColorEventArgs(AxColorPicker.ColorCommittedEvent, Colors.Red));

        Assert.False(field.IsNone, "выбранный цвет не снял отметку «нет цвета»");
        Assert.Equal(Colors.Red, field.Color);

        window.Close();
    }

    /// <summary>
    /// Полоса открывает выбор с цветом поля и кареткой в спектре; шаг стрелкой там — фиксация, которую слышит
    /// хозяин поля, а Esc закрывает выбор и возвращает каретку к полосе.
    /// </summary>
    [AvaloniaFact]
    public void The_swatch_opens_the_picker_whose_steps_reach_the_owner_and_escape_comes_back()
    {
        var (field, window) = Shown(0xFF3C6ECF);
        var committed = Committed(field);
        var swatch = Swatch(field);

        Assert.True(swatch.Focus(NavigationMethod.Tab), "полоса не взяла каретку");
        Press(window, Key.Enter);
        Pump();

        Assert.True(field.IsPickerOpen, "Enter на полосе не открыл выбор");

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
