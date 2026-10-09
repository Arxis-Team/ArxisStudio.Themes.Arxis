using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Поле вектора: отступ, скругление, точка, размер.
/// </summary>
/// <remarks>
/// Вектор пишут одним значением — <c>Margin="4,8"</c>, — поэтому правка любой ячейки выходит наружу всем
/// вектором. Связанные значения меняются разом, Esc посреди протяжки возвращает весь вектор, а узкая колонка
/// переносит ячейки поровну, а не рядом и сиротой.
/// </remarks>
public class VectorFieldTests
{
    [AvaloniaFact]
    public void A_cell_edit_commits_the_whole_vector()
    {
        var (field, window) = Shown();
        var committed = Committed(field);

        Type(window, field.Cells[1], "8");
        Press(window, Key.Enter);

        var (values, index) = Assert.Single(committed);

        Assert.Equal([1d, 8d, 3d, 4d], values);
        Assert.Equal(1, index);
        Assert.Equal([1d, 8d, 3d, 4d], field.Values!);

        window.Close();
    }

    /// <summary>Связанные значения — «одинаково со всех сторон»: правка любой ячейки ставит её число во все.</summary>
    [AvaloniaFact]
    public void Linked_values_change_together()
    {
        var (field, window) = Shown(setup: field => field.IsLinked = true);
        var committed = Committed(field);

        Type(window, field.Cells[2], "5");
        Press(window, Key.Enter);

        var (values, index) = Assert.Single(committed);

        Assert.Equal([5d, 5d, 5d, 5d], values);
        Assert.Equal(-1, index);
        Assert.All(field.Cells, cell => Assert.Equal("5", cell.Text));

        window.Close();
    }

    /// <summary>Протяжка связанных видна во всех ячейках, а Esc возвращает весь вектор, а не одну тянутую.</summary>
    [AvaloniaFact]
    public void Escape_gives_back_every_linked_cell()
    {
        var (field, window) = Shown(setup: field => field.IsLinked = true);
        var committed = Committed(field);
        var changing = new List<IReadOnlyList<double>>();
        var cancelled = 0;

        field.ValuesChanging += (_, e) => changing.Add(e.Values);
        field.ScrubCancelled += (_, _) => cancelled++;

        var label = Assert.IsType<AxScrubLabel>(field.Cells[0].InnerLeftContent);
        var at = label.TranslatePoint(new Point(label.Bounds.Width / 2, label.Bounds.Height / 2), window)!.Value;

        window.MouseDown(at, MouseButton.Left);
        window.MouseMove(at + new Vector(5, 0));
        window.MouseMove(at + new Vector(105, 0));

        Assert.NotEmpty(changing);
        Assert.All(field.Cells, cell => Assert.Equal(field.Cells[0].Value, cell.Value));

        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, string.Empty);
        window.MouseUp(at + new Vector(105, 0), MouseButton.Left);

        Assert.Equal([1d, 2d, 3d, 4d], field.Values!);
        Assert.Equal([1d, 2d, 3d, 4d], field.Cells.Select(cell => cell.Value));
        Assert.Equal(1, cancelled);
        Assert.Empty(committed);

        window.Close();
    }

    /// <summary>Разные значения — по составляющей: прочерк только у той ячейки, что расходится.</summary>
    [AvaloniaFact]
    public void Only_the_mixed_component_shows_its_dash()
    {
        var (field, window) = Shown(setup: field => field.MixedMask = 0b0010);

        Assert.Equal([false, true, false, false], field.Cells.Select(cell => cell.IsMixed));
        Assert.Equal(string.Empty, field.Cells[1].Text);

        Type(window, field.Cells[1], "6");
        Press(window, Key.Enter);

        Assert.Equal(0, field.MixedMask);

        window.Close();
    }

    /// <summary>Тесно — ячейки переносятся поровну: четыре становятся двумя по две, три — столбцом, а не двумя и сиротой.</summary>
    [AvaloniaFact]
    public void A_narrow_field_wraps_its_cells_into_even_rows()
    {
        var (field, window) = Shown();
        var panel = field.GetVisualDescendants().OfType<AxVectorPanel>().Single();
        var cell = panel.CellMinWidth;
        var gap = panel.Spacing;

        Assert.Equal(4, panel.Columns);

        field.Width = 4 * cell + 3 * gap - 1;
        window.UpdateLayout();

        Assert.Equal(2, panel.Columns);
        Assert.True(
            field.Cells[2].Bounds.Top > field.Cells[0].Bounds.Top,
            "третья ячейка осталась в первом ряду");

        Assert.Equal(1, panel.ColumnsFor(3 * cell + 2 * gap - 1, 3));
        Assert.Equal(3, panel.ColumnsFor(3 * cell + 2 * gap, 3));

        window.Close();
    }

    /// <summary>Ячейку диктор называет именем поля и подписью — «Margin L», — а не одной буквой.</summary>
    [AvaloniaFact]
    public void Cells_are_named_after_the_field()
    {
        var (field, window) = Shown(setup: field => AutomationProperties.SetName(field, "Margin"));

        Assert.Equal(["Margin L", "Margin T", "Margin R", "Margin B"], field.Cells.Select(AutomationProperties.GetName));

        window.Close();
    }

    /// <summary>Переключатель связи стоит, только если связь разрешена, и сам включает её.</summary>
    [AvaloniaFact]
    public void The_link_toggle_shows_where_linking_is_allowed()
    {
        var (field, window) = Shown();
        var link = field.GetVisualDescendants().OfType<AxToggleButton>().Single(button => button.Name == "PART_Link");

        Assert.False(link.IsVisible, "переключатель связи виден без разрешения");

        field.CanLink = true;
        window.UpdateLayout();
        Assert.True(link.IsVisible, "переключатель связи не виден");

        link.IsChecked = true;
        Assert.True(field.IsLinked, "переключатель не связал значения");

        window.Close();
    }

    private static (AxVectorField Field, Window Window) Shown(Action<AxVectorField>? setup = null)
    {
        var field = new AxVectorField
        {
            Labels = ["L", "T", "R", "B"],
            Values = [1, 2, 3, 4],
            Width = 400,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
        };

        setup?.Invoke(field);

        var window = new Window { Width = 500, Height = 200, Content = field };

        window.Show();
        window.UpdateLayout();

        return (field, window);
    }

    private static List<(IReadOnlyList<double> Values, int Index)> Committed(AxVectorField field)
    {
        var committed = new List<(IReadOnlyList<double>, int)>();

        field.ValuesCommitted += (_, e) => committed.Add((e.Values, e.Index));

        return committed;
    }

    private static void Type(Window window, AxNumberField cell, string text)
    {
        cell.Focus();
        cell.SelectAll();
        window.KeyTextInput(text);
    }

    private static void Press(Window window, Key key)
    {
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, string.Empty);
        window.UpdateLayout();
    }
}
