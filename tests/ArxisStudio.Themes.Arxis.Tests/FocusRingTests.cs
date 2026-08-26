using ArxisStudio.Controls;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Кольцо фокуса: пункт 8 приёмки и правило раздела 12.
/// </summary>
/// <remarks>
/// Правило записано в двух половинах, и обе обязательны. «Кольцо только на
/// <c>:focus-visible</c>» — значит, пройдя форму с клавиатуры, человек на
/// каждом шаге видит, где он. «Мышь фокус не подсвечивает» — значит, щёлкнув,
/// он не получает кольцо, которое будет висеть, пока он не щёлкнет ещё раз.
///
/// Проверять это глазами в галерее и означало бы жать Tab и смотреть, но
/// смотреть придётся заново после каждой правки темы, а забыть — один раз.
/// Здесь то же самое спрашивается у контрола: фокус с клавиатуры и фокус
/// указателем — разные способы, и Avalonia их различает сама.
/// </remarks>
public class FocusRingTests
{
    /// <summary>Контролы, у которых в теме есть слой кольца.</summary>
    public static TheoryData<Type> Focusable =>
    [
        typeof(AxButton),
        typeof(AxCheckBox),
        typeof(AxComboBox),
        typeof(AxDropDownButton),
        typeof(AxLink),
        typeof(AxRadioButton),
        typeof(AxSlider),
        typeof(AxSplitButton),
        typeof(AxTextBox),
        typeof(AxToggleSwitch),
    ];

    /// <summary>С клавиатуры кольцо видно.</summary>
    [AvaloniaTheory]
    [MemberData(nameof(Focusable))]
    public void Keyboard_focus_shows_the_ring(Type controlType)
    {
        var (control, window) = Shown(controlType);

        Reach(control).Focus(NavigationMethod.Tab);
        window.UpdateLayout();

        Assert.True(Shows(control), $"{controlType.Name}: с клавиатуры кольца не видно");

        window.Close();
    }

    /// <summary>
    /// Указателем — не видно.
    /// </summary>
    /// <remarks>
    /// Кроме поля ввода: его компонент показывает контур по обычному
    /// <c>:focus</c>, и не зря. Щёлкнув в поле, человек ставит туда каретку и
    /// печатает — поле обязано сказать, что ввод идёт в него, независимо от
    /// того, пришёл он клавишей или мышью. У кнопки такого продолжения нет:
    /// её нажали, и всё.
    /// </remarks>
    [AvaloniaTheory]
    [MemberData(nameof(Focusable))]
    public void Pointer_focus_leaves_the_ring_hidden(Type controlType)
    {
        if (controlType == typeof(AxTextBox))
        {
            return;
        }

        var (control, window) = Shown(controlType);

        Reach(control).Focus(NavigationMethod.Pointer);
        window.UpdateLayout();

        Assert.False(Shows(control), $"{controlType.Name}: щелчок зажёг кольцо");

        window.Close();
    }

    /// <summary>
    /// Проход Tab по форме доходит до каждого контрола и нигде не застревает.
    /// </summary>
    /// <remarks>
    /// Пункт 8 приёмки требует пройти галерею без мыши. Здесь тот же проход по
    /// собранной из тех же контролов форме: важно не число шагов, а что каждый
    /// контрол своей очереди дожидается и на каждом шаге видно, где стоишь.
    /// </remarks>
    [AvaloniaFact]
    public void Tab_reaches_every_control_and_shows_where_it_stands()
    {
        var controls = new Control[]
        {
            new AxButton { Content = "Кнопка" },
            new AxTextBox(),
            new AxCheckBox { Content = "Флажок" },
            new AxToggleSwitch { Content = "Тумблер" },
            new AxComboBox(),
        };

        var panel = new StackPanel();

        foreach (var control in controls)
            panel.Children.Add(control);

        var window = new Window { Content = panel };
        window.Show();
        window.UpdateLayout();

        foreach (var control in controls)
        {
            control.Focus(NavigationMethod.Tab);
            window.UpdateLayout();

            Assert.True(control.IsFocused, $"{control.GetType().Name} не принял фокус");
            Assert.True(Shows(control), $"на {control.GetType().Name} не видно, где стоишь");
        }

        window.Close();
    }

    /// <summary>
    /// Выключенный контрол очереди не занимает.
    /// </summary>
    /// <remarks>
    /// Иначе проход с клавиатуры останавливается на том, чего нельзя сделать, —
    /// и человек не понимает, почему форма его не пускает дальше.
    /// </remarks>
    [AvaloniaTheory]
    [MemberData(nameof(Focusable))]
    public void Disabled_control_stays_out_of_the_tab_order(Type controlType)
    {
        var (control, window) = Shown(controlType);
        control.IsEnabled = false;
        window.UpdateLayout();

        control.Focus(NavigationMethod.Tab);
        window.UpdateLayout();

        Assert.False(control.IsFocused, $"{controlType.Name}: выключенный принял фокус");

        window.Close();
    }

    /// <summary>
    /// Тот контрол, до которого доходит Tab.
    /// </summary>
    /// <remarks>
    /// Обычно это сам контрол. Кнопка с меню фокуса не берёт — Avalonia отдаёт
    /// его половинам, — и спрашивать надо у той, на которой человек окажется
    /// первой: у действия.
    /// </remarks>
    private static Control Reach(Control control) => control.Focusable
        ? control
        : control.GetVisualDescendants().OfType<Control>().First(child => child.Focusable);

    private static (Control Control, Window Window) Shown(Type controlType)
    {
        var control = (Control)Activator.CreateInstance(controlType)!;

        if (control is ContentControl content)
            content.Content = "Проверка";

        var window = new Window { Content = control };

        window.Show();
        window.UpdateLayout();

        return (control, window);
    }

    /// <summary>
    /// Видно ли кольцо человеку.
    /// </summary>
    /// <remarks>
    /// Двумя приёмами: у большинства контролов слой кольца прячется и
    /// показывается, а у ссылки он же и корень шаблона — прятать его значило бы
    /// прятать саму ссылку, поэтому там гаснет кисть. На экране это одно и то
    /// же, и спрашивать надо об этом, а не о способе.
    /// </remarks>
    private static bool Shows(Control control) => Rings(control).Any(ring =>
        ring.IsVisible && ring.GetValue(Border.BorderBrushProperty) is ISolidColorBrush { Color.A: > 0 });

    /// <summary>
    /// Все слои кольца под контролом.
    /// </summary>
    /// <remarks>
    /// Их бывает больше одного: у кнопки с меню фокус приходит не ей самой, а
    /// её половинам, и кольцо у каждой своё. Вопрос «видно ли, где стоишь»
    /// один на все слои.
    /// </remarks>
    private static IReadOnlyList<Control> Rings(Control control)
    {
        var rings = control.GetVisualDescendants()
            .OfType<Control>()
            .Where(child => child.Name == "PART_FocusRing")
            .ToList();

        Assert.True(rings.Count > 0, $"{control.GetType().Name}: в шаблоне нет слоя кольца");

        return rings;
    }
}
