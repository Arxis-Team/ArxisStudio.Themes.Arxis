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
        typeof(AxSearchField),
        typeof(AxSlider),
        typeof(AxSplitButton),
        typeof(AxTextArea),
        typeof(AxTextBox),
        typeof(AxToggleSwitch),
    ];

    /// <summary>
    /// Контрол с кольцом не обрезает себя по своим границам.
    /// </summary>
    /// <remarks>
    /// Кольцо вынесено наружу отрицательным полем, а TemplatedControl в
    /// Avalonia обрезает содержимое по границам — со включённой обрезкой от
    /// кольца оставались только угловые дуги, заходящие внутрь из-за
    /// скругления. У флажка это выглядело как четыре синих уголка вместо
    /// рамки, у кнопки терялось за перекрашенной рамкой самого контрола и
    /// потому не бросалось в глаза.
    ///
    /// Обрезка нужна не контролу целиком, а тем частям, которым она нужна, —
    /// там она и стоит: шапка панели, полоса вкладок, карточка диалога.
    /// </remarks>
    [AvaloniaTheory]
    [MemberData(nameof(Focusable))]
    public void Control_with_a_ring_does_not_clip_itself(Type controlType)
    {
        var (control, window) = Shown(controlType);

        Assert.False(control.ClipToBounds, $"{controlType.Name}: обрезка съест кольцо фокуса");

        window.Close();
    }

    /// <summary>
    /// Кольцо в фокусе выходит за своего хозяина — и остаётся видимым целиком.
    /// </summary>
    /// <remarks>
    /// Сравниваем с родителем кольца, а не с контролом: у сплит-кнопки фокус
    /// берут половины, и кольцо принадлежит половине — оно и должно быть шире
    /// её, а не всей кнопки.
    /// </remarks>
    [AvaloniaTheory]
    [MemberData(nameof(Focusable))]
    public void Ring_reaches_past_its_owner(Type controlType)
    {
        var (control, window) = Shown(controlType);

        Reach(control).Focus(NavigationMethod.Tab);
        window.UpdateLayout();

        var ring = Rings(control).First(r => r.IsVisible);
        var owner = (Avalonia.Visual)ring.GetVisualParent()!;

        Assert.True(
            ring.Bounds.Width > owner.Bounds.Width,
            $"{controlType.Name}: кольцо {ring.Bounds.Width} не шире хозяина {owner.Bounds.Width}");

        window.Close();
    }

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
        if (typeof(AxTextBox).IsAssignableFrom(controlType))
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
    /// Контур фокуса всегда даёт 2 пикселя акцента — но складывается по-разному.
    /// </summary>
    /// <remarks>
    /// У контрола со своей рамкой она на фокусе сама становится акцентной и
    /// считается первым пикселем контура, а снаружи ложится второй: так это
    /// объявлено в компонентах проекта — <c>border-color</c> плюс
    /// <c>box-shadow 0 0 0 1px</c>. У контрола без рамки — флажка, тумблера,
    /// ползунка — все два лежат снаружи.
    ///
    /// Кольцо в два поверх перекрашенной рамки давало три, и контрол в фокусе
    /// занимал на пиксель больше места с каждой стороны, чем задумано.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData(typeof(AxButton), 1d, 0d)]
    [InlineData(typeof(AxTextBox), 1d, 0d)]
    [InlineData(typeof(AxSearchField), 1d, 0d)]
    [InlineData(typeof(AxTextArea), 1d, 0d)]
    [InlineData(typeof(AxComboBox), 1d, 0d)]
    [InlineData(typeof(AxDropDownButton), 1d, 0d)]
    [InlineData(typeof(AxCheckBox), 2d, 1d)]
    [InlineData(typeof(AxRadioButton), 2d, 1d)]
    [InlineData(typeof(AxToggleSwitch), 2d, 1d)]
    [InlineData(typeof(AxSlider), 2d, 1d)]
    [InlineData(typeof(AxSplitButton), 2d, 1d)]
    [InlineData(typeof(AxLink), 2d, 1d)]
    public void Focus_outline_adds_up_to_two(Type controlType, double outside, double gap = 0d)
    {
        var (control, window) = Shown(controlType);

        Reach(control).Focus(NavigationMethod.Tab);
        window.UpdateLayout();

        var ring = Rings(control).First(r => r.IsVisible);
        var accent = Resource(window, "AxOutlineFocusedColor");

        // Кольцо снаружи: своей толщины и сдвинуто на неё же плюс просвет.
        // Просвет нужен там, где под кольцом залитая акцентом фигура — без
        // него кольцо сливается с ней в одно пятно.
        Assert.Equal(outside, ring.GetValue(Border.BorderThicknessProperty).Left);
        Assert.Equal(-(outside + gap), ring.Margin.Left);
        Assert.Equal(accent, Colour(ring.GetValue(Border.BorderBrushProperty)));

        // Недостающее до двух даёт своя рамка контрола — и она обязана быть
        // акцентной, иначе контур получится тоньше, а не сложится.
        var inside = 2d - outside;
        var edge = control.GetVisualDescendants()
            .OfType<Control>()
            .FirstOrDefault(child => child.Name is "PART_BorderElement" or "PART_ContentPresenter" or "PART_Background");

        if (inside > 0)
        {
            Assert.True(edge is not null, $"{controlType.Name}: нечему добрать контур до двух");
            Assert.Equal(inside, edge!.GetValue(Border.BorderThicknessProperty).Left);
            Assert.Equal(accent, Colour(edge.GetValue(Border.BorderBrushProperty)));
        }

        window.Close();
    }

    /// <summary>
    /// Кольцо обнимает содержимое, а не отведённую контролу высоту.
    /// </summary>
    /// <remarks>
    /// У флажка, переключателя и радиокнопки строка держит высоту контрола
    /// (24), а сама фигура — коробка, дорожка, кружок — вдвое ниже. Кольцо,
    /// растянутое по строке, висело вокруг пустоты: сверху и снизу от него до
    /// коробки оставался зазор в четыре пикселя, и на снимке это читалось как
    /// вытянутая рамка, а не как обводка флажка.
    ///
    /// Мера простая: кольцо выходит наружу на три пикселя с каждой стороны,
    /// и если оно всё же не выше самого контрола — значит, обнимает фигуру,
    /// а не строку.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData(typeof(AxCheckBox))]
    [InlineData(typeof(AxRadioButton))]
    [InlineData(typeof(AxToggleSwitch))]
    public void Ring_hugs_the_content_not_the_row(Type controlType)
    {
        var (control, window) = Shown(controlType);

        Reach(control).Focus(NavigationMethod.Tab);
        window.UpdateLayout();

        var ring = Rings(control).First(r => r.IsVisible);

        Assert.True(
            ring.Bounds.Height <= control.Bounds.Height,
            $"{controlType.Name}: кольцо {ring.Bounds.Height} растянуто по строке {control.Bounds.Height}");

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
    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key)
    {
        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value), key);

        return (Color)value!;
    }

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
