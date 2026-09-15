using ArxisStudio.Controls;
using ArxisStudio.Icons;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Иконка рисуется в клетке 16 × 16 и не подгоняется под свои чернила.
/// </summary>
/// <remarks>
/// Набор нарисован в одной системе координат: глиф занимает в клетке ровно
/// столько, сколько задумано, и стоит там, где поставлен. Плюс — 9 × 9 в
/// середине, шеврон — 7.6 × 3.8 ниже центра, и это осмысленная разница, а не
/// небрежность.
///
/// Стоит подогнать путь под рамку — и разница пропадает: каждый глиф
/// раздувается до краёв своим множителем (плюс в 1.23 раза, шеврон в 1.33) и
/// садится по центру своих чернил, а не клетки. Обводка 1.2 растёт вместе с
/// ним. Снаружи это выглядит как «иконки разного размера и не отцентрованы» —
/// и ровно так и было, пока путь лежал в Viewbox без клетки.
///
/// Дуга спиннера от этого ещё и вращалась вокруг центра своих чернил, а не
/// вокруг центра окружности: он уезжал, и лоадер крутился «от края».
/// </remarks>
public class IconRenderTests
{
    /// <summary>Клетка иконки, в которой нарисован весь набор.</summary>
    private const double Cell = 16d;

    [AvaloniaTheory]
    [MemberData(nameof(Samples))]
    public void Icon_keeps_the_scale_of_its_grid(string name)
    {
        var icon = new AxIcon { Data = Icon(name) };
        var window = Shown(icon);

        Assert.Equal(Cell, icon.Bounds.Width);
        Assert.Equal(Cell, Inner(icon).Width);
        Assert.Equal(Cell, Inner(icon).Height);

        window.Close();
    }

    /// <summary>
    /// Мелкий шеврон — единственное отступление от 16 — уменьшается целиком,
    /// вместе с клеткой, а не подгоняется чернилами.
    /// </summary>
    [AvaloniaFact]
    public void Small_icon_scales_the_whole_cell()
    {
        var icon = new AxIcon { Classes = { "small" }, Data = AxIcons.ChevronDown };
        var window = Shown(icon);

        Assert.Equal(12d, icon.Bounds.Width);
        Assert.Equal(Cell, Inner(icon).Width);

        window.Close();
    }

    /// <summary>
    /// Обводка остаётся 1.2 — её масштабировала та же подгонка.
    /// </summary>
    [AvaloniaFact]
    public void Icon_keeps_the_stroke_of_the_set()
    {
        var icon = new AxIcon { Data = AxIcons.Plus };
        var window = Shown(icon);

        Assert.Equal(1.2d, Stroke(icon));

        window.Close();
    }

    /// <summary>
    /// Клетка в два пикселя — обводка ровно в клетку: заданные 1.2 легли бы
    /// 2.4 пикселя, и ось на границе пикселя обросла бы каймой с обеих сторон.
    /// У неквадратной рамки клетку задаёт короткая сторона — по ней путь и вписан.
    /// </summary>
    [AvaloniaTheory]
    [InlineData(32d, 32d)]
    [InlineData(48d, 48d)]
    [InlineData(40d, 32d)]
    public void On_whole_pixels_a_cell_the_stroke_is_one_cell(double width, double height)
    {
        var icon = new AxIcon { Data = AxIcons.Plus, Width = width, Height = height };
        var window = Shown(icon);

        Assert.Equal(1d, Stroke(icon), 6);

        window.Close();
    }

    /// <summary>
    /// Между целыми клетками — и в мелком шевроне — обводка остаётся той, что
    /// задана: полуклетка там падает на доли пикселя, и толщина её не соберёт.
    /// </summary>
    [AvaloniaTheory]
    [InlineData(12d)]
    [InlineData(24d)]
    [InlineData(40d)]
    public void Between_whole_cells_the_stroke_stays_as_given(double size)
    {
        var icon = new AxIcon { Data = AxIcons.Plus, Width = size, Height = size };
        var window = Shown(icon);

        Assert.Equal(1.2d, Stroke(icon));

        window.Close();
    }

    /// <summary>
    /// Иконка, сжатая обратно в 16, возвращает заданную обводку и слушает своё
    /// свойство дальше: сведённое значение не остаётся на пути навсегда.
    /// </summary>
    [AvaloniaFact]
    public void A_shrunk_icon_gets_the_stroke_of_its_property_back()
    {
        var icon = new AxIcon { Data = AxIcons.Plus, Width = 32, Height = 32 };
        var window = Shown(icon);

        icon.Width = icon.Height = Cell;
        window.UpdateLayout();

        Assert.Equal(1.2d, Stroke(icon));

        icon.StrokeThickness = 1.5;

        Assert.Equal(1.5d, Stroke(icon));

        window.Close();
    }

    /// <summary>
    /// Окно, переехавшее на экран с другим масштабом, сводит обводку заново:
    /// та же иконка в 16 точек при 200% получает клетку, при 150% — заданную.
    /// </summary>
    [AvaloniaFact]
    public void The_stroke_follows_the_scale_of_the_screen()
    {
        var icon = new AxIcon { Data = AxIcons.Plus };
        var window = Shown(icon);

        window.SetRenderScaling(2);
        Assert.Equal(1d, Stroke(icon), 6);

        window.SetRenderScaling(1.5);
        Assert.Equal(1.2d, Stroke(icon));

        window.SetRenderScaling(3);
        Assert.Equal(1d, Stroke(icon), 6);

        window.Close();
    }

    public static TheoryData<string> Samples =>
        ["Plus", "ChevronDown", "Search", "Close", "Play", "Folder", "Check", "Settings"];

    private static Geometry Icon(string name)
        => (Geometry)typeof(AxIcons).GetProperty(name)!.GetValue(null)!;

    /// <summary>Толщина, которой путь в шаблоне действительно рисуется.</summary>
    private static double Stroke(AxIcon icon)
        => icon.GetVisualDescendants()
            .OfType<Avalonia.Controls.Shapes.Path>()
            .Single()
            .StrokeThickness;

    /// <summary>Клетка внутри Viewbox: её размер и есть знаменатель масштаба.</summary>
    private static Rect Inner(AxIcon icon)
        => icon.GetVisualDescendants()
            .OfType<Canvas>()
            .Single()
            .Bounds;

    private static Window Shown(Control control)
    {
        var window = new Window { Content = control };

        window.Show();
        window.UpdateLayout();

        return window;
    }
}
