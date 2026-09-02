using ArxisStudio.Controls;
using ArxisStudio.Icons;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Размеры темы — живые токены, а не снимок значений.
/// </summary>
/// <remarks>
/// Токен, за которым никто не следит, отличим от числа только на вид: оба
/// показывают одно и то же, пока значение не меняли. Поэтому здесь его меняют —
/// и смотрят, пошёл ли за ним интерфейс. Это же ловит и обратную ошибку: место,
/// где числом задана не длина, а система координат, и меняться оно как раз не
/// должно.
/// </remarks>
public class SizeTokenTests
{
    /// <summary>Иконка идёт за своим токеном, а её клетка координат — нет.</summary>
    /// <remarks>
    /// Клетка 16×16 — это viewBox, в котором нарисованы все пути набора
    /// (раздел 7). Пойди она за размером, Viewbox начал бы вписывать клетку в
    /// клетку вместо рисунка в рамку, и при токене 24 путь занял бы шестнадцать
    /// единиц в углу двадцатичетырёх.
    /// </remarks>
    [AvaloniaFact]
    public void An_icon_follows_its_token_and_its_grid_does_not()
    {
        var icon = new AxIcon { Data = AxIcons.Plus };
        var window = Shown(icon);

        var grid = icon.GetVisualDescendants().OfType<Canvas>().Single();

        Assert.Equal(new Size(16, 16), icon.Bounds.Size);
        Assert.Equal(new Size(16, 16), grid.Bounds.Size);

        window.Resources["AxIconSize"] = 24d;
        window.UpdateLayout();

        Assert.Equal(new Size(24, 24), icon.Bounds.Size);
        Assert.Equal(new Size(16, 16), grid.Bounds.Size);

        window.Close();
    }

    /// <summary>Мелкая иконка идёт за своим токеном, а не за общим.</summary>
    [AvaloniaFact]
    public void A_small_icon_follows_the_token_of_its_own()
    {
        var icon = new AxIcon { Classes = { "small" }, Data = AxIcons.ChevronDown };
        var window = Shown(icon);

        Assert.Equal(new Size(12, 12), icon.Bounds.Size);

        window.Resources["AxIconSizeSmall"] = 20d;
        window.UpdateLayout();

        Assert.Equal(new Size(20, 20), icon.Bounds.Size);

        // Общий токен мелкую иконку не трогает: у неё отступление своё.
        window.Resources["AxIconSize"] = 32d;
        window.UpdateLayout();

        Assert.Equal(new Size(20, 20), icon.Bounds.Size);

        window.Close();
    }

    /// <summary>
    /// Колонка шеврона в дереве — та же мелкая иконка.
    /// </summary>
    /// <remarks>
    /// В колонке стоит шеврон класса small, и разъедься они — стрелка перестала
    /// бы попадать в свою клетку. До токена число двенадцать стояло в двух
    /// местах порознь.
    /// </remarks>
    [AvaloniaFact]
    public void The_tree_chevron_column_is_the_small_icon()
    {
        // Ветка с ребёнком: у пустой шеврона нет вовсе — мерить было бы нечего.
        var item = new AxTreeViewItem
        {
            Header = "Views",
            ItemsSource = new[] { new AxTreeViewItem { Header = "MainWindow.axaml" } },
        };

        var tree = new AxTreeView { ItemsSource = new[] { item } };
        var window = Shown(tree);

        var chevron = tree.GetVisualDescendants()
            .OfType<Control>()
            .First(child => child.Name == "PART_ExpandCollapseChevron");

        Assert.Equal(12d, chevron.Bounds.Width);

        window.Resources["AxIconSizeSmall"] = 20d;
        window.UpdateLayout();

        Assert.Equal(20d, chevron.Bounds.Width);

        window.Close();
    }

    /// <summary>
    /// Клетка иконки в пункте меню — из токена, и колонка идёт за ней.
    /// </summary>
    /// <remarks>
    /// Ширина колонки была написана числом рядом с размером клетки: два места и
    /// одно решение. Теперь колонка Auto — её меряет сама клетка, и разъехаться
    /// им негде. Меню в студии пока нет, поэтому живьём это не увидеть: проверка
    /// здесь — единственное, что стоит между колонкой и её иконкой.
    /// </remarks>
    [AvaloniaFact]
    public void The_menu_icon_column_is_measured_by_the_icon()
    {
        var item = new AxMenuItem { Header = "Добавить контрол", Icon = new AxIcon { Data = AxIcons.Plus } };
        var window = Shown(item);

        var slot = item.GetVisualDescendants().OfType<Viewbox>().First();
        var header = item.GetVisualDescendants()
            .OfType<Control>()
            .First(child => child.Name == "PART_HeaderPresenter");

        Assert.Equal(new Size(16, 16), slot.Bounds.Size);

        var was = header.TranslatePoint(default, item);

        Assert.NotNull(was);

        window.Resources["AxIconSize"] = 24d;
        window.UpdateLayout();

        var now = header.TranslatePoint(default, item);

        Assert.NotNull(now);
        Assert.Equal(new Size(24, 24), slot.Bounds.Size);

        // Заголовок съехал ровно на то, насколько выросла клетка. Мерить одну
        // клетку мало: в узкой колонке она переполняет её и остаётся широкой, а
        // текст стоит на месте — колонка и клетка разъехались бы молча.
        Assert.Equal(8d, now.Value.X - was.Value.X);

        window.Close();
    }

    /// <summary>Кегль темы — тоже токен: текст идёт за ним.</summary>
    [AvaloniaFact]
    public void Text_follows_the_size_token()
    {
        var button = new AxButton { Content = "Открыть" };
        var window = Shown(button);

        Assert.Equal(13d, button.FontSize);

        window.Resources["AxFontSize"] = 26d;
        window.UpdateLayout();

        Assert.Equal(26d, button.FontSize);

        window.Close();
    }

    /// <summary>Полоса прокрутки и её ползунок — из токенов.</summary>
    /// <remarks>
    /// Полоса всегда одной ширины, а меняется ползунок: так содержимое не
    /// дёргается. Оба числа стояли парами, по одному на ориентацию.
    /// </remarks>
    [AvaloniaFact]
    public void The_scroll_lane_and_its_thumb_come_from_tokens()
    {
        var bar = new ScrollBar { Orientation = Avalonia.Layout.Orientation.Vertical };
        var window = Shown(bar);

        var thumb = bar.GetVisualDescendants()
            .OfType<Control>()
            .First(child => child.Name == "PART_Thumb");

        Assert.Equal(12d, bar.Bounds.Width);
        Assert.Equal(6d, thumb.Bounds.Width);

        window.Resources["AxScrollBarLane"] = 20d;
        window.Resources["AxScrollThumbSize"] = 10d;
        window.UpdateLayout();

        Assert.Equal(20d, bar.Bounds.Width);
        Assert.Equal(10d, thumb.Bounds.Width);

        window.Close();
    }

    private static Window Shown(Control content)
    {
        var window = new Window
        {
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = content,
        };

        window.Show();
        window.UpdateLayout();

        return window;
    }
}
