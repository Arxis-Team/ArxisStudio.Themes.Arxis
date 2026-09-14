using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Длинный список — одна остановка Tab, и строка, на которую она приходит, видна.
/// </summary>
/// <remarks>
/// Прогон студии на просторной ступени плотности назвал строку журнала «достижимой с Tab, но
/// невидимой». Опыт показал, что это не так: список входит в обход один раз, на первую строку или
/// на ту, что была в руках, а окно прокрутки подвозит её в вид. Инструмент считал достижимыми все
/// строки, которые виртуализация держит про запас за краем окна.
/// <para>
/// Поведение это даёт Avalonia, а сломать его проще всего здесь: одна строка
/// <c>KeyboardNavigation.TabNavigation</c> в теме списка — и Tab пошёл бы по каждой строке, в том
/// числе по тем, которых не видно.
/// </para>
/// </remarks>
public class ListBoxTabTests
{
    /// <summary>Tab входит в список на первую строку и следующим нажатием выходит из него.</summary>
    [AvaloniaFact]
    public void A_long_list_is_a_single_tab_stop()
    {
        var (window, before, list, after) = Shown();

        before.Focus();

        Tab(window);

        var entered = Assert.IsAssignableFrom<ListBoxItem>(window.FocusManager?.GetFocusedElement());

        Assert.Equal("Строка 1", entered.Content);

        Tab(window);

        var left = window.FocusManager?.GetFocusedElement();

        Assert.True(ReferenceEquals(after, left), $"второй Tab остался в списке: фокус у {left?.GetType().Name ?? "никого"}");

        window.Close();
    }

    /// <summary>
    /// Вернувшись в список, Tab приходит на строку, что была в руках, — и та видна.
    /// </summary>
    [AvaloniaFact]
    public void Tabbing_back_brings_the_row_in_hand_into_view()
    {
        var (window, before, list, _) = Shown();

        list.SelectedIndex = 24;
        list.ScrollIntoView(24);
        Dispatcher.UIThread.RunJobs();
        Assert.IsAssignableFrom<ListBoxItem>(list.ContainerFromIndex(24)).Focus();

        before.Focus();
        list.ScrollIntoView(0);
        Dispatcher.UIThread.RunJobs();

        Tab(window);

        var row = Assert.IsAssignableFrom<ListBoxItem>(window.FocusManager?.GetFocusedElement());
        var top = row.TranslatePoint(default, list)!.Value.Y;

        Assert.Equal("Строка 25", row.Content);
        Assert.True(
            top >= 0 && top + row.Bounds.Height <= list.Bounds.Height,
            $"строка в руках пришла за край окна: {top} при высоте списка {list.Bounds.Height}");

        window.Close();
    }

    private static (Window Window, AxButton Before, AxListBox List, AxButton After) Shown()
    {
        var before = new AxButton { Content = "До" };
        var after = new AxButton { Content = "После" };
        var list = new AxListBox
        {
            Height = 60,
            ItemsSource = Enumerable.Range(1, 30).Select(number => $"Строка {number}").ToList(),
        };

        var window = new Window
        {
            Width = 300,
            Height = 300,
            Content = new StackPanel { Children = { before, list, after } },
        };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        return (window, before, list, after);
    }

    private static void Tab(Window window)
    {
        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, string.Empty);
        Dispatcher.UIThread.RunJobs();
    }
}
