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
/// Активная панель: та, в которой сейчас клавиатура.
/// </summary>
/// <remarks>
/// У среды с десятком панелей вопрос «куда пойдёт нажатие» должен иметь ответ до нажатия. Отвечает
/// на него выбранная вкладка: полоса под ней акцентна, а подпись — основным цветом, пока
/// клавиатура в этой панели, и нейтральна, когда клавиатура ушла. Шапка не меняется вовсе — прежде
/// поднималась она целиком, и пятно во всю её ширину переезжало между панелями на каждый щелчок по
/// вкладке.
/// <para>
/// Проверяется наблюдаемое: чем покрашена полоса выбранной вкладки, чем покрашена её подпись и что
/// шапка осталась прежней.
/// </para>
/// </remarks>
public class ToolWindowFocusTests
{
    /// <summary>Полоса выбранной вкладки становится акцентной, когда в панель приходит каретка.</summary>
    [AvaloniaFact]
    public void The_bar_of_the_selected_tab_turns_accent_with_the_keyboard()
    {
        var (panel, inside, tab, window) = Shown();

        Assert.Equal(Token(panel, "AxStrokeControlBrush"), Paint(Marker(tab).Background));

        Assert.True(inside.Focus(), "контролу внутри панели не досталось фокуса");
        window.UpdateLayout();

        Assert.Equal(Token(panel, "AxAccentBrush"), Paint(Marker(tab).Background));
        Assert.Equal(Token(panel, "AxTextPrimaryBrush"), Paint(tab.Foreground));

        window.Close();
    }

    /// <summary>
    /// Панель без каретки всё равно называет свою вкладку.
    /// </summary>
    /// <remarks>
    /// Погасить полосу совсем значило бы забрать ответ на другой вопрос — какая вкладка выбрана, —
    /// а он нужен и в спящей панели: на соседку смотрят, чтобы решить, куда идти.
    /// </remarks>
    [AvaloniaFact]
    public void A_panel_without_the_keyboard_keeps_a_visible_bar()
    {
        var (panel, inside, tab, window) = Shown();
        var away = new Border { Focusable = true, Height = 20 };

        ((StackPanel)window.Content!).Children.Add(away);
        window.UpdateLayout();

        Assert.True(inside.Focus());
        window.UpdateLayout();

        Assert.True(away.Focus());
        window.UpdateLayout();

        var marker = Marker(tab);

        Assert.True(marker.IsVisible, "выбранная вкладка спящей панели осталась без полосы");
        Assert.Equal(Token(panel, "AxStrokeControlBrush"), Paint(marker.Background));
        Assert.Equal(Token(panel, "AxTextSecondaryBrush"), Paint(tab.Foreground));

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

    /// <summary>Акцентная полоса в окне ровно одна.</summary>
    /// <remarks>
    /// Состояние, которое видно всегда, не состояние: две панели, обе с акцентом, отвечают на
    /// вопрос о клавиатуре так же плохо, как две одинаково спящие.
    /// </remarks>
    [AvaloniaFact]
    public void Only_one_panel_at_a_time_shows_the_accent_bar()
    {
        var (first, inside, tab, window) = Shown();
        var (second, beside, tabs) = Panel();

        ((StackPanel)window.Content!).Children.Add(second);
        window.UpdateLayout();

        var other = Chosen(tabs);
        var accent = Token(first, "AxAccentBrush");
        var idle = Token(first, "AxStrokeControlBrush");

        Assert.True(inside.Focus());
        window.UpdateLayout();

        Assert.Equal(accent, Paint(Marker(tab).Background));
        Assert.Equal(idle, Paint(Marker(other).Background));

        Assert.True(beside.Focus(), "контролу второй панели не досталось фокуса");
        window.UpdateLayout();

        Assert.Equal(idle, Paint(Marker(tab).Background));
        Assert.Equal(accent, Paint(Marker(other).Background));

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
    private static (AxToolWindow Panel, Border Inside, AxTabItem Tab, Window Window) Shown(string variant = "Dark")
    {
        var (panel, inside, tabs) = Panel();
        var window = new Window
        {
            Content = new StackPanel { Children = { panel } },
            Width = 400,
            Height = 300,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
        };

        window.Show();
        window.UpdateLayout();

        return (panel, inside, Chosen(tabs), window);
    }

    /// <summary>Панель докинга как она есть: шапка из вкладок, заголовка нет.</summary>
    private static (AxToolWindow Panel, Border Inside, AxTabStrip Tabs) Panel()
    {
        var inside = new Border { Focusable = true, Height = 20 };
        var tabs = new AxTabStrip();

        tabs.Items.Add(new AxTabItem { Content = "Проект" });
        tabs.Items.Add(new AxTabItem { Content = "Структура" });
        tabs.SelectedIndex = 0;

        // Вкладки шапки — вкладки панели: вид им ставит тема полосы шапки.
        Assert.True(Application.Current!.TryFindResource("AxToolWindowTabStrip", out var theme));
        tabs.Theme = (ControlTheme)theme!;

        var panel = new AxToolWindow
        {
            ShowHeader = true,
            ShowHeaderSeparator = true,
            Tabs = tabs,
            Content = inside,
            Width = 240,
            Height = 120,
        };

        return (panel, inside, tabs);
    }
}
