using ArxisStudio.Controls;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Нажатие и раскрытый список: состояния, которых у флажка, переключателя и поля выбора не было.
/// </summary>
/// <remarks>
/// Контрол, не отвечающий на нажатие, оставляет промах неотличимым от попадания: человек жмёт, и
/// до самого отпускания не знает, попал ли. У флажка это заметно вдвойне — он переключается, и
/// единственным ответом было переключение, то есть последствие, а не подтверждение нажатия.
/// <para>
/// У поля выбора та же история с открытым списком: список висит над полем, а поле ничем не
/// показывает, что он его.
/// </para>
/// </remarks>
public class PressedStateTests
{
    /// <summary>Нажатый пустой флажок темнеет коробкой, отмеченный — акцентом.</summary>
    [AvaloniaTheory]
    [InlineData(false, "AxPressedColor")]
    [InlineData(true, "AxAccentFillPressedColor")]
    public void Pressed_check_box_darkens_its_box(bool isChecked, string key)
    {
        var box = new AxCheckBox { Content = "Больше не спрашивать", IsChecked = isChecked };
        var window = Shown(box);

        Press(box, window);

        Assert.Equal(Resource(window, key), Colour(Part<Border>(box, "PART_Box").Background));

        window.Close();
    }

    /// <summary>Нажатый переключатель темнеет дорожкой.</summary>
    [AvaloniaTheory]
    [InlineData(false, "AxPressedColor")]
    [InlineData(true, "AxAccentFillPressedColor")]
    public void Pressed_switch_darkens_its_track(bool isChecked, string key)
    {
        var toggle = new AxToggleSwitch { Content = "Показывать журнал", IsChecked = isChecked };
        var window = Shown(toggle);

        Press(toggle, window);

        Assert.Equal(Resource(window, key), Colour(Part<Border>(toggle, "PART_Track").Background));

        window.Close();
    }

    /// <summary>Нажатая радиокнопка темнеет серединой, выбранная — кольцом.</summary>
    [AvaloniaFact]
    public void Pressed_radio_button_answers_with_its_circle()
    {
        var radio = new AxRadioButton { Content = "Плотная" };
        var window = Shown(radio);

        Press(radio, window);

        Assert.Equal(Resource(window, "AxPressedColor"), Colour(Part<Ellipse>(radio, "PART_Circle").Fill));

        radio.IsChecked = true;
        window.UpdateLayout();

        Assert.Equal(Resource(window, "AxAccentFillPressedColor"), Colour(Part<Ellipse>(radio, "PART_Circle").Stroke));

        window.Close();
    }

    /// <summary>
    /// Пока список открыт, поле выбора держит рамку фокуса и заливку нажатия.
    /// </summary>
    /// <remarks>
    /// Состояние приходит от самой Avalonia — <c>:dropdownopen</c>, — и проверяется оно живым
    /// открытием, а не выставленным псевдоклассом: имя, которого у контрола нет, так и осталось бы
    /// правилом, которое никогда не срабатывает.
    /// </remarks>
    [AvaloniaFact]
    public void An_open_combo_box_keeps_the_focus_border()
    {
        var combo = new AxComboBox { ItemsSource = new[] { "net8.0", "net10.0" }, SelectedIndex = 0 };
        var window = Shown(combo);

        combo.IsDropDownOpen = true;
        window.UpdateLayout();

        var background = Part<Border>(combo, "PART_Background");

        Assert.Equal(Resource(window, "AxFocusRingColor"), Colour(background.BorderBrush));
        Assert.Equal(Resource(window, "AxPressedColor"), Colour(background.Background));

        window.Close();
    }

    private static void Press(Control control, Window window)
    {
        ((Avalonia.Controls.IPseudoClasses)control.Classes).Set(":pressed", true);

        window.UpdateLayout();
    }

    private static T Part<T>(Control control, string name)
        where T : Control =>
        control.GetVisualDescendants().OfType<T>().Single(child => child.Name == name);

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key)
    {
        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value), key);

        return (Color)value!;
    }

    private static Window Shown(Control control)
    {
        var window = new Window
        {
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = new StackPanel { Children = { control } },
        };

        window.Show();
        window.UpdateLayout();

        return window;
    }
}
