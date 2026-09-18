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
/// Выбранная строка помечена у переднего края: вторичным цветом, пока фокус вне её области, и
/// акцентом, пока в ней.
/// </summary>
/// <remarks>
/// Заливка выделения отличается от панели на 1,3–1,6:1, а полная и погашенная — друг от друга лишь
/// оттенком, на 1,01–1,05:1. Выбор держался на одном цвете, и для обводки и графики этого мало:
/// обозначение состояния по WCAG 1.4.11 — 3:1. Метка даёт его, не трогая заливку, как полоса
/// выбранной строки в списках Windows 11.
/// </remarks>
public class SelectionMarkerTests
{
    /// <summary>Строка списка: метка у выбранной, вторичная без фокуса и акцентная с ним.</summary>
    [AvaloniaFact]
    public void A_selected_list_row_is_marked_at_its_leading_edge()
    {
        var list = new AxListBox { ItemsSource = new[] { "MainWindow.axaml", "ChatView.axaml" }, Width = 200, Height = 80 };
        var away = new AxTextBox();
        var window = Shown(new StackPanel { Children = { list, away } }, ThemeVariant.Dark);

        list.SelectedIndex = 0;
        window.UpdateLayout();

        var rows = list.GetVisualDescendants().OfType<AxListBoxItem>().ToList();
        var marker = Marker(rows[0]);

        Assert.True(marker.IsEffectivelyVisible, "у выбранной строки нет метки");
        Assert.False(Marker(rows[1]).IsEffectivelyVisible, "метка стоит и у невыбранной строки");
        Assert.Equal(0, marker.TranslatePoint(default, rows[0])!.Value.X);
        Assert.Equal(Resource(window, "AxTextSecondaryColor"), Colour(marker.Background));

        rows[0].Focus();
        window.UpdateLayout();

        Assert.Equal(Resource(window, "AxAccentColor"), Colour(marker.Background));

        away.Focus();
        window.UpdateLayout();

        Assert.Equal(Resource(window, "AxTextSecondaryColor"), Colour(marker.Background));

        window.Close();
    }

    /// <summary>Строка дерева помечена той же меткой, что строка списка.</summary>
    [AvaloniaFact]
    public void A_selected_tree_row_is_marked_like_a_list_row()
    {
        var node = new AxTreeViewItem { Header = "App" };
        var other = new AxTreeViewItem { Header = "Lib" };
        var tree = new AxTreeView { ItemsSource = new[] { node, other }, Width = 200, Height = 80 };
        var window = Shown(tree, ThemeVariant.Dark);

        tree.SelectedItem = node;
        node.Focus();
        window.UpdateLayout();

        var marker = Marker(node);

        Assert.True(marker.IsEffectivelyVisible, "у выбранной строки дерева нет метки");
        Assert.False(Marker(other).IsEffectivelyVisible, "метка стоит и у невыбранной строки дерева");
        Assert.Equal(Resource(window, "AxAccentColor"), Colour(marker.Background));

        window.Close();
    }

    /// <summary>
    /// Метка отличается и от заливки своей строки, и от панели за ней не меньше чем на 3:1 — в обеих
    /// темах, погашенная и полная.
    /// </summary>
    /// <remarks>
    /// Цвета берутся у нарисованной метки и нарисованной заливки, а не из палитры: правило держит то,
    /// что на экране, и сменённая в шаблоне роль прошла бы мимо проверки пар палитры.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void The_row_marker_stands_out_by_three_to_one(string variant)
    {
        var list = new AxListBox { ItemsSource = new[] { "MainWindow.axaml" }, Width = 200, Height = 40 };
        var away = new AxTextBox();
        var window = Shown(new StackPanel { Children = { list, away } }, variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        list.SelectedIndex = 0;
        window.UpdateLayout();

        var row = list.GetVisualDescendants().OfType<AxListBoxItem>().Single();
        var panel = Resource(window, "AxSurfacePanelColor");

        foreach (var focused in new[] { false, true })
        {
            if (focused)
                row.Focus();
            else
                away.Focus();

            window.UpdateLayout();

            var marker = Colour(Marker(row).Background)!.Value;
            var fill = Colour(row.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>()
                .First(part => part.Name == "PART_ContentPresenter").Background)!.Value;

            Assert.True(Ratio(marker, fill) >= 3, $"{variant}, фокус {focused}: метка к заливке {Ratio(marker, fill):0.00}:1");
            Assert.True(Ratio(marker, panel) >= 3, $"{variant}, фокус {focused}: метка к панели {Ratio(marker, panel):0.00}:1");
        }

        window.Close();
    }

    private static Border Marker(Control row) =>
        row.GetVisualDescendants().OfType<Border>().First(part => part.Name == "PART_SelectionMarker");

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
