using ArxisStudio.Controls;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Область выделения: полным цветом горит та строка, в чьём списке стоит клавиатура.
/// </summary>
/// <remarks>
/// До этой вехи полную силу выделению давали общие стили с предком —
/// <c>:is(ListBox):focus-within ax|AxListBoxItem:selected</c>. Предок в селекторе не различает
/// вложенные списки, не достаёт до списка в попапе и пропадает вместе с фокусом, ушедшим в меню:
/// у окна попапа нет визуального предка. Здесь проверены все три случая — и четвёртый, ради
/// которого область и заведена: панель остаётся активной, пока её меню открыто.
/// </remarks>
public class SelectionScopeTests
{
    /// <summary>Выбранная строка горит, пока клавиатура в её списке, и гаснет, когда ушла.</summary>
    [AvaloniaFact]
    public void A_selected_row_lights_up_while_the_keyboard_is_in_its_list()
    {
        var list = List("MainWindow.axaml", "ChatView.axaml");
        var away = new AxTextBox();
        var window = Shown(new StackPanel { Children = { list, away } });

        list.SelectedIndex = 0;
        Row(list).Focus();
        window.UpdateLayout();

        Assert.Equal(Resource(window, "AxSelectionActiveColor"), Fill(list));

        away.Focus();
        window.UpdateLayout();

        Assert.Equal(Resource(window, "AxSelectionInactiveColor"), Fill(list));

        window.Close();
    }

    /// <summary>
    /// Вложенный список не загорается от фокуса внешнего.
    /// </summary>
    /// <remarks>
    /// Предок в селекторе на это не смотрел вовсе: внутренний список лежит внутри внешнего, и
    /// <c>:focus-within</c> у внешнего означал «горят оба».
    /// </remarks>
    [AvaloniaFact]
    public void A_nested_list_keeps_its_own_selection_calm()
    {
        var inner = List("ChatView.axaml", "LoginView.axaml");
        var outer = new AxListBox { ItemsSource = new Control[] { new AxListBoxItem { Content = "Проект" }, inner } };
        var window = Shown(outer);

        inner.SelectedIndex = 0;
        outer.SelectedIndex = 0;
        window.UpdateLayout();

        Row(outer).Focus();
        window.UpdateLayout();

        Assert.Equal(Resource(window, "AxSelectionActiveColor"), Fill(outer));
        Assert.Equal(Resource(window, "AxSelectionInactiveColor"), Fill(inner));

        window.Close();
    }

    /// <summary>
    /// Открытое над строкой меню выделения не гасит.
    /// </summary>
    /// <remarks>
    /// Это и есть тот случай, ради которого область смотрит на логическую цепочку: у окна попапа
    /// нет визуального предка, и <c>:focus-within</c> у списка пропадал ровно в тот миг, когда
    /// человек выбирает действие над выделенной строкой.
    /// </remarks>
    [AvaloniaFact]
    public void An_open_menu_over_a_row_keeps_the_selection_lit()
    {
        var list = List("MainWindow.axaml", "ChatView.axaml");
        var item = new AxMenuItem { Header = "Открыть" };
        var menu = new ContextMenu { ItemsSource = new[] { item } };
        var window = Shown(list);

        list.SelectedIndex = 0;
        Row(list).Focus();
        window.UpdateLayout();

        list.ContextMenu = menu;
        menu.Open(list);
        window.UpdateLayout();

        item.Focus();
        window.UpdateLayout();

        Assert.Equal(Resource(window, "AxSelectionActiveColor"), Fill(list));

        menu.Close();
        window.Close();
    }

    /// <summary>
    /// Список в попапе поиска горит, хотя фокус стоит в строке запроса.
    /// </summary>
    /// <remarks>
    /// Поиск — одна область целиком: и поле, и список под ним. Стилю с предком это не давалось
    /// никак, и выбор в палитре команд всегда выглядел погашенным.
    /// </remarks>
    [AvaloniaFact]
    public void The_quick_search_list_lights_up_from_its_own_field()
    {
        var search = new AxQuickSearch { ItemsSource = new[] { "ChatView.axaml", "ChatService.cs" } };
        var window = Shown(search);

        search.SelectedItem = "ChatView.axaml";
        window.UpdateLayout();

        var field = search.GetVisualDescendants().OfType<AxTextBox>().First();

        field.Focus();
        window.UpdateLayout();

        var row = search.GetVisualDescendants().OfType<AxListBoxItem>().First(item => item.IsSelected);

        Assert.Equal(Resource(window, "AxSelectionActiveColor"), Colour(Presenter(row).Background));

        window.Close();
    }

    /// <summary>Панель остаётся активной, пока открыто её меню.</summary>
    [AvaloniaFact]
    public void A_panel_stays_active_while_its_menu_is_open()
    {
        var inside = new AxTextBox();
        var panel = new AxToolWindow { Title = "Консоль", Content = inside };
        var item = new AxMenuItem { Header = "Очистить" };
        var menu = new ContextMenu { ItemsSource = new[] { item } };
        var window = Shown(panel);

        inside.Focus();
        window.UpdateLayout();

        Assert.Contains(":active", panel.Classes);

        panel.ContextMenu = menu;
        menu.Open(panel);
        window.UpdateLayout();

        item.Focus();
        window.UpdateLayout();

        Assert.Contains(":active", panel.Classes);

        menu.Close();
        window.Close();
    }

    private static AxListBox List(params string[] items) =>
        new() { ItemsSource = items, Width = 200, Height = 80 };

    /// <summary>Выбранная строка списка: фокус ставят на неё, а не на список.</summary>
    private static AxListBoxItem Row(AxListBox list) =>
        list.GetVisualDescendants().OfType<AxListBoxItem>().First(item => item.IsSelected);

    private static Color? Fill(AxListBox list) => Colour(Presenter(Row(list)).Background);

    private static ContentPresenter Presenter(Control row) =>
        row.GetVisualDescendants().OfType<ContentPresenter>().First(child => child.Name == "PART_ContentPresenter");

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key)
    {
        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value), key);

        return (Color)value!;
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
