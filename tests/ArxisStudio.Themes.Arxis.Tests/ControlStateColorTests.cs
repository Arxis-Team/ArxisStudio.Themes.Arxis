using ArxisStudio.Controls;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
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
    /// Единственное расхождение с буквой компонента, оставленное намеренно.
    /// </summary>
    /// <remarks>
    /// У акцентной кнопки компонент объявляет рамку прозрачной поверх заливки
    /// <c>accS</c>; тема красит её тем же <c>accS</c>. На экране это один и тот
    /// же пиксель — прозрачная рамка показывает фон под собой, — но своим
    /// цветом рамка переезжает вместе с фоном на наведении и нажатии, и
    /// светлая полоса на стыке не появляется.
    /// </remarks>
    private static readonly Dictionary<(string State, string Property), string> Decided = new()
    {
        [("AxButton/isAccent/base", "border-color")] = "accS",
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

                    // Компонент оставляет цвет параметром — {{ fg }}: значение
                    // задаёт тот, кто вставляет компонент, и сверять нечего.
                    if (named != "transparent" && !design.Variables.ContainsKey(named))
                        continue;

                    foreach (var variant in new[] { "Light", "Dark" })
                    {
                        var expected = named == "transparent"
                            ? "#00000000"
                            : design.Variables[named][variant];

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
