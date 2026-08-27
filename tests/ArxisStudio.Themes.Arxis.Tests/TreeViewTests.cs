using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Дерево проекта: лестница уровней, значки и выделение строки.
/// </summary>
/// <remarks>
/// Карточка «Tree · проект ChatApp» размечает строки отбивками 8 → 26 → 44 от
/// левого края панели. Шаг 18 — это ровно шеврон 12 с зазором 6, и потому у
/// строки без детей колонки шеврона нет вовсе: значок файла встаёт на его
/// место. Пустая колонка увела бы все файлы на 18 вправо, и лестница разошлась
/// бы с макетом.
///
/// Высота строки — 24, а не 26 из карточки: раздел 6 приёмки сводит строку
/// списка и дерева к AxRowHeight.
/// </remarks>
public class TreeViewTests
{
    /// <summary>Строка дерева — AxRowHeight, как решено в разделе 6 приёмки.</summary>
    [AvaloniaFact]
    public void Row_is_the_row_height_of_the_scale()
    {
        var (tree, window) = Shown();

        foreach (var row in Rows(tree))
            Assert.Equal(24d, Part(row, "PART_Root").Bounds.Height);

        window.Close();
    }

    /// <summary>
    /// Лестница карточки: 8 у корня, 26 на первом уровне, 44 на втором.
    /// </summary>
    /// <remarks>
    /// Считаем от левого края панели, поэтому к каждому числу прибавлены 2 —
    /// отбивка, которой выделение вставлено от краёв.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData(0, 10d)]
    [InlineData(1, 28d)]
    [InlineData(2, 46d)]
    public void Level_steps_by_a_chevron_and_its_gap(int level, double left)
    {
        var (tree, window) = Shown();

        var row = Rows(tree).First(r => r.Level == level);
        var content = Part(row, "PART_Root").GetVisualDescendants().OfType<DockPanel>().First();

        Assert.Equal(left, Left(content, tree));

        window.Close();
    }

    /// <summary>
    /// У строки без детей колонки шеврона нет: значок занимает её место.
    /// </summary>
    [AvaloniaFact]
    public void Leaf_gives_the_chevron_column_to_its_icon()
    {
        var (tree, window) = Shown();

        // Лист и узел берём одного уровня: сравнивать колонки разных уровней
        // нечего — они и должны стоять на шаг друг от друга.
        var node = Rows(tree).First(r => r.ItemCount > 0 && r.Level > 0);
        var leaf = Rows(tree).First(r => r.ItemCount == 0 && r.Icon is not null && r.Level == node.Level);

        Assert.False(Part(leaf, "PART_ExpandCollapseChevron").IsVisible);
        Assert.Equal(
            Left(Part(node, "PART_ExpandCollapseChevron"), tree),
            Left(Part(leaf, "PART_Icon"), tree));

        window.Close();
    }

    /// <summary>Место под значок занято, только когда значок есть.</summary>
    [AvaloniaFact]
    public void Icon_slot_stands_aside_when_the_row_has_none()
    {
        var (tree, window) = Shown();

        Assert.True(Part(Rows(tree).First(r => r.Icon is not null), "PART_Icon").IsVisible);
        Assert.False(Part(Rows(tree).First(r => r.Icon is null), "PART_Icon").IsVisible);

        window.Close();
    }

    /// <summary>Шеврон: 12 на 12 в AxFg2, вниз у раскрытого узла.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Chevron_is_the_small_icon_of_the_set(string variant)
    {
        var (tree, window) = Shown(variant);

        var node = Rows(tree).First(r => r.ItemCount > 0);
        var chevron = (AxIcon)Part(node, "Chevron");

        Assert.Equal(12d, chevron.Bounds.Width);
        Assert.Equal(12d, chevron.Bounds.Height);
        Assert.Equal(Resource(window, "AxFg2Color", variant), Colour(chevron.Foreground));
        Assert.Same(AxIcons.ChevronDown, chevron.Data);

        node.IsExpanded = false;
        window.UpdateLayout();

        Assert.Same(AxIcons.ChevronRight, chevron.Data);

        window.Close();
    }

    /// <summary>
    /// Под курсором подсвечивается одна строка, а не вся ветка предков.
    /// </summary>
    /// <remarks>
    /// У элемента дерева :pointerover держится, пока курсор над любым его
    /// потомком, а дочерние строки лежат внутри него. Наведение поэтому
    /// спрашивается у самой строки: детей она не содержит.
    /// </remarks>
    [AvaloniaFact]
    public void Hover_lights_the_row_under_the_pointer_alone()
    {
        var (tree, window) = Shown();

        var leaf = Rows(tree).First(r => r.ItemCount == 0);
        var parent = Rows(tree).First(r => r.ItemCount > 0);

        // Так это делает Avalonia: наведение на потомка помечает и предков.
        foreach (var row in new[] { leaf, parent })
            ((IPseudoClasses)row.Classes).Set(":pointerover", true);

        ((IPseudoClasses)Part(leaf, "PART_Root").Classes).Set(":pointerover", true);
        window.UpdateLayout();

        Assert.Equal(
            Resource(window, "AxBg3Color", "Dark"),
            Colour(((Border)Part(leaf, "PART_Root")).Background));
        Assert.Equal(
            Colors.Transparent.ToUInt32(),
            Colour(((Border)Part(parent, "PART_Root")).Background)!.Value.ToUInt32());

        window.Close();
    }

    /// <summary>Выбранная строка: заливка AxSel с радиусом контрола.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Selected_row_takes_the_selection_token(string variant)
    {
        var (tree, window) = Shown(variant);

        var row = Rows(tree).First(r => r.ItemCount == 0);
        ((IPseudoClasses)row.Classes).Set(":selected", true);
        window.UpdateLayout();

        var fill = (Border)Part(row, "PART_Root");

        Assert.Equal(Resource(window, "AxSelColor", variant), Colour(fill.Background));
        Assert.Equal(new CornerRadius(4), fill.CornerRadius);

        window.Close();
    }

    /// <summary>Заливка вставлена от краёв панели на 2, как в карточке.</summary>
    [AvaloniaFact]
    public void Selection_is_inset_from_the_panel_edges()
    {
        var (tree, window) = Shown();

        Assert.Equal(2d, tree.Padding.Left);
        Assert.Equal(2d, tree.Padding.Right);

        var row = Rows(tree).First();
        Assert.Equal(tree.Bounds.Width - 4, Part(row, "PART_Root").Bounds.Width);

        window.Close();
    }

    private static (AxTreeView Tree, Window Window) Shown(string variant = "Dark")
    {
        var leaf = new AxTreeViewItem
        {
            Header = "ChatView.axaml",
            Icon = new Border { Width = 16, Height = 16 },
        };

        var views = new AxTreeViewItem { Header = "Views", IsExpanded = true, Icon = new AxIcon() };
        views.Items.Add(leaf);

        var plain = new AxTreeViewItem { Header = "ChatViewModel.cs" };
        var models = new AxTreeViewItem { Header = "ViewModels", IsExpanded = true, Icon = new AxIcon() };
        models.Items.Add(plain);

        var root = new AxTreeViewItem { Header = "ChatApp", IsExpanded = true, Icon = new AxIcon() };
        root.Items.Add(views);
        root.Items.Add(models);
        root.Items.Add(new AxTreeViewItem { Header = "App.axaml", Icon = new Border { Width = 16, Height = 16 } });

        var tree = new AxTreeView();
        tree.Items.Add(root);

        var window = new Window
        {
            Width = 320,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = tree,
        };

        window.Show();
        window.UpdateLayout();

        return (tree, window);
    }

    private static IEnumerable<AxTreeViewItem> Rows(AxTreeView tree) =>
        tree.GetVisualDescendants().OfType<AxTreeViewItem>();

    /// <summary>Левый край части в координатах панели, а не своей строки.</summary>
    private static double Left(Visual part, Visual tree) =>
        part.TranslatePoint(default, tree)!.Value.X;

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
