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
/// Строка списка и сегмент крошек — цель перетаскивания: подкраска и рамка поверх, раскладка на месте.
/// </summary>
/// <remarks>
/// Цель ставит хозяин списка и крошек (<see cref="AxListBoxItem.IsDropTarget"/>,
/// <see cref="AxBreadcrumbItem.IsDropTarget"/>), а тема её рисует. Рамка лежит поверх, как кольцо
/// фокуса: на самой заливке она сдвинула бы содержимое на свою толщину, и строка дёргалась бы под
/// курсором, пока над ней несут файлы.
/// </remarks>
public class DropTargetTests
{
    /// <summary>Цель подкрашена и обведена, соседняя строка — нет, а содержимое не сдвинулось.</summary>
    [AvaloniaFact]
    public void A_list_row_marked_as_a_drop_target_wears_a_ring_and_keeps_its_layout()
    {
        var list = new AxListBox { ItemsSource = new[] { "Models", "Views" }, Width = 200, Height = 80 };
        var window = Shown(list, ThemeVariant.Dark);
        var rows = list.GetVisualDescendants().OfType<AxListBoxItem>().ToList();
        var label = rows[0].GetVisualDescendants().OfType<TextBlock>().Single();
        var before = label.TranslatePoint(default, rows[0]);

        rows[0].IsDropTarget = true;
        window.UpdateLayout();

        Assert.True(Ring(rows[0]).IsEffectivelyVisible, "у цели нет рамки");
        Assert.False(Ring(rows[1]).IsEffectivelyVisible, "рамка стоит и у соседней строки");
        Assert.Equal(Resource(window, "AxInfoFillColor"), Colour(Fill(rows[0]).Background));
        Assert.Equal(Resource(window, "AxAccentColor"), Colour(Ring(rows[0]).BorderBrush));
        Assert.Equal(before, label.TranslatePoint(default, rows[0]));

        rows[0].IsDropTarget = false;
        window.UpdateLayout();

        Assert.False(Ring(rows[0]).IsEffectivelyVisible, "рамка осталась, когда цель сняли");
        Assert.NotEqual(Resource(window, "AxInfoFillColor"), Colour(Fill(rows[0]).Background));

        window.Close();
    }

    /// <summary>
    /// Выбранная строка, над которой несут файлы, говорит «сюда ляжет», а не «выбрано».
    /// </summary>
    [AvaloniaFact]
    public void A_drop_target_is_stronger_than_the_selection()
    {
        var list = new AxListBox { ItemsSource = new[] { "Models" }, Width = 200, Height = 40 };
        var window = Shown(list, ThemeVariant.Dark);
        var row = list.GetVisualDescendants().OfType<AxListBoxItem>().Single();

        list.SelectedIndex = 0;
        row.Focus();
        row.IsDropTarget = true;
        window.UpdateLayout();

        Assert.Equal(Resource(window, "AxInfoFillColor"), Colour(Fill(row).Background));

        window.Close();
    }

    /// <summary>
    /// Рамка отличается и от подкраски под ней, и от панели за строкой не меньше чем на 3:1 — в обеих
    /// темах.
    /// </summary>
    /// <remarks>
    /// Цвета берутся у нарисованной рамки и нарисованной подкраски: правило держит экран, и роль,
    /// сменённая в шаблоне, прошла бы мимо сверки пар палитры.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void The_drop_ring_stands_out_by_three_to_one(string variant)
    {
        var list = new AxListBox { ItemsSource = new[] { "Models" }, Width = 200, Height = 40 };
        var window = Shown(list, variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        var row = list.GetVisualDescendants().OfType<AxListBoxItem>().Single();

        row.IsDropTarget = true;
        window.UpdateLayout();

        var ring = Colour(Ring(row).BorderBrush)!.Value;
        var fill = Colour(Fill(row).Background)!.Value;
        var panel = Resource(window, "AxSurfacePanelColor");

        Assert.True(Ratio(ring, fill) >= 3, $"{variant}: рамка к подкраске {Ratio(ring, fill):0.00}:1");
        Assert.True(Ratio(ring, panel) >= 3, $"{variant}: рамка к панели {Ratio(ring, panel):0.00}:1");

        window.Close();
    }

    /// <summary>
    /// Сегмент крошек — цель: подложка подкрашена и обведена, соседний сегмент — нет, подпись на
    /// месте; текущий сегмент, к наведению инертный, цель показывает тоже.
    /// </summary>
    /// <remarks>
    /// Уровень пути — такое же место, как строка каталога, и отпущенное на нём ложится туда; копия,
    /// отпущенная на текущем сегменте, — туда, где человек стоит.
    /// </remarks>
    [AvaloniaFact]
    public void A_crumb_marked_as_a_drop_target_wears_a_ring_and_keeps_its_layout()
    {
        var crumbs = new AxBreadcrumb { ItemsSource = new[] { "Hello", "src", "App" }, Width = 400 };
        var window = Shown(crumbs, ThemeVariant.Dark);
        var segments = crumbs.GetVisualDescendants().OfType<AxBreadcrumbItem>().ToList();
        var label = segments[1].GetVisualDescendants().OfType<TextBlock>().Single();
        var before = label.TranslatePoint(default, segments[1]);

        Assert.True(segments[2].IsCurrent, "последний сегмент не текущий — проверять нечего");
        Assert.Equal(Resource(window, "AxTextSecondaryColor"), Colour(Text(segments[1]).Foreground));

        segments[1].IsDropTarget = true;
        segments[2].IsDropTarget = true;
        window.UpdateLayout();

        // Цель читается как сегмент под курсором: подпись основная, а не вторичная, как у пути.
        Assert.Equal(Resource(window, "AxTextPrimaryColor"), Colour(Text(segments[1]).Foreground));
        Assert.True(Ring(segments[1]).IsEffectivelyVisible, "у цели нет рамки");
        Assert.False(Ring(segments[0]).IsEffectivelyVisible, "рамка стоит и у соседнего сегмента");
        Assert.True(Ring(segments[2]).IsEffectivelyVisible, "текущий сегмент цели не показывает");
        Assert.Equal(Resource(window, "AxInfoFillColor"), Colour(Plate(segments[1]).Background));
        Assert.Equal(Resource(window, "AxInfoFillColor"), Colour(Plate(segments[2]).Background));
        Assert.Equal(Resource(window, "AxAccentColor"), Colour(Ring(segments[1]).BorderBrush));
        Assert.Equal(before, label.TranslatePoint(default, segments[1]));

        segments[1].IsDropTarget = false;
        window.UpdateLayout();

        Assert.False(Ring(segments[1]).IsEffectivelyVisible, "рамка осталась, когда цель сняли");
        Assert.NotEqual(Resource(window, "AxInfoFillColor"), Colour(Plate(segments[1]).Background));

        window.Close();
    }

    /// <summary>
    /// У сегмента-цели рамка отличается от подкраски и от панели не меньше чем на 3:1, а подпись на
    /// подкраске читается на 4,5:1 — в обеих темах.
    /// </summary>
    /// <remarks>
    /// Подпись сегмента вторичная; на подкраске цели она становится основной, и проверяется цвет,
    /// которым её нарисовали.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void A_crumb_drop_target_reads_in_both_themes(string variant)
    {
        var crumbs = new AxBreadcrumb { ItemsSource = new[] { "Hello", "App" }, Width = 400 };
        var window = Shown(crumbs, variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        var segment = crumbs.GetVisualDescendants().OfType<AxBreadcrumbItem>().First();

        segment.IsDropTarget = true;
        window.UpdateLayout();

        var ring = Colour(Ring(segment).BorderBrush)!.Value;
        var fill = Colour(Plate(segment).Background)!.Value;
        var text = Colour(Text(segment).Foreground)!.Value;
        var panel = Resource(window, "AxSurfacePanelColor");

        Assert.True(Ratio(ring, fill) >= 3, $"{variant}: рамка к подкраске {Ratio(ring, fill):0.00}:1");
        Assert.True(Ratio(ring, panel) >= 3, $"{variant}: рамка к панели {Ratio(ring, panel):0.00}:1");
        Assert.True(Ratio(text, fill) >= 4.5, $"{variant}: подпись к подкраске {Ratio(text, fill):0.00}:1");

        window.Close();
    }

    /// <summary>
    /// Пункт меню — цель: подложка подкрашена и обведена, соседний пункт — нет, подпись на месте;
    /// цель сильнее выбора.
    /// </summary>
    /// <remarks>
    /// Так выглядит спрятанный уровень крошек в меню переполнения, когда тяга над ним: пункт —
    /// такое же место, как сам уровень.
    /// </remarks>
    [AvaloniaFact]
    public void A_menu_item_marked_as_a_drop_target_wears_a_ring_and_keeps_its_layout()
    {
        var items = new[] { new AxMenuItem { Header = "App" }, new AxMenuItem { Header = "Views" } };
        var panel = new StackPanel { Width = 220 };

        panel.Children.AddRange(items);

        var window = Shown(panel, ThemeVariant.Dark);
        var label = items[0].GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == "App");
        var before = label.TranslatePoint(default, items[0]);

        items[0].IsSelected = true;
        items[0].IsDropTarget = true;
        window.UpdateLayout();

        Assert.True(Ring(items[0]).IsEffectivelyVisible, "у цели нет рамки");
        Assert.False(Ring(items[1]).IsEffectivelyVisible, "рамка стоит и у соседнего пункта");
        Assert.Equal(Resource(window, "AxInfoFillColor"), Colour(Layout(items[0]).Background));
        Assert.Equal(Resource(window, "AxAccentColor"), Colour(Ring(items[0]).BorderBrush));
        Assert.Equal(before, label.TranslatePoint(default, items[0]));

        items[0].IsDropTarget = false;
        window.UpdateLayout();

        Assert.False(Ring(items[0]).IsEffectivelyVisible, "рамка осталась, когда цель сняли");
        Assert.NotEqual(Resource(window, "AxInfoFillColor"), Colour(Layout(items[0]).Background));

        window.Close();
    }

    /// <summary>
    /// У пункта-цели рамка отличается от подкраски и от полотна меню не меньше чем на 3:1, а подпись на
    /// подкраске читается на 4,5:1 — в обеих темах.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void A_menu_item_drop_target_reads_in_both_themes(string variant)
    {
        var item = new AxMenuItem { Header = "App" };
        var window = Shown(new StackPanel { Width = 220, Children = { item } }, variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        item.IsDropTarget = true;
        window.UpdateLayout();

        var ring = Colour(Ring(item).BorderBrush)!.Value;
        var fill = Colour(Layout(item).Background)!.Value;
        var text = Colour(item.Foreground)!.Value;
        var menu = Resource(window, "AxSurfaceOverlayColor");

        Assert.True(Ratio(ring, fill) >= 3, $"{variant}: рамка к подкраске {Ratio(ring, fill):0.00}:1");
        Assert.True(Ratio(ring, menu) >= 3, $"{variant}: рамка к полотну меню {Ratio(ring, menu):0.00}:1");
        Assert.True(Ratio(text, fill) >= 4.5, $"{variant}: подпись к подкраске {Ratio(text, fill):0.00}:1");

        window.Close();
    }

    private static Border Ring(Control row) =>
        row.GetVisualDescendants().OfType<Border>().First(part => part.Name == "PART_DropTarget");

    private static Border Layout(Control item) =>
        item.GetVisualDescendants().OfType<Border>().First(part => part.Name == "PART_LayoutRoot");

    private static Border Plate(Control segment) =>
        segment.GetVisualDescendants().OfType<Border>().First(part => part.Name == "PART_Plate");

    /// <summary>Подпись сегмента — её цветом и рисуется текст.</summary>
    private static ContentPresenter Text(Control segment) => Fill(segment);

    private static ContentPresenter Fill(Control row) =>
        row.GetVisualDescendants().OfType<ContentPresenter>().First(part => part.Name == "PART_ContentPresenter");

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key)
    {
        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value), key);

        return (Color)value!;
    }

    private static double Ratio(Color first, Color second)
    {
        var a = Luminance(first);
        var b = Luminance(second);

        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static double Luminance(Color color)
    {
        static double Channel(byte value)
        {
            var c = value / 255.0;

            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(color.R)) + (0.7152 * Channel(color.G)) + (0.0722 * Channel(color.B));
    }

    private static Window Shown(Control content, ThemeVariant variant)
    {
        var window = new Window
        {
            Width = 400,
            Height = 300,
            RequestedThemeVariant = variant,
            Content = content,
        };

        window.Show();
        window.UpdateLayout();

        return window;
    }
}
