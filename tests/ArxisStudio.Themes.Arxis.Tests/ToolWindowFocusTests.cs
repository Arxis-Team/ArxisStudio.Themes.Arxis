using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Выбранная вкладка и активная панель: кто что говорит.
/// </summary>
/// <remarks>
/// Вопросов два, и отвечают на них порознь. Какая вкладка выбрана — полоса под ней, акцентная
/// всегда: панель без каретки показывает свой выбор сразу, а не ждёт, когда в неё придут. Где
/// сейчас клавиатура — подпись выбранной вкладки: основная в той панели, где каретка, вторичная во
/// всех остальных. Шапка не говорит ничего: прежде поднималась она целиком, и пятно во всю её
/// ширину переезжало между панелями на каждый щелчок.
/// <para>
/// Проверяется наблюдаемое: чем покрашены полоса и подпись и что шапка осталась прежней.
/// </para>
/// </remarks>
public class ToolWindowFocusTests
{
    /// <summary>
    /// Полоса под выбранной вкладкой акцентная сразу, без всякой каретки.
    /// </summary>
    /// <remarks>
    /// При запуске студии клавиатуры нет ни в одной панели, а вкладка в каждой уже выбрана. Полоса,
    /// зависевшая от фокуса, выходила в этот миг серой у всех — выбор не показывался вовсе, и это
    /// читалось поломкой, а не состоянием.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_selected_tab_wears_its_bar_from_the_start(string variant)
    {
        var (panel, _, tab, window) = Shown(variant);
        var marker = Marker(tab);

        Assert.True(marker.IsVisible, "выбранная вкладка осталась без полосы");
        Assert.Equal(Token(panel, "AxAccentBrush"), Paint(marker.Background));

        window.Close();
    }

    /// <summary>
    /// Одинокой вкладке полосы не полагается.
    /// </summary>
    /// <remarks>
    /// Выбирать не из чего, и линия под единственной вкладкой говорит о том, чего никто не
    /// спрашивает. В доке из одних одиночек ряд таких линий читался бы шумом.
    /// </remarks>
    [AvaloniaFact]
    public void A_lone_tab_has_no_bar_at_all()
    {
        var (_, inside, tab, window) = Shown(tabs: 1);

        Assert.False(Marker(tab).IsVisible, "у одинокой вкладки есть полоса");

        Assert.True(inside.Focus(), "контролу внутри панели не досталось фокуса");
        window.UpdateLayout();

        Assert.False(Marker(tab).IsVisible, "полоса появилась у одинокой вкладки под кареткой");

        window.Close();
    }

    /// <summary>Подпись выбранной вкладки называет панель, в которой клавиатура.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_label_of_the_selected_tab_names_the_panel_with_the_keyboard(string variant)
    {
        var (panel, inside, tab, window) = Shown(variant);

        Assert.Equal(Token(panel, "AxTextSecondaryBrush"), Paint(tab.Foreground));

        Assert.True(inside.Focus(), "контролу внутри панели не досталось фокуса");
        window.UpdateLayout();

        Assert.Equal(Token(panel, "AxTextPrimaryBrush"), Paint(tab.Foreground));

        window.Close();
    }

    /// <summary>
    /// Шапка не меняется от того, кому досталась клавиатура.
    /// </summary>
    /// <remarks>
    /// Прежде шапка поднималась на ступень поверхности, и щелчок по вкладке перекрашивал полосу во
    /// всю ширину панели. Признак переехал на вкладку, а шапка обязана молчать — и цветом, и своей
    /// линией.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_header_keeps_its_colour_whoever_holds_the_keyboard(string variant)
    {
        var (panel, inside, _, window) = Shown(variant);
        var header = Header(panel);
        var rule = Rule(panel);
        var calm = Paint(header.Background);
        var line = Paint(rule.Fill);

        Assert.True(inside.Focus());
        window.UpdateLayout();

        Assert.Equal(calm, Paint(header.Background));
        Assert.Equal(line, Paint(rule.Fill));
        Assert.Equal(Token(panel, "AxStrokeSubtleBrush"), Paint(rule.Fill));

        window.Close();
    }

    /// <summary>Подпись горит ровно у одной панели.</summary>
    /// <remarks>
    /// Состояние, которое видно всегда, не состояние: две панели, обе с основной подписью, отвечают
    /// на вопрос о клавиатуре так же плохо, как две одинаково спящие.
    /// </remarks>
    [AvaloniaFact]
    public void Only_one_panel_at_a_time_lights_its_label()
    {
        var (first, inside, tab, window) = Shown();
        var (second, beside, tabs) = Panel(2);

        ((StackPanel)window.Content!).Children.Add(second);
        window.UpdateLayout();

        var other = Chosen(tabs);
        var lit = Token(first, "AxTextPrimaryBrush");
        var calm = Token(first, "AxTextSecondaryBrush");

        Assert.True(inside.Focus());
        window.UpdateLayout();

        Assert.Equal(lit, Paint(tab.Foreground));
        Assert.Equal(calm, Paint(other.Foreground));

        Assert.True(beside.Focus(), "контролу второй панели не досталось фокуса");
        window.UpdateLayout();

        Assert.Equal(calm, Paint(tab.Foreground));
        Assert.Equal(lit, Paint(other.Foreground));

        window.Close();
    }

    /// <summary>
    /// У панели с заголовком вместо вкладок отвечает заголовок.
    /// </summary>
    /// <remarks>
    /// В докинге заголовок пуст — там работу делают вкладки, — но контрол умеет и заголовок, и
    /// тогда сказать о клавиатуре больше нечем. Способ тот же, что у вкладки: яркость подписи.
    /// </remarks>
    [AvaloniaFact]
    public void The_title_of_a_panel_without_tabs_answers_instead_of_the_bar()
    {
        var inside = new Border { Focusable = true, Height = 20 };
        var panel = new AxToolWindow
        {
            ShowHeader = true,
            ShowHeaderSeparator = true,
            Title = "Проект",
            Content = inside,
            Width = 240,
            Height = 120,
        };
        var window = new Window { Content = new StackPanel { Children = { panel } }, Width = 400, Height = 300 };

        window.Show();
        window.UpdateLayout();

        var title = panel.GetVisualDescendants().OfType<TextBlock>().Single(part => part.Name == "PART_Title");

        Assert.Equal(Token(panel, "AxTextSecondaryBrush"), Paint(title.Foreground));

        Assert.True(inside.Focus());
        window.UpdateLayout();

        Assert.Equal(Token(panel, "AxTextPrimaryBrush"), Paint(title.Foreground));

        window.Close();
    }

    /// <summary>Шапка панели — она в шаблоне одна.</summary>
    private static Border Header(AxToolWindow panel) =>
        panel.GetVisualDescendants().OfType<Border>().Single(border => border.Name == "PART_Header");

    /// <summary>Линия под шапкой: разделитель, а не нижняя рамка шапки.</summary>
    private static AxDivider Rule(AxToolWindow panel) =>
        panel.GetVisualDescendants().OfType<AxDivider>().Single(part => part.Name == "PART_HeaderRule");

    /// <summary>Полоса выбора под вкладкой.</summary>
    private static Border Marker(AxTabItem tab) =>
        tab.GetVisualDescendants().OfType<Border>().Single(part => part.Name == "PART_ActiveMarker");

    /// <summary>Выбранная вкладка полосы.</summary>
    private static AxTabItem Chosen(AxTabStrip strip) => (AxTabItem)strip.Items[strip.SelectedIndex]!;

    /// <summary>Цвет кисти; <c>null</c> — кисти нет вовсе.</summary>
    private static Color? Paint(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    /// <summary>
    /// Цвет токена темы — того самого, что стоит в словаре.
    /// </summary>
    /// <remarks>
    /// Вариант спрашивается явно: без него поиск не доходит до словарей темы,
    /// где токены и разложены по светлому и тёмному.
    /// </remarks>
    private static Color? Token(Control control, string key) =>
        control.TryFindResource(key, control.ActualThemeVariant, out var found) ? Paint(found as IBrush) : null;

    /// <summary>Панель с полосой вкладок и местом для каретки, показанная в окне.</summary>
    private static (AxToolWindow Panel, Border Inside, AxTabItem Tab, Window Window) Shown(
        string variant = "Dark", int tabs = 2)
    {
        var (panel, inside, strip) = Panel(tabs);
        var window = new Window
        {
            Content = new StackPanel { Children = { panel } },
            Width = 400,
            Height = 300,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
        };

        window.Show();
        window.UpdateLayout();

        return (panel, inside, Chosen(strip), window);
    }

    /// <summary>Панель докинга как она есть: шапка из вкладок, заголовка нет.</summary>
    private static (AxToolWindow Panel, Border Inside, AxTabStrip Tabs) Panel(int tabs)
    {
        var inside = new Border { Focusable = true, Height = 20 };
        var strip = new AxTabStrip();

        foreach (var name in new[] { "Проект", "Структура" }.Take(tabs))
            strip.Items.Add(new AxTabItem { Content = name });

        strip.SelectedIndex = 0;

        // Вкладки шапки — вкладки панели: вид им ставит тема полосы шапки.
        Assert.True(Application.Current!.TryFindResource("AxToolWindowTabStrip", out var theme));
        strip.Theme = (ControlTheme)theme!;

        var panel = new AxToolWindow
        {
            ShowHeader = true,
            ShowHeaderSeparator = true,
            Tabs = strip,
            Content = inside,
            Width = 240,
            Height = 120,
        };

        return (panel, inside, strip);
    }
}
