using System.Text.RegularExpressions;
using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Правила, которым обязана подчиняться тема любого интерактивного контрола.
/// </summary>
/// <remarks>
/// Состояние, которого нет, человек не отличит: строка, на которую нельзя нажать, выглядит
/// обычной; кнопка, не отвечающая на нажатие, оставляет промах неотличимым от попадания; контрол
/// без кольца фокуса теряет клавиатуру. Каждое из этих упущений в отдельном шаблоне заметить
/// трудно — тут их видно списком.
/// <para>
/// Спрашивается тема как она есть: стили, объявленные внутри неё и в теме, на которой она
/// основана. Это не замена проверкам на живом дереве, а сеть под ними — живьём проверяется, что
/// состояние выглядит правильно, здесь то, что оно вообще объявлено.
/// </para>
/// </remarks>
public class ControlThemeLintTests
{
    /// <summary>
    /// Состояния, обязательные для каждого интерактивного контрола.
    /// </summary>
    /// <remarks>
    /// Нажатие спрашивается там, где нажимают сам контрол: кнопка, флажок, переключатель, поле
    /// выбора. У строки списка и пункта меню его нет — строку выбирают, и выбор виден выделением,
    /// а не вдавленностью; у поля ввода тоже: в него ставят каретку.
    /// </remarks>
    public static TheoryData<Type, string[]> Interactive => new()
    {
        { typeof(AxButton), [":pointerover", ":pressed", ":disabled", ":focus-visible"] },
        { typeof(AxToggleButton), [":pointerover", ":pressed", ":checked", ":disabled", ":focus-visible"] },
        { typeof(AxDropDownButton), [":pointerover", ":pressed", ":disabled", ":focus-visible"] },
        { typeof(AxCheckBox), [":pointerover", ":pressed", ":checked", ":disabled", ":focus-visible"] },
        { typeof(AxRadioButton), [":pointerover", ":pressed", ":checked", ":disabled", ":focus-visible"] },
        { typeof(AxToggleSwitch), [":pointerover", ":pressed", ":checked", ":disabled", ":focus-visible"] },
        { typeof(AxComboBox), [":pointerover", ":pressed", ":disabled", ":focus-visible"] },
        { typeof(AxTextBox), [":pointerover", ":focus", ":disabled"] },
        { typeof(AxLink), [":pointerover", ":pressed", ":disabled", ":focus-visible"] },
        { typeof(AxListBoxItem), [":pointerover", ":selected", ":selection-active", ":disabled", ":focus-visible"] },
        { typeof(AxComboBoxItem), [":pointerover", ":selected", ":disabled", ":focus-visible"] },
        { typeof(AxTreeViewItem), [":pointerover", ":selected", ":selection-active", ":disabled", ":focus-visible"] },
        { typeof(AxTabItem), [":pointerover", ":selected", ":selection-active", ":disabled", ":focus-visible"] },
        { typeof(AxSegmentItem), [":pointerover", ":selected", ":disabled", ":focus-visible"] },
        { typeof(AxMenuItem), [":pointerover", ":selected", ":disabled"] },
        { typeof(AxSplitter), [":pointerover", ":pressed", ":disabled", ":focus-visible"] },
        { typeof(AxGroupHeader), [":pointerover", ":disabled", ":focus-visible"] },
    };

    /// <summary>
    /// Половины кнопки с раздельным действием: свои темы, и состояния у них свои.
    /// </summary>
    /// <remarks>
    /// Нажимают не саму кнопку, а её половину — действие или стрелку, — и состояния носят они.
    /// Поэтому в списке выше её нет, а здесь есть обе половины: и та, что делает, и та, что
    /// открывает меню.
    /// </remarks>
    public static TheoryData<string, string[]> Halves => new()
    {
        { "AxSplitButtonPrimary", [":pointerover", ":pressed", ":focus-visible"] },
        { "AxSplitButtonSecondary", [":pointerover", ":pressed", ":focus-visible"] },
        { "AxWindowButton", [":pointerover", ":pressed", ":focus-visible"] },
    };

    /// <summary>Контролы, у которых кольцо фокуса — часть шаблона.</summary>
    /// <remarks>
    /// Поле ввода показывает фокус рамкой по <c>:focus</c>, а не по <c>:focus-visible</c>: щёлкнув
    /// в поле, человек ставит туда каретку, и поле обязано сказать, что ввод идёт в него.
    /// </remarks>
    public static TheoryData<Type, string> Ringed => new()
    {
        { typeof(AxButton), ":focus-visible" },
        { typeof(AxToggleButton), ":focus-visible" },
        { typeof(AxDropDownButton), ":focus-visible" },
        { typeof(AxCheckBox), ":focus-visible" },
        { typeof(AxRadioButton), ":focus-visible" },
        { typeof(AxToggleSwitch), ":focus-visible" },
        { typeof(AxComboBox), ":focus-visible" },
        { typeof(AxComboBoxItem), ":focus-visible" },
        { typeof(AxListBoxItem), ":focus-visible" },
        { typeof(AxTreeViewItem), ":focus-visible" },
        { typeof(AxTabItem), ":focus-visible" },
        { typeof(AxSegmentItem), ":focus-visible" },
        { typeof(AxLink), ":focus-visible" },
        { typeof(AxSlider), ":focus-visible" },
        { typeof(AxGroupHeader), ":focus-visible" },
        { typeof(AxTextBox), ":focus" },
    };

    /// <summary>У интерактивного контрола объявлено каждое его состояние.</summary>
    [AvaloniaTheory]
    [MemberData(nameof(Interactive))]
    public void Interactive_theme_declares_every_state(Type control, string[] states)
    {
        var selectors = Selectors(control);

        foreach (var state in states)
        {
            Assert.True(
                selectors.Any(selector => selector.Contains(state, StringComparison.Ordinal)),
                $"{control.Name}: в теме нет состояния {state} — на экране его не отличить");
        }
    }

    /// <summary>У половины кнопки с раздельным действием объявлено каждое её состояние.</summary>
    [AvaloniaTheory]
    [MemberData(nameof(Halves))]
    public void A_half_of_the_split_button_declares_every_state(string key, string[] states)
    {
        var selectors = Selectors(key);

        foreach (var state in states)
        {
            Assert.True(
                selectors.Any(selector => selector.Contains(state, StringComparison.Ordinal)),
                $"{key}: в теме нет состояния {state} — на экране его не отличить");
        }
    }

    /// <summary>Фокус показывает кольцо, и показывает его та причина, ради которой оно заведено.</summary>
    [AvaloniaTheory]
    [MemberData(nameof(Ringed))]
    public void Focus_is_shown_by_the_ring(Type control, string state)
    {
        var selectors = Selectors(control);

        Assert.True(
            selectors.Any(selector =>
                selector.Contains(state, StringComparison.Ordinal) &&
                selector.Contains("PART_FocusRing", StringComparison.Ordinal)),
            $"{control.Name}: кольцо фокуса не показывается по {state}");
    }

    /// <summary>
    /// Рука под курсором — только у ссылки.
    /// </summary>
    /// <remarks>
    /// Десктопная конвенция: рука значит «здесь ссылка». Над кнопкой, вкладкой и строкой курсор
    /// остаётся стрелкой — так их показывают Visual Studio, Rider и сам проводник.
    /// </remarks>
    [Fact]
    public void The_hand_cursor_belongs_to_links_alone()
    {
        foreach (var (name, text) in ThemeSources.All())
        {
            if (name is "AxLink.axaml")
                continue;

            Assert.False(
                text.Contains("Value=\"Hand\"", StringComparison.Ordinal),
                $"{name}: рука под курсором стоит не у ссылки");
        }
    }

    /// <summary>
    /// То, что появляется под курсором, появляется и с клавиатуры.
    /// </summary>
    /// <remarks>
    /// Крестик вкладки, кнопка строки, стрелка раскрытия: показывай их только наведение — с
    /// клавиатуры их нет вовсе, и человек, идущий по Tab, не видит действия, до которого дошёл.
    /// Поэтому у каждого правила с <c>:pointerover</c>, которое показывает или прячет часть,
    /// обязан быть близнец — фокус или выбор той же части.
    /// </remarks>
    [Fact]
    public void A_hover_affordance_has_a_keyboard_twin()
    {
        var styles = new Regex("<Style Selector=\"([^\"]*)\">(.*?)</Style>", RegexOptions.Singleline);

        foreach (var (name, text) in ThemeSources.All())
        {
            var matches = styles.Matches(text);

            var shown = matches
                .Where(match => match.Groups[1].Value.Contains(":pointerover", StringComparison.Ordinal))
                .Where(match => match.Groups[2].Value.Contains("Property=\"IsVisible\" Value=\"True\"", StringComparison.Ordinal))
                .Select(match => Part(match.Groups[1].Value))
                .OfType<string>()
                .ToList();

            foreach (var part in shown)
            {
                // Сама подсветка наведения — не действие: у клавиатуры для того же есть кольцо.
                if (part.Contains("Hover", StringComparison.Ordinal))
                    continue;

                var twin = matches.Any(match =>
                    Part(match.Groups[1].Value) == part &&
                    match.Groups[2].Value.Contains("Property=\"IsVisible\" Value=\"True\"", StringComparison.Ordinal) &&
                    (match.Groups[1].Value.Contains(":focus", StringComparison.Ordinal) ||
                     match.Groups[1].Value.Contains(":selected", StringComparison.Ordinal)));

                Assert.True(twin, $"{name}: {part} показывается наведением и ничем больше — с клавиатуры его нет");
            }
        }
    }

    /// <summary>
    /// Глобальных правил «для всякого контрола» в теме нет.
    /// </summary>
    /// <remarks>
    /// Такое правило оценивается на каждом элементе дерева, а нужно оно немногим: задержку
    /// подсказки носят контролы с подписью и текст. Список типов стоит ровно столько, сколько в
    /// нём типов.
    /// </remarks>
    [Fact]
    public void The_theme_has_no_rule_for_every_control() =>
        Assert.False(
            ThemeSources.Text("ArxisTheme.axaml").Contains("Selector=\":is(Control)\"", StringComparison.Ordinal),
            "в общих стилях темы стоит правило для всякого контрола");

    /// <summary>
    /// Состояние выделения не держится на предке в селекторе.
    /// </summary>
    /// <remarks>
    /// <c>:focus-within</c> у предка не различает вложенные списки, не достаёт до списка в попапе и
    /// гаснет, стоит открыть меню: у окна попапа нет визуального предка. Полную силу выделению
    /// даёт область (<c>:selection-active</c>), и она объявляет себя сама.
    /// </remarks>
    [Fact]
    public void Selection_does_not_lean_on_an_ancestor_selector()
    {
        var ancestors = new Regex("Selector=\"[^\"]*:focus-within");

        foreach (var (name, text) in ThemeSources.All())
        {
            Assert.False(
                ancestors.IsMatch(text),
                $"{name}: состояние взято у предка через :focus-within");
        }
    }

    /// <summary>
    /// Движутся только два индикатора хода, и переходов нет вовсе.
    /// </summary>
    /// <remarks>
    /// Смена состояния мгновенная: переход на цвете или размере превращает нажатие в ожидание, а
    /// перекладку — в работу на каждом кадре. Движение остаётся там, где оно и есть сообщение:
    /// спиннер и неопределённая полоса хода говорят «идёт», и сказать это иначе нечем.
    /// </remarks>
    [Fact]
    public void Only_the_two_indicators_move()
    {
        foreach (var (name, text) in ThemeSources.All())
        {
            Assert.False(
                text.Contains("<Transitions>", StringComparison.Ordinal) ||
                text.Contains("Transitions=", StringComparison.Ordinal),
                $"{name}: в теме появился переход — смена состояния обязана быть мгновенной");

            if (name is "AxPrimitives.axaml" or "AxToolbox.axaml")
                continue;

            Assert.False(
                text.Contains("Style.Animations", StringComparison.Ordinal),
                $"{name}: движение заведено не у полосы хода и не у спиннера");
        }
    }

    /// <summary>Имя части из селектора: то, что стоит после решётки.</summary>
    private static string? Part(string selector)
    {
        var hash = selector.LastIndexOf('#');

        return hash < 0 ? null : selector[(hash + 1)..].Trim();
    }

    /// <summary>Селекторы темы контрола вместе с теми, что пришли из темы-основы.</summary>
    private static List<string> Selectors(Type control) => Selectors((object)control);

    /// <summary>Селекторы темы по ключу: тип или имя.</summary>
    private static List<string> Selectors(object key)
    {
        Assert.True(Application.Current!.TryFindResource(key, null, out var found), $"в теме нет {key}");

        var selectors = new List<string>();

        for (var theme = Assert.IsType<ControlTheme>(found); theme is not null; theme = theme.BasedOn as ControlTheme)
            selectors.AddRange(Flatten(theme).Select(style => style.Selector?.ToString() ?? string.Empty));

        return selectors;
    }

    private static IEnumerable<Style> Flatten(StyleBase style) =>
        style.Children.OfType<Style>().SelectMany(child => Flatten(child).Prepend(child));
}
