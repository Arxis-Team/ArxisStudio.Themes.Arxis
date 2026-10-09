using System.Globalization;
using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Числовое поле и подпись, за которую его тянут.
/// </summary>
/// <remarks>
/// Поле правит текст разметки, а не число на экране: читает выражения и запятую, пишет инвариантно,
/// фиксирует одно значение — на Enter, уходе фокуса, шаге стрелкой и конце протяжки, — а протяжку
/// показывает, но не фиксирует, пока кнопку не отпустили. Проверяется это жестами, а не вызовами
/// методов: протяжку ведёт подпись, и путь от мыши до значения — то, что здесь и закрепляется.
/// </remarks>
public class NumberFieldTests
{
    [AvaloniaTheory]
    [InlineData("12.5", 12.5)]
    [InlineData("12,5", 12.5)]
    [InlineData("2*8+(10-4)/2", 19)]
    [InlineData("-(2+3)", -5)]
    [InlineData("1.5e2", 150)]
    [InlineData(" 7 ", 7)]
    public void Text_and_expressions_are_read(string text, double expected)
    {
        Assert.True(AxNumberField.TryEvaluate(text, out var value), $"«{text}» не прочлось");
        Assert.Equal(expected, value, 9);
    }

    [AvaloniaTheory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("2**3")]
    [InlineData("1/0")]
    [InlineData("(1+2")]
    [InlineData("1,5.2")]
    public void Nonsense_is_not_a_number(string text) =>
        Assert.False(AxNumberField.TryEvaluate(text, out _), $"«{text}» прочлось числом");

    /// <summary>Значение пишется с точкой при любом языке системы: поле правит разметку, а не экран.</summary>
    [AvaloniaFact]
    public void The_value_is_written_invariantly_under_any_culture()
    {
        var culture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");

            var (field, window) = Shown(value: 0);

            Type(window, field, "1234,5");
            Press(window, Key.Enter);

            Assert.Equal(1234.5, field.Value);
            Assert.Equal("1234.5", field.Text);
            Assert.Equal("0.25", AxNumberField.Format(0.25));

            window.Close();
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [AvaloniaFact]
    public void Enter_commits_once_and_shows_the_value()
    {
        var (field, window) = Shown(value: 3);
        var committed = Committed(field);

        Type(window, field, "2*8");
        Press(window, Key.Enter);

        Assert.Equal(16, field.Value);
        Assert.Equal("16", field.Text);
        Assert.Equal([16d], committed);

        window.Close();
    }

    [AvaloniaFact]
    public void Leaving_the_field_commits_what_was_typed()
    {
        var (field, window) = Shown(value: 3);
        var committed = Committed(field);

        Type(window, field, "8");
        Beside(window).Focus();

        Assert.Equal(8, field.Value);
        Assert.Equal([8d], committed);

        window.Close();
    }

    /// <summary>Непрочитанное остаётся в поле с отметкой ошибки, а Esc возвращает значение и снимает её.</summary>
    [AvaloniaFact]
    public void Unreadable_text_stays_marked_and_escape_brings_the_value_back()
    {
        var (field, window) = Shown(value: 3);
        var committed = Committed(field);

        Type(window, field, "abc");
        Press(window, Key.Enter);

        Assert.Equal(3, field.Value);
        Assert.Equal("abc", field.Text);
        Assert.True(field.Classes.Contains(":validation-error"), "непрочитанное не отмечено ошибкой");
        Assert.Empty(committed);

        Press(window, Key.Escape);

        Assert.Equal("3", field.Text);
        Assert.False(field.Classes.Contains(":validation-error"), "ошибка осталась после Esc");

        window.Close();
    }

    [AvaloniaFact]
    public void The_value_stays_within_its_bounds()
    {
        var (field, window) = Shown(value: 3, setup: field =>
        {
            field.Minimum = 0;
            field.Maximum = 10;
        });

        Type(window, field, "25");
        Press(window, Key.Enter);
        Assert.Equal(10, field.Value);

        Type(window, field, "-4");
        Press(window, Key.Enter);
        Assert.Equal(0, field.Value);

        window.Close();
    }

    /// <summary>«Авто» читается только у поля, которое его разрешает; у другого пустое — просьба снять значение.</summary>
    [AvaloniaFact]
    public void Auto_is_nan_where_allowed_and_an_empty_field_asks_to_clear_elsewhere()
    {
        var (auto, window) = Shown(value: 100, setup: field => field.AllowsAuto = true);

        Type(window, auto, "auto");
        Press(window, Key.Enter);

        Assert.True(double.IsNaN(auto.Value), $"«auto» прочлось как {auto.Value}");
        Assert.Equal("Auto", auto.Text);

        window.Close();

        var (plain, second) = Shown(value: 5);
        var cleared = 0;

        plain.Cleared += (_, _) => cleared++;
        plain.Focus();
        plain.Text = string.Empty;
        Press(second, Key.Enter);

        Assert.Equal(1, cleared);
        Assert.Equal("5", plain.Text);

        second.Close();
    }

    /// <summary>Стрелки — замена протяжки с клавиатуры: шаг, Shift крупнее, Alt мельче, и каждый фиксируется.</summary>
    [AvaloniaFact]
    public void Arrows_step_and_commit_with_shift_larger_and_alt_finer()
    {
        var (field, window) = Shown(value: 1);
        var committed = Committed(field);

        field.Focus();
        Press(window, Key.Up);
        Press(window, Key.Up, RawInputModifiers.Shift);
        Press(window, Key.Down, RawInputModifiers.Alt);

        Assert.Equal([2d, 12d, 11.9d], committed);
        Assert.Equal("11.9", field.Text);

        window.Close();
    }

    /// <summary>
    /// Набранное переживает обновление снаружи: хозяин может обновлять поле когда угодно, человек допишет
    /// своё. Без набранного новое значение видно сразу и при каретке в поле.
    /// </summary>
    [AvaloniaFact]
    public void A_draft_survives_an_update_from_outside()
    {
        var (field, window) = Shown(value: 3);

        Type(window, field, "4");
        field.Value = 7;

        Assert.Equal("4", field.Text);
        Assert.True(field.HasDraft, "набранное перестало быть черновиком");

        Press(window, Key.Escape);
        Assert.Equal("7", field.Text);

        field.Value = 9;
        Assert.Equal("9", field.Text);

        window.Close();
    }

    /// <summary>Разные значения — прочерк, а шаг идёт от значения первого из выбранных и кладёт одно на все.</summary>
    [AvaloniaFact]
    public void Mixed_shows_a_dash_and_steps_from_the_first_value()
    {
        var (field, window) = Shown(value: 5, setup: field => field.IsMixed = true);

        Assert.Equal(string.Empty, field.Text);

        var dash = field.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Name == "PART_Watermark");

        Assert.True(dash.IsEffectivelyVisible, "у разных значений не видно прочерка");
        Assert.Equal("—", dash.Text);

        field.Focus();
        Press(window, Key.Up);

        Assert.Equal(6, field.Value);
        Assert.False(field.IsMixed, "шаг не свёл значения в одно");

        window.Close();
    }

    /// <summary>Протяжка видна на каждом движении, а фиксируется одним значением при отпускании.</summary>
    [AvaloniaFact]
    public void A_scrub_shows_every_step_and_commits_once()
    {
        var (field, label, window) = Scrubbed(value: 0);
        var committed = Committed(field);
        var changing = new List<double>();

        field.ValueChanging += (_, e) => changing.Add(e.Value);

        var at = Grip(label, window);

        window.MouseDown(at, MouseButton.Left);
        window.MouseMove(at + new Vector(5, 0));
        window.MouseMove(at + new Vector(30, 0));
        window.MouseMove(at + new Vector(55, 0));

        Assert.True(changing.Count >= 2, $"протяжка показала {changing.Count} шагов");
        Assert.Empty(committed);

        window.MouseUp(at + new Vector(55, 0), MouseButton.Left);

        // Шаг на точку у нуля — 0,03 шага поля: пятьдесят точек дают полтора.
        Assert.Equal(1.5, field.Value, 9);
        Assert.Equal([1.5d], committed);

        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(RawInputModifiers.Shift, 6)]
    [InlineData(RawInputModifiers.Alt, 0.38)]
    public void Shift_scrubs_faster_and_alt_finer(RawInputModifiers modifiers, double expected)
    {
        var (field, label, window) = Scrubbed(value: 0);
        var at = Grip(label, window);

        window.MouseDown(at, MouseButton.Left);
        window.MouseMove(at + new Vector(5, 0));
        window.MouseMove(at + new Vector(55, 0), modifiers);
        window.MouseUp(at + new Vector(55, 0), MouseButton.Left, modifiers);

        Assert.Equal(expected, field.Value, 9);

        window.Close();
    }

    /// <summary>Крупное значение тянется быстрее мелкого: шаг на точку растёт корнем из значения.</summary>
    [AvaloniaFact]
    public void A_large_value_scrubs_faster_than_a_small_one()
    {
        var (field, label, window) = Scrubbed(value: 400);
        var at = Grip(label, window);

        window.MouseDown(at, MouseButton.Left);
        window.MouseMove(at + new Vector(5, 0));
        window.MouseMove(at + new Vector(15, 0));
        window.MouseUp(at + new Vector(15, 0), MouseButton.Left);

        // √400 · 0,03 · 10 точек = 6.
        Assert.Equal(406, field.Value, 9);

        window.Close();
    }

    /// <summary>Esc посреди протяжки возвращает прежнее — и «авто» остаётся «авто», а не нулём, от которого тянули.</summary>
    [AvaloniaFact]
    public void Escape_cancels_a_scrub_and_gives_back_what_was_there()
    {
        var (field, label, window) = Scrubbed(value: double.NaN, setup: field => field.AllowsAuto = true);
        var committed = Committed(field);
        var cancelled = 0;

        field.ScrubCancelled += (_, _) => cancelled++;

        var at = Grip(label, window);

        window.MouseDown(at, MouseButton.Left);
        window.MouseMove(at + new Vector(5, 0));
        window.MouseMove(at + new Vector(55, 0));

        Assert.False(double.IsNaN(field.Value), "протяжка не показала числа");

        Press(window, Key.Escape);

        // Мышь отпущена вместе с протяжкой, хотя кнопка ещё зажата: курсор над соседом — и сосед это видит.
        var beside = Beside(window);
        var over = beside.TranslatePoint(new Point(beside.Bounds.Width / 2, beside.Bounds.Height / 2), window)!.Value;

        window.MouseMove(over);
        Assert.True(beside.IsPointerOver, "после Esc подпись держит мышь, и сосед под курсором наведения не видит");

        window.MouseUp(over, MouseButton.Left);

        Assert.True(double.IsNaN(field.Value), $"после Esc осталось {field.Value}");
        Assert.Equal("Auto", field.Text);
        Assert.Equal(1, cancelled);
        Assert.Empty(committed);

        window.Close();
    }

    /// <summary>Нажатие без протяжки — щелчок: каретка в поле, набранное выделено, значение не тронуто.</summary>
    [AvaloniaFact]
    public void A_press_without_a_drag_puts_the_caret_into_the_field()
    {
        var (field, label, window) = Scrubbed(value: 42);
        var at = Grip(label, window);

        window.MouseDown(at, MouseButton.Left);
        window.MouseMove(at + new Vector(2, 0));
        window.MouseUp(at + new Vector(2, 0), MouseButton.Left);

        Assert.True(field.IsFocused, "щелчок по подписи не поставил каретку в поле");
        Assert.Equal(42, field.Value);
        Assert.Equal(0, field.SelectionStart);
        Assert.Equal(field.Text!.Length, field.SelectionEnd);

        window.Close();
    }

    /// <summary>Подпись называет поле диктору и показывает курсор протяжки, только пока поле можно тянуть.</summary>
    [AvaloniaFact]
    public void The_label_names_its_field_and_offers_the_grip_only_while_it_can_be_dragged()
    {
        var (field, label, window) = Scrubbed(value: 1);

        Assert.Same(label, Avalonia.Automation.AutomationProperties.GetLabeledBy(field));

        window.MouseMove(Grip(label, window));
        Assert.True(label.Classes.Contains(":scrubbable"), "подпись живого поля не предлагает протяжку");

        field.IsReadOnly = true;
        window.MouseMove(new Point(1, window.Bounds.Height - 1));
        window.MouseMove(Grip(label, window));
        Assert.False(label.Classes.Contains(":scrubbable"), "поле только для чтения предлагает протяжку");

        window.Close();
    }

    private static (AxNumberField Field, Window Window) Shown(double value, Action<AxNumberField>? setup = null)
    {
        var field = new AxNumberField { Value = value };

        setup?.Invoke(field);

        var window = new Window
        {
            Width = 400,
            Height = 200,
            Content = new StackPanel { Children = { field, new AxButton { Content = "beside" } } },
        };

        window.Show();
        window.UpdateLayout();

        return (field, window);
    }

    private static (AxNumberField Field, AxScrubLabel Label, Window Window) Scrubbed(
        double value, Action<AxNumberField>? setup = null)
    {
        var field = new AxNumberField { Value = value, Width = 200 };

        setup?.Invoke(field);

        var label = new AxScrubLabel { Content = "Width", Target = field, Width = 120 };
        var window = new Window
        {
            Width = 400,
            Height = 200,
            Content = new StackPanel
            {
                Children =
                {
                    new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { label, field } },
                    new AxButton { Content = "beside" },
                },
            },
        };

        window.Show();
        window.UpdateLayout();

        return (field, label, window);
    }

    private static Point Grip(AxScrubLabel label, Window window) =>
        label.TranslatePoint(new Point(10, label.Bounds.Height / 2), window)!.Value;

    private static Control Beside(Window window) =>
        window.GetVisualDescendants().OfType<AxButton>().First(button => Equals(button.Content, "beside"));

    private static List<double> Committed(AxNumberField field)
    {
        var committed = new List<double>();

        field.ValueCommitted += (_, e) => committed.Add(e.Value);

        return committed;
    }

    private static void Type(Window window, AxNumberField field, string text)
    {
        field.Focus();
        field.SelectAll();
        window.KeyTextInput(text);
    }

    private static void Press(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, string.Empty);
        window.UpdateLayout();
    }
}
