using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
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
    public void Icon_keeps_the_scale_of_the_design_grid(string name)
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
    public void Icon_keeps_the_stroke_of_the_specification()
    {
        var icon = new AxIcon { Data = AxIcons.Plus };
        var window = Shown(icon);

        var path = icon.GetVisualDescendants()
            .OfType<Avalonia.Controls.Shapes.Path>()
            .Single();

        Assert.Equal(1.2d, path.StrokeThickness);

        window.Close();
    }

    /// <summary>Дуга лоадера вращается вокруг центра клетки.</summary>
    [AvaloniaFact]
    public void Spinner_turns_around_the_centre_of_its_cell()
    {
        var spinner = new AxSpinner();
        var window = Shown(spinner);

        var arc = spinner.GetVisualDescendants().OfType<AxIcon>().Single();

        Assert.Equal(Cell, arc.Bounds.Width);
        Assert.Equal(Cell, arc.Bounds.Height);
        Assert.Equal(RelativePoint.Center, arc.RenderTransformOrigin);
        Assert.Equal(Cell, Inner(arc).Width);

        window.Close();
    }

    public static TheoryData<string> Samples =>
        ["Plus", "ChevronDown", "Search", "Close", "Play", "Folder", "Check", "Settings"];

    private static Geometry Icon(string name)
        => (Geometry)typeof(AxIcons).GetProperty(name)!.GetValue(null)!;

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
