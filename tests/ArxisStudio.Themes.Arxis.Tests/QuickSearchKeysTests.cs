using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Поиск разбирает клавиатуру сам: стрелки водят выбор, Enter говорит «это», Esc — «передумал».
/// </summary>
/// <remarks>
/// Прежде это лежало у палитры команд — её единственного потребителя, — и второй поиск в студии
/// начинался бы с переписывания тех же сорока строк. А поиск без клавиатуры не поиск: набирают в
/// нём всегда, и рука на стрелках, а не на мыши.
/// <para>
/// Клавиши карточка ловит на пути вниз, до поля ввода: выбор в списке — её дело, и решение о
/// стрелках и Enter принадлежит ей, что бы поле ни делало с ними завтра.
/// </para>
/// </remarks>
public class QuickSearchKeysTests
{
    /// <summary>Стрелка вниз ведёт выбор дальше, вверх — назад.</summary>
    [AvaloniaFact]
    public void The_arrows_walk_the_results()
    {
        var (search, window) = Shown("Собрать решение", "Собрать проект", "Очистить решение");

        search.SelectedItem = "Собрать решение";
        window.UpdateLayout();

        Press(window, Key.Down, PhysicalKey.ArrowDown);

        Assert.Equal("Собрать проект", search.SelectedItem);

        Press(window, Key.Up, PhysicalKey.ArrowUp);

        Assert.Equal("Собрать решение", search.SelectedItem);

        window.Close();
    }

    /// <summary>
    /// Выбор ходит по кругу.
    /// </summary>
    /// <remarks>
    /// Список короткий, и человек, дошедший стрелкой до низа, ждёт начала, а не тишины.
    /// </remarks>
    [AvaloniaFact]
    public void The_selection_wraps_at_both_ends()
    {
        var (search, window) = Shown("Собрать решение", "Собрать проект", "Очистить решение");

        search.SelectedItem = "Очистить решение";
        window.UpdateLayout();

        Press(window, Key.Down, PhysicalKey.ArrowDown);

        Assert.Equal("Собрать решение", search.SelectedItem);

        Press(window, Key.Up, PhysicalKey.ArrowUp);

        Assert.Equal("Очистить решение", search.SelectedItem);

        window.Close();
    }

    /// <summary>Enter принимает выбранное, Esc бросает поиск.</summary>
    [AvaloniaFact]
    public void Enter_accepts_and_escape_cancels()
    {
        var (search, window) = Shown("Собрать решение", "Собрать проект");
        var accepted = 0;
        var cancelled = 0;

        search.Accepted += (_, _) => accepted++;
        search.Cancelled += (_, _) => cancelled++;
        search.SelectedItem = "Собрать проект";
        window.UpdateLayout();

        Press(window, Key.Enter, PhysicalKey.Enter, "\r");

        Assert.Equal(1, accepted);
        Assert.Equal(0, cancelled);
        Assert.Equal("Собрать проект", search.SelectedItem);

        Press(window, Key.Escape, PhysicalKey.Escape);

        Assert.Equal(1, accepted);
        Assert.Equal(1, cancelled);

        window.Close();
    }

    /// <summary>Щелчок по строке — это и выбор, и «да».</summary>
    /// <remarks>
    /// Ждать после щелчка ещё и Enter человека заставляет только список, в котором выбирают, а не
    /// открывают.
    /// </remarks>
    [AvaloniaFact]
    public void A_click_on_a_row_accepts_it()
    {
        var (search, window) = Shown("Собрать решение", "Собрать проект");
        var accepted = 0;

        search.Accepted += (_, _) => accepted++;

        var row = search.GetVisualDescendants().OfType<AxListBoxItem>().Last();
        var at = row.TranslatePoint(new Avalonia.Point(row.Bounds.Width / 2, row.Bounds.Height / 2), window)!.Value;

        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        window.UpdateLayout();

        Assert.Equal(1, accepted);
        Assert.Equal("Собрать проект", search.SelectedItem);

        window.Close();
    }

    /// <summary>Набранное остаётся набранным: буквы карточка у поля не отбирает.</summary>
    [AvaloniaFact]
    public void Typing_still_reaches_the_field()
    {
        var (search, window) = Shown("Собрать решение");
        var field = search.GetVisualDescendants().OfType<AxTextBox>().First();

        window.KeyTextInput("со");
        window.UpdateLayout();

        Assert.Equal("со", field.Text);
        Assert.Equal("со", search.Text);

        window.Close();
    }

    private static void Press(Window window, Key key, PhysicalKey physical, string text = "") =>
        window.KeyPress(key, RawInputModifiers.None, physical, text);

    /// <summary>Карточка поиска в окне, с кареткой в строке запроса.</summary>
    private static (AxQuickSearch Search, Window Window) Shown(params string[] items)
    {
        var search = new AxQuickSearch { ItemsSource = items };

        var window = new Window
        {
            Width = 600,
            Height = 400,
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = search,
        };

        window.Show();
        window.UpdateLayout();

        search.GetVisualDescendants().OfType<AxTextBox>().First().Focus();
        window.UpdateLayout();

        return (search, window);
    }
}
