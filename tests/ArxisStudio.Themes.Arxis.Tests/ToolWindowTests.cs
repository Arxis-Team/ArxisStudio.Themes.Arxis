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
/// Панель инструментов: обводка, шапка и вкладки в ней.
/// </summary>
/// <remarks>
/// Карточка «Заголовок панели» показывает три вида шапки — только заголовок,
/// заголовок с линией, заголовок с вкладками — и все три на одной и той же
/// обведённой панели с подложкой AxBg2.
///
/// Радиус в карточке 6, а в шкале проекта его нет (3 / 4 / 8), поэтому взят
/// контрольный. Высота шапки 38 — единственное число карточки, которого нет
/// ни в одной шкале, но заголовок панели ни высотой строки, ни высотой
/// контрола не меряется, и своей шкалы у него в проекте не заведено.
/// </remarks>
public class ToolWindowTests
{
    /// <summary>Панель обведена, скруглена и стоит на подложке второго уровня.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Panel_is_a_bordered_surface(string variant)
    {
        var (panel, window) = Shown(variant: variant);

        Assert.Equal(new Thickness(1), panel.BorderThickness);
        Assert.Equal(new CornerRadius(4), panel.CornerRadius);
        Assert.Equal(Resource(window, "AxBg2Color", variant), Colour(panel.Background));
        Assert.Equal(Resource(window, "AxBrdColor", variant), Colour(panel.BorderBrush));

        // Обрезка: без неё линия под шапкой вылезает за скругление усиками.
        Assert.True(Root(panel).ClipToBounds, "панель не обрезает содержимое по скруглению");

        window.Close();
    }

    /// <summary>
    /// Шапка: 30 высотой и без собственных отступов — их носят заголовок и
    /// действия.
    /// </summary>
    /// <remarks>
    /// Отбивку карточки носит тот, кому она нужна. Стой она на самой шапке —
    /// вкладки не доходили бы до края и полоса вкладок начиналась бы правее
    /// панели под ней.
    /// </remarks>
    [AvaloniaFact]
    public void Header_is_thirty_high()
    {
        var (panel, window) = Shown();

        var header = (Border)Part(panel, "PART_Header");
        var title = (TextBlock)Part(panel, "PART_Title");
        var actions = Part(panel, "PART_Actions");

        Assert.Equal(30d, header.Bounds.Height);
        Assert.Equal(default, header.Padding);

        // Отбивку носят те двое, кому она нужна, — вкладки идут вровень с краем.
        Assert.Equal(new Thickness(12, 0), title.Margin);
        Assert.Equal(new Thickness(12, 0), actions.Margin);

        window.Close();
    }

    /// <summary>Линия под шапкой появляется по свойству, а не сама собой.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Separator_under_the_header_is_asked_for(string variant)
    {
        var (panel, window) = Shown(variant: variant);

        var header = (Border)Part(panel, "PART_Header");

        Assert.Equal(new Thickness(0), header.BorderThickness);

        panel.ShowHeaderSeparator = true;
        window.UpdateLayout();

        Assert.Equal(new Thickness(0, 0, 0, 1), header.BorderThickness);
        Assert.Equal(Resource(window, "AxBrdColor", variant), Colour(header.BorderBrush));

        window.Close();
    }

    /// <summary>Заголовок: основной текст усиленным начертанием, кегль базовый.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Title_is_the_main_text_in_semibold(string variant)
    {
        var (panel, window) = Shown(variant: variant);

        var title = (TextBlock)Part(panel, "PART_Title");

        Assert.Equal(Resource(window, "AxFgColor", variant), Colour(title.Foreground));
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
    /// ней; своя высота 34 оставила бы вкладку висеть посреди тридцати восьми.
    /// </remarks>
    [AvaloniaFact]
    public void Header_tab_fills_the_header()
    {
        var (panel, window) = Shown(tabs: true);

        var tabs = Tabs(panel).ToList();

        Assert.Equal(30d, tabs[0].Bounds.Height);
        Assert.Equal(tabs[0].Bounds.Right, tabs[1].Bounds.Left);

        window.Close();
    }

    /// <summary>
    /// Выбранную вкладку в шапке видно цветом, начертанием и полосой — но не
    /// фоном.
    /// </summary>
    /// <remarks>
    /// Фон остаётся вкладке редактора, где ряд имён стоит на общей подложке и
    /// выбранное имя поднимается над ней. Панельной карточка даёт три знака:
    /// цвет, начертание 500 и полосу в три пикселя — на один толще, чем у
    /// документа, потому что фона, который держал бы выбор, здесь нет.
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
        Assert.Equal(3d, marker.Bounds.Height);
        Assert.Equal(FontWeight.Medium, selected.FontWeight);
        Assert.Equal(Resource(window, "AxAccColor", variant), Colour(marker.Background));
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
            // Класс compact — это и есть вкладка в шапке: за ним начертание
            // выбранной, толщина полосы под ней и высота, которую задаёт шапка.
            var strip = new AxTabStrip();
            strip.Items.Add(new AxTabItem { Classes = { "compact" }, Content = "Text" });
            strip.Items.Add(new AxTabItem { Classes = { "compact" }, Content = "Text" });

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
