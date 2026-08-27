using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Хлебные крошки: звено, разделитель и рамочная полоса дерева контролов.
/// </summary>
/// <remarks>
/// Карточка «Breadcrumbs» показывает четыре ряда: путь со значками, путь с
/// усечением, дерево контролов в рамке и путь с ошибкой и погашенными
/// звеньями. Звено везде одно и то же — 6 на 2 с радиусом контрола, — а
/// различают ряды состояние и класс.
/// </remarks>
public class BreadcrumbTests
{
    /// <summary>Звено: отбивка 6 на 2, радиус контрола.</summary>
    [AvaloniaFact]
    public void Link_is_a_plate_of_six_by_two()
    {
        var (bar, window) = Shown();

        var link = Links(bar).First();

        Assert.Equal(new Thickness(6, 2), link.Padding);
        Assert.Equal(new CornerRadius(4), link.CornerRadius);

        window.Close();
    }

    /// <summary>
    /// Разделитель: мелкий шеврон набора в AxFg2, у первого звена его нет.
    /// </summary>
    /// <remarks>
    /// В карточке он того же серого, что и глиф папки в звене, — а не бледнее.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Separator_is_the_small_chevron_in_the_icon_colour(string variant)
    {
        var (bar, window) = Shown(variant);

        var links = Links(bar).ToList();

        Assert.False(Separator(links[0]).IsVisible);

        // Не первое попавшееся звено с шевроном, а живое: у погашенного
        // разделитель гаснет вместе с ним, и цвет там другой по делу.
        var chevron = Separator(links.First(l => l.IsEnabled && Separator(l).IsVisible));

        Assert.True(chevron.IsVisible);
        Assert.Equal(12d, chevron.Bounds.Width);
        Assert.Equal(Resource(window, "AxFg2Color", variant), Colour(chevron.Foreground));
        Assert.Same(AxIcons.ChevronRight, chevron.Data);

        window.Close();
    }

    /// <summary>Текущее звено: основной текст усиленным начертанием.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Current_link_ends_the_path(string variant)
    {
        var (bar, window) = Shown(variant);

        var current = Links(bar).First(l => l.Classes.Contains("current"));

        Assert.Equal(Resource(window, "AxFgColor", variant), Colour(current.Foreground));
        Assert.Equal("Medium", current.FontWeight.ToString());

        window.Close();
    }

    /// <summary>Звено с ошибкой: красный текст на плашке ошибки.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Broken_link_sits_on_the_error_plate(string variant)
    {
        var (bar, window) = Shown(variant);

        var broken = Links(bar).First(l => l.Classes.Contains("error"));

        Assert.Equal(Resource(window, "AxRedTextColor", variant), Colour(broken.Foreground));
        Assert.Equal("Medium", broken.FontWeight.ToString());
        Assert.Equal(
            Resource(window, "AxErrorBackgroundColor", variant),
            Colour(Plate(broken).Background));

        window.Close();
    }

    /// <summary>Наведение поднимает под звеном плашку AxBg3.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Hover_raises_a_plate_under_the_link(string variant)
    {
        var (bar, window) = Shown(variant);

        var link = Links(bar).First();
        ((IPseudoClasses)link.Classes).Set(":pointerover", true);
        window.UpdateLayout();

        Assert.Equal(Resource(window, "AxBg3Color", variant), Colour(Plate(link).Background));

        window.Close();
    }

    /// <summary>
    /// Выключенное звено гаснет вместе со своим разделителем.
    /// </summary>
    /// <remarks>
    /// Свой цвет у разделителя задан прямо в шаблоне и от Foreground звена не
    /// зависит; в карточке погашенный путь погашен целиком, вместе с шевронами.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Disabled_link_takes_its_separator_with_it(string variant)
    {
        var (bar, window) = Shown(variant);

        var dead = Links(bar).First(l => !l.IsEnabled);
        var expected = Resource(window, "AxFgDisabledColor", variant);

        Assert.Equal(expected, Colour(dead.Foreground));
        Assert.Equal(expected, Colour(Separator(dead).Foreground));

        window.Close();
    }

    /// <summary>Место под значок занято, только когда значок есть.</summary>
    [AvaloniaFact]
    public void Icon_slot_stands_aside_when_the_link_has_none()
    {
        var (bar, window) = Shown();

        Assert.True(Part(Links(bar).First(l => l.Icon is not null), "PART_Icon").IsVisible);
        Assert.False(Part(Links(bar).First(l => l.Icon is null), "PART_Icon").IsVisible);

        window.Close();
    }

    /// <summary>
    /// Рамочная полоса: дерево контролов в дизайнере.
    /// </summary>
    /// <remarks>
    /// Карточка даёт ей высоту 32 и радиус 6 — ни того, ни другого в шкале
    /// проекта нет, поэтому взяты AxControlHeight и AxCornerRadius. Отбивка 4
    /// вместе с шестёркой звена даёт те же 10 до первой подписи, что в карточке.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Framed_bar_is_a_sunken_strip(string variant)
    {
        var (bar, window) = Shown(variant);

        bar.Classes.Add("framed");
        window.UpdateLayout();

        Assert.Equal(28d, bar.Bounds.Height);
        Assert.Equal(new Thickness(1), bar.BorderThickness);
        Assert.Equal(new CornerRadius(4), bar.CornerRadius);
        Assert.Equal(new Thickness(4, 0), bar.Padding);
        Assert.Equal(Resource(window, "AxBgSunkenColor", variant), Colour(bar.Background));
        Assert.Equal(Resource(window, "AxBrdColor", variant), Colour(bar.BorderBrush));

        // 1 рамки, 4 полосы и 6 звена — содержимое первого звена на 11 от края.
        var first = Part(Links(bar).First(), "PART_Icon");

        Assert.Equal(11d, first.TranslatePoint(default, bar)!.Value.X);

        window.Close();
    }

    private static (AxBreadcrumbBar Bar, Window Window) Shown(string variant = "Dark")
    {
        var bar = new AxBreadcrumbBar();

        bar.Items.Add(new AxBreadcrumbItem { Content = "ВолнаЧат", Icon = new AxIcon() });
        bar.Items.Add(new AxBreadcrumbItem { Content = "Views", IsEnabled = false });
        bar.Items.Add(new AxBreadcrumbItem { Content = "LoginView.axaml", Classes = { "error" } });
        bar.Items.Add(new AxBreadcrumbItem { Content = "ChatView.axaml", Classes = { "current" } });

        var window = new Window
        {
            Width = 520,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = bar,
        };

        window.Show();
        window.UpdateLayout();

        return (bar, window);
    }

    private static IEnumerable<AxBreadcrumbItem> Links(AxBreadcrumbBar bar) =>
        bar.GetVisualDescendants().OfType<AxBreadcrumbItem>();

    private static AxIcon Separator(AxBreadcrumbItem link) => (AxIcon)Part(link, "PART_Separator");

    /// <summary>Плашка звена: она несёт фон наведения и фон ошибки.</summary>
    private static Border Plate(AxBreadcrumbItem link) => (Border)Part(link, "PART_LayoutRoot");

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
