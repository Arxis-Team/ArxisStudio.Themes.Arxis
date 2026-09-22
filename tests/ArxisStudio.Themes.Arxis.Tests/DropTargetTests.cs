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
/// Строка списка — цель перетаскивания: подкраска и рамка поверх, раскладка на месте.
/// </summary>
/// <remarks>
/// Цель ставит хозяин списка (<see cref="AxListBoxItem.IsDropTarget"/>), а тема её рисует. Рамка
/// лежит поверх строки, как кольцо фокуса: на самой заливке она сдвинула бы содержимое на свою
/// толщину, и строка дёргалась бы под курсором, пока над ней несут файлы.
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

    private static Border Ring(Control row) =>
        row.GetVisualDescendants().OfType<Border>().First(part => part.Name == "PART_DropTarget");

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
