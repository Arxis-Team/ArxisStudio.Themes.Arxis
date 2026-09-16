using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Панель инструментов: обводка, шапка и вкладки в ней.
/// </summary>
/// <remarks>
/// Вида шапки три — только заголовок, заголовок с линией, заголовок с
/// вкладками — и все три на одной и той же обведённой панели с подложкой
/// AxSurfacePanel и контрольным радиусом.
/// </remarks>
public class ToolWindowTests
{
    /// <summary>
    /// Панель плоская: ни рамки, ни скругления, только своя подложка.
    /// </summary>
    /// <remarks>
    /// Доки — одна поверхность, разрезанная линиями, а не стопка карточек. Рамка панели легла бы
    /// поверх разделителя области — три линии там, где нужна одна, — а скруглённый угол на стыке
    /// открывал фон оболочки, и по границам выступали тёмные зазубрины.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Panel_is_a_flat_surface(string variant)
    {
        var (panel, window) = Shown(variant: variant);

        Assert.Equal(default, panel.BorderThickness);
        Assert.Equal(default, panel.CornerRadius);
        Assert.Equal(Resource(window, "AxSurfacePanelColor", variant), Colour(panel.Background));

        // BorderBrush остался цветом линии под шапкой — рисует её разделитель.
        Assert.Equal(Resource(window, "AxStrokeSubtleColor", variant), Colour(panel.BorderBrush));

        window.Close();
    }

    /// <summary>
    /// Шапка: 30 высотой, слева без отбивки, справа со своей.
    /// </summary>
    /// <remarks>
    /// Слева отбивку носит тот, кому она нужна, — заголовок: стой она на самой
    /// шапке, вкладки не доходили бы до края и полоса вкладок начиналась бы
    /// правее панели под ней. Справа отбивка у шапки: последним у края бывает
    /// кто угодно из иконочных кнопок, и помнить о крае каждому из них значило
    /// бы однажды забыть.
    /// </remarks>
    [AvaloniaFact]
    public void Header_is_the_height_of_a_tab()
    {
        var (panel, window) = Shown();

        var header = (Border)Part(panel, "PART_Header");
        var title = (TextBlock)Part(panel, "PART_Title");
        var actions = Part(panel, "PART_Actions");

        Assert.Equal(32d, header.Bounds.Height);
        Assert.Equal(new Thickness(0, 0, 12, 0), header.Padding);

        // Слева вкладки идут вровень с краем, и отбивку носит заголовок; у действий своя одна —
        // зазор от вкладок.
        Assert.Equal(new Thickness(12, 0), title.Margin);
        Assert.Equal(new Thickness(12, 0, 0, 0), actions.Margin);

        window.Close();
    }

    /// <summary>Правого края шапки не касается никто: ни действия, ни кнопки, приехавшие с вкладками.</summary>
    /// <remarks>
    /// Кнопка скрытия группы доков и кнопка переполнения полосы стоят не в действиях, а внутри
    /// вкладок, — и, дойди они до края, читались бы обрезанными, а подсветка под курсором
    /// упиралась бы в соседнюю панель. Проверяется то, что видно: расстояние от кнопки до края
    /// панели.
    /// </remarks>
    [AvaloniaFact]
    public void Nothing_in_the_header_touches_the_right_edge()
    {
        // Панель докинга: заголовка нет, действий нет, и у правого края стоит кнопка, приехавшая
        // с вкладками, — так стоит кнопка скрытия группы.
        var edge = new Button { Content = "×", VerticalAlignment = VerticalAlignment.Center };
        var docked = new AxToolWindow { Tabs = new DockPanel { Children = { edge } } };

        DockPanel.SetDock(edge, Dock.Right);

        var window = new Window { Content = docked, Width = 320, Height = 200 };

        window.Show();
        window.UpdateLayout();

        Assert.Equal(12d, docked.Bounds.Width - Right(edge, docked));

        // Панель студии: у края стоят её действия.
        var panel = new AxToolWindow { Title = "Панель", Actions = new Button { Content = "…" } };

        window.Content = panel;
        window.UpdateLayout();

        Assert.Equal(12d, panel.Bounds.Width - Right(Part(panel, "PART_Actions"), panel));

        window.Close();
    }

    private static double Right(Visual part, Visual of) =>
        part.TranslatePoint(new Point(part.Bounds.Width, 0), of)!.Value.X;

    /// <summary>Линия под шапкой появляется по свойству, а не сама собой.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Separator_under_the_header_is_asked_for(string variant)
    {
        var (panel, window) = Shown(variant: variant);

        var rule = (AxDivider)Part(panel, "PART_HeaderRule");

        Assert.False(rule.IsVisible, "линия под шапкой без спроса");

        panel.ShowHeaderSeparator = true;
        window.UpdateLayout();

        Assert.True(rule.IsVisible, "линия под шапкой не появилась");
        Assert.Equal(Resource(window, "AxStrokeSubtleColor", variant), Colour(rule.Fill));

        window.Close();
    }

    /// <summary>Линии нет там, где нет шапки: лечь ей не подо что.</summary>
    [AvaloniaFact]
    public void A_panel_without_a_header_has_no_line_under_it()
    {
        var (panel, window) = Shown();

        panel.ShowHeaderSeparator = true;
        panel.ShowHeader = false;
        window.UpdateLayout();

        Assert.False(((AxDivider)Part(panel, "PART_HeaderRule")).IsVisible, "линия под снятой шапкой");

        window.Close();
    }

    /// <summary>Заголовок: усиленное начертание, базовый кегль, а цвет говорит о клавиатуре.</summary>
    /// <remarks>
    /// Вторичный у спящей панели и основной у той, где клавиатура: панель с заголовком вместо
    /// вкладок отвечает на вопрос «куда пойдёт нажатие» тем же способом, что вкладка, — яркостью
    /// подписи. Здесь панель фокуса не держит, поэтому цвет вторичный; про обе стороны говорит
    /// <c>ToolWindowFocusTests</c>.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Title_is_the_main_text_in_semibold(string variant)
    {
        var (panel, window) = Shown(variant: variant);

        var title = (TextBlock)Part(panel, "PART_Title");

        Assert.Equal(Resource(window, "AxTextSecondaryColor", variant), Colour(title.Foreground));
        // Avalonia зовёт этот вес DemiBold — то же начертание, другое имя.
        Assert.Equal(FontWeight.SemiBold, title.FontWeight);
        Assert.Equal(13d, title.FontSize);

        window.Close();
    }

    /// <summary>
    /// Вкладка в шапке тянется на всю её высоту и стоит вплотную к соседней.
    /// </summary>
    /// <remarks>
    /// Полоса выбора обязана лечь на нижний край шапки, вплотную к линии под
    /// ней; своя высота оставила бы вкладку висеть посреди шапки.
    /// </remarks>
    [AvaloniaFact]
    public void Header_tab_fills_the_header()
    {
        var (panel, window) = Shown(tabs: true);

        var tabs = Tabs(panel).ToList();

        Assert.Equal(32d, tabs[0].Bounds.Height);
        Assert.Equal(tabs[0].Bounds.Right, tabs[1].Bounds.Left);

        window.Close();
    }

    /// <summary>
    /// Выбранную вкладку в шапке видно цветом и полосой — но не фоном и не
    /// начертанием.
    /// </summary>
    /// <remarks>
    /// Фон остаётся вкладке редактора, где ряд имён стоит на общей подложке и
    /// выбранное имя поднимается над ней. Веса же не даётся ни той, ни другой:
    /// жирное начертание меняет метрику текста, вкладка становится шире, и весь
    /// ряд сдвигается на каждое переключение.
    /// <para>
    /// Полоса акцентная и без каретки: она говорит о выборе, а не о фокусе. О том, где клавиатура,
    /// говорит подпись, и об этом — <c>ToolWindowFocusTests</c>.
    /// </para>
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Selected_header_tab_is_marked_by_the_bar_alone(string variant)
    {
        var (panel, window) = Shown(tabs: true, variant: variant);

        // Состояние ставим тем же псевдоклассом, которым его включает тема:
        // живая полоса выбора выбирает свою вкладку сама, а здесь выбирать
        // некому — окно в безголовом прогоне никем не щёлкнуто.
        var selected = Tabs(panel).First();
        ((IPseudoClasses)selected.Classes).Set(":selected", true);
        window.UpdateLayout();

        var marker = (Border)Part(selected, "PART_ActiveMarker");

        Assert.True(marker.IsVisible, "полосы выбора не видно");
        Assert.Equal(2d, marker.Bounds.Height);
        Assert.Equal(FontWeight.Normal, selected.FontWeight);
        Assert.Equal(Resource(window, "AxAccentColor", variant), Colour(marker.Background));
        // Прозрачная кисть, а не её отсутствие: своего фона у вкладки нет, но
        // сама кисть нужна — без неё вкладку не поймать курсором.
        Assert.Equal(0, Colour(((Border)Part(selected, "PART_Root")).Background)!.Value.A);

        window.Close();
    }

    private static (AxToolWindow Panel, Window Window) Shown(bool tabs = false, string variant = "Dark")
    {
        var panel = new AxToolWindow { Title = "Header", Height = 150, ShowHeaderSeparator = false };

        if (tabs)
        {
            // Вкладки шапки — вкладки панели: вид им ставит тема полосы шапки.
            var strip = new AxTabStrip();
            strip.Items.Add(new AxTabItem { Content = "Text" });
            strip.Items.Add(new AxTabItem { Content = "Text" });

            Assert.True(Application.Current!.TryFindResource("AxToolWindowTabStrip", out var theme));
            strip.Theme = (ControlTheme)theme!;

            panel.Tabs = strip;
        }

        var window = new Window
        {
            Width = 358,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = panel,
        };

        window.Show();
        window.UpdateLayout();

        return (panel, window);
    }

    private static IEnumerable<AxTabItem> Tabs(AxToolWindow panel) =>
        panel.GetVisualDescendants().OfType<AxTabItem>();

    /// <summary>Корень шаблона: он несёт обводку, скругление и обрезку.</summary>
    private static Border Root(AxToolWindow panel) =>
        panel.GetVisualDescendants().OfType<Border>().First();

    private static Control Part(Control control, string name)
    {
        var part = control.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Name == name);

        Assert.True(part is not null, $"в шаблоне нет части {name}");
        return part!;
    }

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key, string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        Assert.True(window.TryFindResource(key, theme, out var value), key);

        return (Color)value!;
    }
}
