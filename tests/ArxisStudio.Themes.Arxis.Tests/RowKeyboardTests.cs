using ArxisStudio.Controls;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Клавиатура в списке и в дереве: поиск набором и клавиша контекстного меню.
/// </summary>
/// <remarks>
/// Список в сотню строк не листают стрелкой по одной, и мышь ради этого не берут: в проводнике
/// Windows, в Rider и в любом файловом дереве набор букв ведёт к нужной строке. Avalonia не делает
/// ни того, ни другого: <c>ContextRequested</c> она поднимает только от указателя, и всё, что
/// живёт в контекстном меню, с клавиатуры было недостижимо вовсе.
/// </remarks>
public class RowKeyboardTests
{
    /// <summary>Набор ведёт выбор к строке, которая с него начинается.</summary>
    [AvaloniaFact]
    public void Typing_walks_the_list_to_the_matching_row()
    {
        var (list, window) = List("Program.cs", "App.axaml", "MainWindow.axaml", "StudioDock.cs");

        Type(window, "m");

        Assert.Equal("MainWindow.axaml", list.SelectedItem);

        window.Close();
    }

    /// <summary>Буквы складываются в слово, пока их набирают подряд.</summary>
    [AvaloniaFact]
    public void The_letters_add_up_into_one_word()
    {
        var (list, window) = List("Стрелка", "Строка", "Столбец");

        Type(window, "стро");

        Assert.Equal("Строка", list.SelectedItem);

        window.Close();
    }

    /// <summary>Та же буква подряд ведёт к следующей строке на неё.</summary>
    /// <remarks>Так ищут, не помня названия целиком: жмут первую букву, пока не покажется нужное.</remarks>
    [AvaloniaFact]
    public void The_same_letter_walks_to_the_next_match()
    {
        var (list, window) = List("Сборка", "Сессия", "Строка", "Проект");

        Type(window, "с");

        Assert.Equal("Сборка", list.SelectedItem);

        Type(window, "с");

        Assert.Equal("Сессия", list.SelectedItem);

        Type(window, "с");

        Assert.Equal("Строка", list.SelectedItem);

        window.Close();
    }

    /// <summary>Каретка идёт за выбором: следующая буква достаётся тому же списку.</summary>
    /// <remarks>
    /// Строку, с которой каретка ушла, список мог и переработать — она уехала за край, — и буква,
    /// набранная следом, не досталась бы никому.
    /// </remarks>
    [AvaloniaFact]
    public void The_caret_follows_the_chosen_row()
    {
        var (list, window) = List("Сборка", "Сессия", "Строка");

        Type(window, "стр");

        Assert.Equal("Строка", list.SelectedItem);

        var chosen = list.GetVisualDescendants().OfType<AxListBoxItem>().Single(row => row.IsSelected);

        Assert.True(chosen.IsFocused, "каретка осталась на прежней строке");

        window.Close();
    }

    /// <summary>Строка, которой в списке нет, выбора не двигает.</summary>
    [AvaloniaFact]
    public void A_word_that_matches_nothing_leaves_the_choice_alone()
    {
        var (list, window) = List("Сборка", "Сессия");

        list.SelectedIndex = 0;
        window.UpdateLayout();

        Type(window, "ю");

        Assert.Equal("Сборка", list.SelectedItem);

        window.Close();
    }

    /// <summary>Набор ищет и в дереве — по раскрытой его части.</summary>
    [AvaloniaFact]
    public void Typing_walks_the_visible_rows_of_a_tree()
    {
        var hidden = new AxTreeViewItem { Header = "Спрятанная" };
        var branch = new AxTreeViewItem { Header = "Ветка", IsExpanded = false };

        branch.Items.Add(hidden);

        var tree = new AxTreeView();

        tree.Items.Add(new AxTreeViewItem { Header = "Solution" });
        tree.Items.Add(branch);
        tree.Items.Add(new AxTreeViewItem { Header = "Сборка" });

        var window = Shown(tree);

        tree.GetVisualDescendants().OfType<AxTreeViewItem>().First().Focus();
        window.UpdateLayout();

        Type(window, "с");

        // Свёрнутая ветка прячет своих детей и от глаз, и от поиска: «Спрятанная» стоит раньше
        // «Сборки», но её на экране нет.
        Assert.True(((AxTreeViewItem)tree.Items[2]!).IsSelected, "набор не дошёл до видимой строки");
        Assert.False(hidden.IsSelected, "набор нашёл строку в свёрнутой ветке");

        window.Close();
    }

    /// <summary>Клавиша меню просит контекстное меню там, где стоит клавиатура.</summary>
    [AvaloniaTheory]
    [InlineData(Key.Apps, PhysicalKey.ContextMenu, RawInputModifiers.None)]
    [InlineData(Key.F10, PhysicalKey.F10, RawInputModifiers.Shift)]
    public void The_menu_key_asks_for_the_context_menu(Key key, PhysicalKey physical, RawInputModifiers raw)
    {
        var (list, window) = List("Program.cs", "App.axaml");
        var asked = 0;

        list.AddHandler(InputElement.ContextRequestedEvent, (_, _) => asked++);

        window.KeyPress(key, raw, physical, string.Empty);
        window.UpdateLayout();

        Assert.Equal(1, asked);

        window.Close();
    }

    /// <summary>Клавиша меню открывает меню и в дереве.</summary>
    [AvaloniaFact]
    public void The_menu_key_works_in_a_tree_too()
    {
        var tree = new AxTreeView();

        tree.Items.Add(new AxTreeViewItem { Header = "Solution" });

        var window = Shown(tree);
        var asked = 0;

        tree.AddHandler(InputElement.ContextRequestedEvent, (_, _) => asked++);
        tree.GetVisualDescendants().OfType<AxTreeViewItem>().First().Focus();
        window.UpdateLayout();

        window.KeyPress(Key.Apps, RawInputModifiers.None, PhysicalKey.ContextMenu, string.Empty);
        window.UpdateLayout();

        Assert.Equal(1, asked);

        window.Close();
    }

    private static void Type(Window window, string text)
    {
        foreach (var letter in text)
        {
            window.KeyTextInput(letter.ToString());
            window.UpdateLayout();
        }
    }

    /// <summary>
    /// Список с кареткой в нём: набор идёт туда, где стоит фокус.
    /// </summary>
    /// <remarks>
    /// Каретка ставится на строку, а не на сам список: фокус на списке в безголовом прогоне никуда
    /// не встаёт, и клавиши уходили бы в пустоту. Выбор после этого снимается — проверяется
    /// именно набор, а не то, с какой строки он начал.
    /// </remarks>
    private static (AxListBox List, Window Window) List(params string[] items)
    {
        var list = new AxListBox { ItemsSource = items, Width = 240, Height = 120 };
        var window = Shown(list);

        list.GetVisualDescendants().OfType<AxListBoxItem>().First().Focus();
        list.SelectedIndex = -1;
        window.UpdateLayout();

        return (list, window);
    }

    private static Window Shown(Control content)
    {
        var window = new Window
        {
            Width = 400,
            Height = 300,
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = content,
        };

        window.Show();
        window.UpdateLayout();

        return window;
    }
}
