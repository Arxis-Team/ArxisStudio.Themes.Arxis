using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Геометрия контролов против компонентов дизайн-проекта.
/// </summary>
/// <remarks>
/// Пять контролов вынесены в проекте отдельными компонентами — <c>AxButton</c>,
/// <c>AxTextBox</c>, <c>AxComboBox</c>, <c>AxCheckBox</c>,
/// <c>AxToggleSwitch.dc.html</c>, — и это самый точный источник размеров:
/// экраны студии подключают их же. Числа ниже сняты оттуда, а не с картинки.
/// Тест читает фактические значения у показанного контрола, поэтому ловит и
/// правку темы, и правку класса, которая размеры перебивает.
/// </remarks>
public class ControlGeometryTests
{
    /// <summary>Кнопка: 28 высотой, минимум 72 в ширину, отступы 12, радиус 4.</summary>
    [AvaloniaFact]
    public void Button_matches_the_component()
    {
        var button = Shown(new AxButton { Content = "Button" });

        Assert.Equal(28d, button.Height);
        Assert.Equal(72d, button.MinWidth);
        // Вертикальный отступ у кнопки нулевой: компонент центрирует текст
        // флексом при высоте 28, а не отступами. Ненулевой съедал высоту, и
        // на 28 текст обрезался снизу.
        Assert.Equal(new Thickness(12, 0), button.Padding);
        Assert.Equal(new CornerRadius(4), button.CornerRadius);
        Assert.Equal(new Thickness(1), button.BorderThickness);
    }

    /// <summary>Призрачная кнопка: без минимальной ширины, отступы 10.</summary>
    [AvaloniaFact]
    public void Ghost_button_matches_the_component()
    {
        var button = Shown(new AxButton { Classes = { "ghost" }, Content = "Button" });

        Assert.Equal(28d, button.Height);
        Assert.Equal(0d, button.MinWidth);
        Assert.Equal(new Thickness(10, 0), button.Padding);
        Assert.Equal(new Thickness(0), button.BorderThickness);
    }

    /// <summary>Иконочная кнопка: 24 × 24 без отступов.</summary>
    [AvaloniaFact]
    public void Icon_button_matches_the_component()
    {
        var button = Shown(new AxButton { Classes = { "icon" } });

        Assert.Equal(24d, button.Width);
        Assert.Equal(24d, button.Height);
        Assert.Equal(0d, button.MinWidth);
        Assert.Equal(new Thickness(0), button.Padding);
        Assert.Equal(new CornerRadius(4), button.CornerRadius);
    }

    /// <summary>Компактная кнопка: 24 высотой — плотная зона раздела 6.</summary>
    [AvaloniaFact]
    public void Compact_button_matches_the_specification()
    {
        var button = Shown(new AxButton { Classes = { "compact" }, Content = "Slim" });

        Assert.Equal(24d, button.Height);
    }

    /// <summary>Поле ввода: 28 высотой, отступы 9, радиус 4.</summary>
    [AvaloniaFact]
    public void Text_box_matches_the_component()
    {
        var box = Shown(new AxTextBox());

        Assert.Equal(28d, box.MinHeight);
        Assert.Equal(new Thickness(9, 0), box.Padding);
        Assert.Equal(new CornerRadius(4), box.CornerRadius);
        Assert.Equal(new Thickness(1), box.BorderThickness);
    }

    /// <summary>Поле в плотной зоне: 24 — то же решение раздела 6.</summary>
    [AvaloniaFact]
    public void Compact_text_box_matches_the_specification()
    {
        var box = Shown(new AxTextBox { Classes = { "compact" } });

        Assert.Equal(24d, box.MinHeight);
    }

    /// <summary>Комбобокс: 28 высотой, отступы 9 слева и 6 справа, радиус 4.</summary>
    [AvaloniaFact]
    public void Combo_box_matches_the_component()
    {
        var combo = Shown(new AxComboBox());

        Assert.Equal(28d, combo.MinHeight);
        Assert.Equal(new Thickness(9, 0, 6, 0), combo.Padding);
        Assert.Equal(new CornerRadius(4), combo.CornerRadius);
    }

    /// <summary>Компактный комбобокс: 24.</summary>
    [AvaloniaFact]
    public void Compact_combo_box_matches_the_specification()
    {
        var combo = Shown(new AxComboBox { Classes = { "compact" } });

        Assert.Equal(24d, combo.MinHeight);
    }

    /// <summary>
    /// Флажок: коробка 16 × 16 с радиусом 3, область нажатия не ниже 24.
    /// </summary>
    [AvaloniaFact]
    public void Check_box_matches_the_component()
    {
        var check = Shown(new AxCheckBox { Content = "Проверка" });
        var box = Part<Border>(check, "PART_Box");

        Assert.Equal(24d, check.MinHeight);
        Assert.Equal(16d, box.Width);
        Assert.Equal(16d, box.Height);
        Assert.Equal(new CornerRadius(3), box.CornerRadius);
    }

    /// <summary>
    /// Тумблер: дорожка 30 × 17, бегунок 13, область нажатия не ниже 24.
    /// </summary>
    [AvaloniaFact]
    public void Toggle_switch_matches_the_component()
    {
        var toggle = Shown(new AxToggleSwitch { Content = "Проверка" });
        var track = Part<Border>(toggle, "PART_Track");
        var knob = Part<Avalonia.Controls.Shapes.Ellipse>(toggle, "PART_Knob");

        Assert.Equal(24d, toggle.MinHeight);
        Assert.Equal(30d, track.Width);
        Assert.Equal(17d, track.Height);
        Assert.Equal(13d, knob.Width);
        Assert.Equal(13d, knob.Height);
    }

    /// <summary>
    /// Радиокнопка повторяет размер и зазор флажка: рядом в одной колонке они
    /// обязаны стоять на одной линии.
    /// </summary>
    [AvaloniaFact]
    public void Radio_button_repeats_the_check_box_metrics()
    {
        var radio = Shown(new AxRadioButton { Content = "Проверка" });
        var circle = Part<Avalonia.Controls.Shapes.Ellipse>(radio, "PART_Circle");

        Assert.Equal(24d, radio.MinHeight);
        Assert.Equal(16d, circle.Bounds.Width);
        Assert.Equal(16d, circle.Bounds.Height);
    }

    /// <summary>Строка списка и дерева: 24 — решение раздела 6 против 26 макета.</summary>
    [AvaloniaFact]
    public void Row_height_is_the_decided_twenty_four()
    {
        var item = Shown(new AxListBoxItem { Content = "Строка" });

        Assert.Equal(24d, item.MinHeight);
    }

    private static T Shown<T>(T control)
        where T : Control
    {
        var window = new Window { Content = control };

        window.Show();
        window.UpdateLayout();

        return control;
    }

    private static TPart Part<TPart>(Control control, string name)
        where TPart : Control
    {
        var part = control.GetVisualDescendants()
            .OfType<TPart>()
            .FirstOrDefault(child => child.Name == name);

        Assert.True(part is not null, $"в шаблоне нет части {name}");
        return part!;
    }
}
