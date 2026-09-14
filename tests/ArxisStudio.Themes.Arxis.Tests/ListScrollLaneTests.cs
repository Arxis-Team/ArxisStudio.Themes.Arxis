using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Строка списка держит содержимое вне полосы прокрутки, а подсветку — во всю ширину.
/// </summary>
/// <remarks>
/// Полоса прокрутки лежит поверх строк и ширины у них не отнимает: так решено темой, чтобы
/// содержимое не дёргалось, когда список начинает прокручиваться. Но подпись у правого края строки —
/// сочетание клавиш в палитре команд, счётчик повторов в журнале — уходила под ползунок: строка
/// отступала от края на 8, а ползунок живёт в полосе шириной 12. В макете этого не видно: там полоса
/// тонкая, но отнимает ширину, и до неё строка не доходит.
/// </remarks>
public class ListScrollLaneTests
{
    /// <summary>Сколько строк заведомо не помещается в список.</summary>
    private const int Many = 40;

    /// <summary>Подпись у правого края строки кончается там, где начинается полоса прокрутки.</summary>
    [AvaloniaFact]
    public void A_hint_at_the_end_of_a_row_stays_out_of_the_scroll_lane()
    {
        var (list, window) = Shown(Many);
        var bar = VerticalBar(list);

        Assert.True(bar.IsVisible, "список не прокручивается, и полосы, в которую заходить, нет");

        var lane = bar.TranslatePoint(default, list)!.Value.X;
        var hints = Hints(list);

        Assert.NotEmpty(hints);

        foreach (var hint in hints)
        {
            var right = Right(hint, list);

            Assert.True(right <= lane + 0.01, $"подпись кончается на {right:0.##}, а полоса прокрутки начинается на {lane:0.##}");
        }

        window.Close();
    }

    /// <summary>
    /// Подпись стоит на одном месте, прокручивается список или нет: иначе она прыгала бы, когда
    /// поиск сужает список до пары строк.
    /// </summary>
    [AvaloniaFact]
    public void The_hint_stands_where_it_stood_when_the_list_stops_scrolling()
    {
        var (scrolling, first) = Shown(Many);
        var (still, second) = Shown(3);

        Assert.True(VerticalBar(scrolling).IsVisible, "длинный список не прокручивается");
        Assert.False(VerticalBar(still).IsVisible, "короткий список прокручивается");
        Assert.Equal(Right(Hints(scrolling)[0], scrolling), Right(Hints(still)[0], still));

        first.Close();
        second.Close();
    }

    /// <summary>
    /// Подсветка выбранной строки идёт во всю ширину и под ползунком: полоса не отнимает у строки
    /// ширины, отступает только содержимое.
    /// </summary>
    [AvaloniaFact]
    public void The_selection_still_runs_under_the_thumb()
    {
        var (list, window) = Shown(Many);

        list.SelectedIndex = 0;
        window.UpdateLayout();

        var viewer = list.GetVisualDescendants().OfType<ScrollViewer>().First();
        var row = list.GetVisualDescendants().OfType<AxListBoxItem>().First();

        Assert.Equal(viewer.Bounds.Width, row.Bounds.Width);

        window.Close();
    }

    private static (AxListBox List, Window Window) Shown(int rows)
    {
        var list = new AxListBox
        {
            Width = 240,
            Height = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            ItemsSource = Enumerable.Range(1, rows).Select(row => $"Команда {row}").ToList(),
            ItemTemplate = new FuncDataTemplate<string>((title, _) =>
            {
                var hint = new TextBlock { Text = "Ctrl+Alt+W", Tag = "hint" };

                DockPanel.SetDock(hint, Dock.Right);

                return new DockPanel { Children = { hint, new TextBlock { Text = title } } };
            }),
        };

        var window = new Window { Width = 320, Height = 240, Content = list };

        window.Show();
        window.UpdateLayout();

        return (list, window);
    }

    private static ScrollBar VerticalBar(AxListBox list) =>
        list.GetVisualDescendants().OfType<ScrollBar>().Single(bar => bar.Orientation == Orientation.Vertical);

    private static List<TextBlock> Hints(AxListBox list) =>
        [.. list.GetVisualDescendants().OfType<TextBlock>().Where(text => Equals(text.Tag, "hint"))];

    private static double Right(Visual visual, Visual relativeTo) =>
        visual.TranslatePoint(new Point(visual.Bounds.Width, 0), relativeTo)!.Value.X;
}
