using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Поле объекта, как ObjectField у Unity: миниатюра и имя, заглушка у пустого, ⊙ просит выбор, крестик снимает, цель броска
/// подсвечена.
/// </summary>
public class ObjectFieldTests
{
    /// <summary>
    /// Пустое поле — заглушка вторичным тоном и без крестика; с объектом — его имя, миниатюра и крестик; у разных значений —
    /// прочерк.
    /// </summary>
    [AvaloniaFact]
    public void An_empty_field_shows_its_placeholder_and_a_chosen_one_its_name_and_thumbnail()
    {
        var (field, window) = Shown();
        var text = Part<TextBlock>(field, "PART_Text");

        Assert.Equal("None (Image)", text.Text);
        Assert.True(field.TryFindResource("AxTextSecondaryBrush", field.ActualThemeVariant, out var secondary), "вторичного тона нет");
        Assert.Same(secondary, text.Foreground);
        Assert.False(Part<Control>(field, "PART_Clear").IsVisible, "у пустого поля крестик");
        Assert.False(Part<Control>(field, "PART_Thumbnail").IsVisible, "у поля без миниатюры место под неё");

        field.Text = "logo.png";
        field.Thumbnail = new DrawingImage();
        Settle(window);

        Assert.Equal("logo.png", text.Text);
        Assert.NotSame(secondary, text.Foreground);
        Assert.True(Part<Control>(field, "PART_Clear").IsVisible, "крестика нет, хотя снять есть что");
        Assert.True(Part<Control>(field, "PART_Thumbnail").IsVisible, "миниатюры нет");

        field.IsMixed = true;
        Settle(window);

        Assert.Equal("—", text.Text);

        window.Close();
    }

    /// <summary>Выбор просят ⊙ и клавиши Enter, пробел, Alt+↓ и F4; снятие — крестик, Delete и Backspace, и только у непустого.</summary>
    [AvaloniaFact]
    public void The_target_and_the_keys_ask_for_a_choice_and_the_cross_and_delete_clear()
    {
        var (field, window) = Shown();
        var picks = 0;
        var clears = 0;

        field.PickRequested += (_, _) => picks++;
        field.Cleared += (_, _) => clears++;

        Click(window, Part<Button>(field, "PART_Pick"));

        Assert.Equal(1, picks);

        field.Focus();
        Press(window, Key.Enter);
        Press(window, Key.Space);
        Press(window, Key.F4);
        Press(window, Key.Down, RawInputModifiers.Alt);

        Assert.Equal(5, picks);

        Press(window, Key.Delete);

        Assert.Equal(0, clears);

        field.Text = "logo.png";
        Settle(window);

        Press(window, Key.Delete);
        Press(window, Key.Back);
        Click(window, Part<Button>(field, "PART_Clear"));

        Assert.Equal(3, clears);
        Assert.True(field.IsFocused, "кнопка в поле взяла каретку");

        window.Close();
    }

    /// <summary>Только для чтения — ни выбора, ни снятия: кнопок нет, клавиши молчат, рамки и подложки нет.</summary>
    [AvaloniaFact]
    public void A_read_only_field_neither_chooses_nor_clears()
    {
        var (field, window) = Shown();
        var asked = 0;

        field.Text = "logo.png";
        field.IsReadOnly = true;
        field.PickRequested += (_, _) => asked++;
        field.Cleared += (_, _) => asked++;
        Settle(window);

        Assert.False(Part<Control>(field, "PART_Pick").IsVisible, "у поля только для чтения ⊙");
        Assert.False(Part<Control>(field, "PART_Clear").IsVisible, "у поля только для чтения крестик");

        field.Focus();
        Press(window, Key.Enter);
        Press(window, Key.Delete);

        Assert.Equal(0, asked);
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(Part<Border>(field, "PART_BorderElement").Background).Color);

        window.Close();
    }

    /// <summary>
    /// Поле — одна остановка Tab, и каретка в нём зажигает рамку и кольцо; цель броска — рамка акцентом поверх и подложка
    /// сведений.
    /// </summary>
    [AvaloniaFact]
    public void The_field_is_one_tab_stop_with_a_ring_and_a_drop_target_is_outlined()
    {
        var (field, window) = Shown();
        var before = window.GetVisualDescendants().OfType<AxButton>().Single(button => Equals(button.Content, "Before"));

        before.Focus(NavigationMethod.Tab);
        Press(window, Key.Tab);

        Assert.True(field.IsFocused, "Tab не встал на поле");
        Assert.True(Part<Control>(field, "PART_FocusRing").IsVisible, "каретка в поле не зажгла кольцо");

        Press(window, Key.Tab);

        Assert.False(field.IsKeyboardFocusWithin, "Tab остался внутри поля — на его кнопке");

        field.IsDropTarget = true;
        Settle(window);

        Assert.True(Part<Control>(field, "PART_DropTarget").IsVisible, "цель броска без рамки");
        Assert.True(field.TryFindResource("AxInfoFillBrush", field.ActualThemeVariant, out var fill), "подложки сведений нет");
        Assert.Same(fill, Part<Border>(field, "PART_BorderElement").Background);

        window.Close();
    }

    /// <summary>Диктору поле — выбор со значением: имя, заглушка или прочерк; вызов просит выбор.</summary>
    [AvaloniaFact]
    public void The_screen_reader_hears_the_value_and_invoking_asks_for_a_choice()
    {
        var (field, window) = Shown();
        var picks = 0;

        field.PickRequested += (_, _) => picks++;

        var peer = ControlAutomationPeer.CreatePeerForElement(field);
        var value = Assert.IsAssignableFrom<IValueProvider>(peer);

        Assert.Equal(AutomationControlType.ComboBox, peer.GetAutomationControlType());
        Assert.Equal("None (Image)", value.Value);
        Assert.True(value.IsReadOnly);

        field.Text = "logo.png";

        Assert.Equal("logo.png", value.Value);

        Assert.IsAssignableFrom<IInvokeProvider>(peer).Invoke();

        Assert.Equal(1, picks);

        window.Close();
    }

    /// <summary>В тесной зоне поле — высоты тесного контрола, как поле ввода.</summary>
    [AvaloniaFact]
    public void A_compact_field_is_as_high_as_a_compact_control()
    {
        var (field, window) = Shown();

        field.Size = AxControlSize.Compact;
        Settle(window);

        Assert.True(field.TryFindResource("AxControlHeightCompact", out var compact) && compact is double height, "тесной высоты нет");
        Assert.Equal((double)compact!, field.Bounds.Height);

        window.Close();
    }

    /// <summary>
    /// Кнопки ⊙ и крестик — во всю высоту поля внутри рамки, а не ростом значка: промах на пару точек мимо значка
    /// ставил бы каретку вместо выбора.
    /// </summary>
    [AvaloniaFact]
    public void The_buttons_take_the_whole_height_inside_the_frame()
    {
        var (field, window) = Shown();

        field.Text = "star.png";
        field.Size = AxControlSize.Compact;
        Settle(window);

        foreach (var name in new[] { "PART_Pick", "PART_Clear" })
        {
            var button = Part<Button>(field, name);
            var row = (Control)button.GetVisualParent()!;

            Assert.True(button.Bounds.Height > 0, $"{name} не показан");
            Assert.Equal(row.Bounds.Height, button.Bounds.Height);
        }

        window.Close();
    }

    private static (AxObjectField Field, Window Window) Shown()
    {
        var field = new AxObjectField { PlaceholderText = "None (Image)", Width = 220, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        var window = new Window
        {
            Width = 320,
            Height = 200,
            Content = new StackPanel
            {
                Children =
                {
                    new AxButton { Content = "Before" },
                    field,
                    new AxButton { Content = "After" },
                },
            },
        };

        window.Show();
        Settle(window);

        return (field, window);
    }

    private static T Part<T>(Control owner, string name)
        where T : Control =>
        owner.GetVisualDescendants().OfType<T>().First(part => part.Name == name);

    private static void Click(Window window, Control control)
    {
        var at = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        Settle(window);
    }

    private static void Press(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, string.Empty);
        window.KeyRelease(key, modifiers, PhysicalKey.None, string.Empty);
        Settle(window);
    }

    /// <summary>Раскладка и такт отрисовки: попадание указателя безголовая платформа считает по отрисованной сцене.</summary>
    private static void Settle(Window window)
    {
        window.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
