using ArxisStudio.Controls;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Controls.Documents;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Цвет каждого состояния контрола против компонентов дизайн-проекта.
/// </summary>
/// <remarks>
/// Компонент объявляет состояния явно — <c>style-hover</c>, <c>style-active</c>,
/// <c>style-focus</c> — и называет переменную, а не литерал: <c>var(--bg3)</c>.
/// Выгрузка сохраняет эту связь, поэтому сверка идёт до конца: имя переменной
/// разворачивается в цвет варианта, а тест читает, чем контрол покрашен на
/// самом деле, подняв нужный псевдокласс.
///
/// Так закрывается третья часть соответствия. Токены проверены отдельно,
/// размеры — тоже; здесь проверено, что нужный токен приходит в нужное
/// состояние нужной части шаблона.
/// </remarks>
public class ControlStateColorTests
{
    /// <summary>
    /// Места, где решение приёмки старше буквы компонента.
    /// </summary>
    private static readonly Dictionary<(string State, string Property), string> Decided = new()
    {
        // Флажок компонент красит той же залитой поверхностью, что и кнопку, —
        // но раздел 6 приёмки разводит их прямо: «AxAcc остаётся подчёркиваниям,
        // флажкам и иконкам — им довольно 3:1». Залитый акцент с текстом берёт
        // AxAccStrong, флажок с галочкой-штрихом — AxAcc.
        [("AxCheckBox/isChecked/base", "background")] = "acc",
        [("AxCheckBox/isChecked/base", "border-color")] = "acc",
        [("AxCheckBox/isIndeterminate/base", "background")] = "acc",
        [("AxCheckBox/isIndeterminate/base", "border-color")] = "acc",
    };

    public static TheoryData<string, string, string, string> Cases
    {
        get
        {
            var design = DesignProject.Load();
            var data = new TheoryData<string, string, string, string>();

            foreach (var (state, declared) in design.States.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                foreach (var (property, value) in declared.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                {
                    var named = Decided.GetValueOrDefault((state, property), value);
                    var literal = named.StartsWith('#');

                    // Рамка своих пикселей не даёт, когда её цвет совпал с
                    // заливкой или когда она прозрачна: в обоих случаях край
                    // показывает ту же заливку, потому что фон под рамку и
                    // заходит. Сверять там нечего — залитый флажок и акцентная
                    // кнопка выглядят одинаково и с рамкой, и без неё, а тема
                    // экономит на лишнем слое.
                    if (property == "border-color"
                        && declared.TryGetValue("background", out var fill)
                        && (named == "transparent"
                            || named == Decided.GetValueOrDefault((state, "background"), fill)))
                    {
                        continue;
                    }

                    // Компонент оставляет цвет параметром — {{ fg }}: значение
                    // задаёт тот, кто вставляет компонент, и сверять нечего.
                    if (named != "transparent" && !literal && !design.Variables.ContainsKey(named))
                        continue;

                    foreach (var variant in new[] { "Light", "Dark" })
                    {
                        var expected = named switch
                        {
                            "transparent" => "#00000000",
                            _ when literal => named,
                            _ => design.Variables[named][variant],
                        };

                        data.Add(state, property, variant, expected);
                    }
                }
            }

            return data;
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void State_colour_matches_the_design_project(
        string state,
        string property,
        string variant,
        string expected)
    {
        var parts = state.Split('/');
        var control = Create(parts[0], parts[1]);

        var window = new Window
        {
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = control,
        };

        window.Show();
        window.UpdateLayout();

        Raise(control, parts[0], parts[1], parts[2]);
        window.UpdateLayout();

        var actual = Read(control, parts[0], parts[2], property);

        Assert.Equal(expected.ToUpperInvariant(), Hex(actual));

        window.Close();
    }

    /// <summary>Создаёт контрол того вида, который объявляет компонент.</summary>
    private static Control Create(string component, string variant) => (component, variant) switch
    {
        ("AxButton", "isDefault") => new AxButton { Content = "Button" },
        ("AxButton", "isAccent") => new AxButton { Classes = { "accent" }, Content = "Button" },
        ("AxButton", "isGhost") => new AxButton { Classes = { "ghost" }, Content = "Button" },
        ("AxButton", "isIcon") => new AxButton { Classes = { "icon" } },
        ("AxButton", "isDanger") => new AxButton { Classes = { "danger" }, Content = "Button" },
        ("AxButton", "isDisabled") => new AxButton { IsEnabled = false, Content = "Button" },
        ("AxTextBox", "isNormal") => new AxTextBox(),
        ("AxTextBox", "isError") => new AxTextBox { Classes = { "error" } },
        ("AxTextBox", "isFocused") => new AxTextBox(),
        ("AxTextBox", "isDisabled") => new AxTextBox { IsEnabled = false },
        ("AxComboBox", "isNormal") => new AxComboBox(),
        ("AxComboBox", "isDisabled") => new AxComboBox { IsEnabled = false },
        ("AxCheckBox", "isChecked") => new AxCheckBox { IsChecked = true },
        ("AxCheckBox", "isIndeterminate") => new AxCheckBox { IsChecked = null },
        ("AxCheckBox", "isEmpty") => new AxCheckBox { IsChecked = false },
        ("AxCheckBox", "root") => new AxCheckBox(),
        ("AxToggleSwitch", "isOn") => new AxToggleSwitch { IsChecked = true },
        ("AxToggleSwitch", "isOff") => new AxToggleSwitch { IsChecked = false },
        ("AxToggleSwitch", "isOffDisabled") => new AxToggleSwitch { IsChecked = false, IsEnabled = false },
        ("AxToggleSwitch", "hasLabel") => new AxToggleSwitch { Content = "Тумблер" },
        ("AxToggleSwitch", "hasLabelDisabled") => new AxToggleSwitch { Content = "Тумблер", IsEnabled = false },
        ("AxToggleSwitch", "root") => new AxToggleSwitch(),
        _ => throw new ArgumentException($"нет разбора для {component}/{variant}"),
    };

    /// <summary>
    /// Поднимает псевдокласс состояния.
    /// </summary>
    /// <remarks>
    /// Поле показывает контур по <c>:focus</c> — оно принимает ввод с клавиатуры
    /// всегда; кнопка и список только по <c>:focus-visible</c>, иначе контур
    /// оставался бы висеть после щелчка мышью.
    /// </remarks>
    private static void Raise(Control control, string component, string variant, string state)
    {
        var pseudo = (IPseudoClasses)control.Classes;

        if (variant == "isFocused")
            pseudo.Set(":focus", true);

        // Флажок и тумблер объявляют контур на общей обёртке, без вида: там
        // фокус — единственное, что показывает эта строка выгрузки.
        if (variant == "root" && state == "focus")
            pseudo.Set(":focus-visible", true);

        switch (state)
        {
            case "hover":
                pseudo.Set(":pointerover", true);
                break;

            case "active":
                pseudo.Set(":pressed", true);
                break;

            case "focus":
                pseudo.Set(component == "AxTextBox" ? ":focus" : ":focus-visible", true);
                break;
        }
    }

    /// <summary>Читает цвет с той части шаблона, которая его и рисует.</summary>
    private static Color? Read(Control control, string component, string state, string property)
    {
        // Второй окрашенный слой компонента: бегунок тумблера и подсказка,
        // которую поле и список показывают, пока в них ничего не набрано.
        if (state == "inner")
            return Inner(control, component);

        if (property == "color")
            return (control.GetValue(TemplatedControl.ForegroundProperty) as ISolidColorBrush)?.Color;

        // Край фокуса в теме рисует отдельный слой поверх фона, а не рамка
        // контрола: так кнопка не дёргается, получая фокус, и контур виден даже
        // там, где рамки нет вовсе — у призрачной и иконочной кнопки.
        if (state == "focus" && property == "border-color")
        {
            var ring = Part(control, "PART_FocusRing");

            Assert.True(ring.IsVisible, "контур фокуса не показан");
            return (ring.GetValue(Border.BorderBrushProperty) as ISolidColorBrush)?.Color;
        }

        var (name, background, border) = component switch
        {
            "AxButton" => ("PART_ContentPresenter", ContentPresenter.BackgroundProperty, ContentPresenter.BorderBrushProperty),
            "AxTextBox" => ("PART_BorderElement", Border.BackgroundProperty, Border.BorderBrushProperty),
            "AxCheckBox" => ("PART_Box", Border.BackgroundProperty, Border.BorderBrushProperty),
            "AxToggleSwitch" => ("PART_Track", Border.BackgroundProperty, Border.BorderBrushProperty),
            _ => ("PART_Background", Border.BackgroundProperty, Border.BorderBrushProperty),
        };

        var part = Part(control, name);

        if (property == "background")
            return (part.GetValue(background) as ISolidColorBrush)?.Color;

        // Рамки нулевой толщины на экране нет, каким бы цветом её ни объявили:
        // призрачная кнопка именно так и получает прозрачный контур компонента.
        var thickness = part.GetValue(Border.BorderThicknessProperty);

        return thickness == default
            ? Colors.Transparent
            : (part.GetValue(border) as ISolidColorBrush)?.Color;
    }

    /// <summary>Читает цвет второго слоя — своего у каждого компонента.</summary>
    private static Color? Inner(Control control, string component) => component switch
    {
        "AxToggleSwitch" => (Part(control, "PART_Knob").GetValue(Shape.FillProperty) as ISolidColorBrush)?.Color,
        "AxTextBox" => (Part(control, "PART_Watermark")
            .GetValue(TextBlock.ForegroundProperty) as ISolidColorBrush)?.Color,
        _ => (control.GetValue(ComboBox.PlaceholderForegroundProperty) as ISolidColorBrush)?.Color,
    };

    private static Control Part(Control control, string name)
        => control.GetVisualDescendants().OfType<Control>().First(child => child.Name == name);

    private static string Hex(Color? color)
    {
        Assert.True(color.HasValue, "часть покрашена не сплошной кистью");
        var value = color!.Value;

        // Прозрачное записывают по-разному — компонент нулями, Avalonia белым
        // с нулевой альфой. На экране это одно и то же: ничего.
        if (value.A == 0x00)
            return "#00000000";

        return value.A == 0xFF
            ? $"#{value.R:X2}{value.G:X2}{value.B:X2}"
            : $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
    }
}
