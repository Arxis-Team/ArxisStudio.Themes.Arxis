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
    /// Мелкий значок уменьшается целиком, вместе с клеткой, а не подгоняется чернилами.
    /// </summary>
    /// <remarks>
    /// Клетка у него своя — двенадцать: набор нарисован и в ней. Прежде путь клетки 16 сжимался в
    /// двенадцать точек, на единицу приходилось три четверти пикселя, и от штриха оставалась
    /// серая полоска: доля сплошных пикселей по всему набору была 1.3 %.
    /// </remarks>
    [AvaloniaFact]
    public void Small_icon_scales_the_whole_cell()
    {
        var icon = new AxIcon { Size = AxIconSize.Small, Data = AxIcons.ChevronDown };
        var window = Shown(icon);

        Assert.Equal(12d, icon.Bounds.Width);
        Assert.Equal(icon.Cell, Inner(icon).Width);
        Assert.Equal(12d, icon.Cell);

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
    /// В клетке, под которую набор нарисован, штрих — ровно пиксель.
    /// </summary>
    /// <remarks>
    /// Набор нарисован в четырёх клетках: 16, 20, 24 и 28. Значок, занявший столько же пикселей,
    /// берёт свою клетку — там на единицу приходится пиксель, оси стоят в его середине, и резким
    /// выходит нечётный штрих. Один пиксель — тот же волосок, что и при 100 %, где заданные 1.2
    /// ложатся ровно в пиксель.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData(12d)]
    [InlineData(20d)]
    [InlineData(24d)]
    [InlineData(28d)]
    public void In_a_cell_of_its_own_the_stroke_is_a_pixel(double size)
    {
        var icon = new AxIcon { Data = AxIcons.Plus, Width = size, Height = size };
        var window = Shown(icon);

        Assert.Equal(size, icon.Cell);
        Assert.Equal(1d, Stroke(icon));

        window.Close();
    }

    /// <summary>
    /// Мимо клеток набора обводка остаётся той, что задана: полуклетка там падает на доли
    /// пикселя, и толщина её не соберёт.
    /// </summary>
    [AvaloniaTheory]
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
        Assert.Equal(16d, icon.Cell);

        // При 150 % значок в шестнадцать точек занимает 24 пикселя — и берёт клетку 24, в которой
        // набор нарисован тоже. Штрих там ровно пиксель.
        window.SetRenderScaling(1.5);
        Assert.Equal(24d, icon.Cell);
        Assert.Equal(1d, Stroke(icon));

        window.SetRenderScaling(3);
        Assert.Equal(16d, icon.Cell);
        Assert.Equal(1d, Stroke(icon), 6);

        window.Close();
    }

    /// <summary>
    /// Силуэт, растянутый туда, где единица сетки не целая, рисуется в клетке своих пикселей, а где
    /// целая — в клетке 16.
    /// </summary>
    /// <remarks>
    /// Плитку окна проекта растягивают ступенями по 16 точек: у 48 при 125 % на единицу приходится
    /// 3,75 пикселя, и путь клетки 16 положил бы край заливки внутрь пикселя — контрол берёт силуэт,
    /// посаженный на 60 пикселей значка. У 64 единица — пять пикселей, и путь стоит на них сам.
    /// </remarks>
    [AvaloniaFact]
    public void A_stretched_silhouette_takes_the_cell_of_its_pixels()
    {
        var icon = new AxIcon { Data = AxIcons.FolderTile, Width = 48, Height = 48 };
        var window = Shown(icon);

        window.SetRenderScaling(1.25);
        window.UpdateLayout();

        Assert.Equal(60d, icon.Cell);
        Assert.Equal(60d, Inner(icon).Width);
        Assert.NotSame(AxIcons.FolderTile, icon.Shown);

        icon.Width = icon.Height = 64;
        window.UpdateLayout();

        Assert.Equal(Cell, icon.Cell);
        Assert.Same(AxIcons.FolderTile, icon.Shown);

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
