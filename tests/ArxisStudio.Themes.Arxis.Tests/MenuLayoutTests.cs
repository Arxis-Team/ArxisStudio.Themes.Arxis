using System.Globalization;
using ArxisStudio.Controls;
using ArxisStudio.Icons;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Меню: колонки выровнены по всему полотну, состояние показано галочкой, жест — платформенный.
/// </summary>
/// <remarks>
/// Колонки у пункта были свои: глиф занимал клетку значка, а жест стоял сразу за подписью — и
/// столбец шорткатов шёл по меню лесенкой, тем заметнее, чем разнее длины подписей. Состояния у
/// пункта не было вовсе: включённый режим нечем было назвать, и меню взглядов студии показывало
/// одинаковые строки. Жест же выводился привязкой к строке, то есть инвариантной записью
/// <c>Ctrl+Shift+P</c> — на macOS, где той же командой правит знак команды, это просто неправда.
/// </remarks>
public class MenuLayoutTests
{
    /// <summary>Подписи начинаются на одной вертикали — и со значком, и без.</summary>
    [AvaloniaFact]
    public void Headers_start_on_the_same_line_with_and_without_an_icon()
    {
        var pictured = new AxMenuItem { Header = "Сохранить", Icon = new AxIcon { Data = AxIcons.Check } };
        var plain = new AxMenuItem { Header = "Закрыть" };
        var (menu, window) = Shown(pictured, plain);

        Assert.Equal(Part(pictured, "PART_HeaderPresenter").Bounds.X, Part(plain, "PART_HeaderPresenter").Bounds.X);

        menu.Close();
        window.Close();
    }

    /// <summary>
    /// Колонка жеста одна на меню: подписи кончаются там, где начинаются шорткаты.
    /// </summary>
    /// <remarks>
    /// Колонка подписи тянущаяся, поэтому её правый край и есть левый край колонки жеста. Совпали
    /// они у пункта с жестом и у пункта без — значит место жесту отмерено по всему меню сразу, а
    /// не по каждому пункту порознь.
    /// </remarks>
    [AvaloniaFact]
    public void The_gesture_column_is_measured_across_the_whole_menu()
    {
        var saving = new AxMenuItem
        {
            Header = "Сохранить всё",
            InputGesture = new KeyGesture(Key.S, KeyModifiers.Control | KeyModifiers.Shift),
        };

        var plain = new AxMenuItem { Header = "Закрыть" };
        var (menu, window) = Shown(saving, plain);

        var gesture = Part(saving, "PART_InputGestureText");

        Assert.True(gesture.Bounds.Width > 0, "жест не занял места");
        Assert.Equal(
            Part(saving, "PART_HeaderPresenter").Bounds.Right,
            Part(plain, "PART_HeaderPresenter").Bounds.Right);

        menu.Close();
        window.Close();
    }

    /// <summary>Включённый пункт-переключатель помечен галочкой, выключенный — нет.</summary>
    [AvaloniaTheory]
    [InlineData(MenuItemToggleType.CheckBox)]
    [InlineData(MenuItemToggleType.Radio)]
    public void A_checked_item_wears_a_tick(MenuItemToggleType toggle)
    {
        var item = new AxMenuItem { Header = "Показывать журнал", ToggleType = toggle };
        var (menu, window) = Shown(item);

        Assert.False(Part(item, "PART_Check").IsVisible, "галочка у выключенного пункта");

        item.IsChecked = true;
        window.UpdateLayout();

        Assert.True(Part(item, "PART_Check").IsVisible, "галочки у включённого пункта нет");

        menu.Close();
        window.Close();
    }

    /// <summary>Жест написан так, как его пишет платформа.</summary>
    /// <remarks>
    /// Проверяется не буква записи, а её источник: шаблон спрашивает <c>KeyGesture</c> в формате
    /// платформы, и на macOS та же проверка ждёт знаков модификаторов, а не имён.
    /// </remarks>
    [AvaloniaFact]
    public void The_gesture_is_written_the_way_the_platform_writes_it()
    {
        var gesture = new KeyGesture(Key.P, KeyModifiers.Control | KeyModifiers.Shift);
        var item = new AxMenuItem { Header = "Палитра команд", InputGesture = gesture };
        var (menu, window) = Shown(item);

        var text = (TextBlock)Part(item, "PART_InputGestureText");

        Assert.Equal(gesture.ToString("p", CultureInfo.CurrentCulture), text.Text);

        menu.Close();
        window.Close();
    }

    /// <summary>Enter подписан так, как подписана клавиша, — Enter, а не Return.</summary>
    /// <remarks>
    /// У Avalonia Enter и Return — одно значение клавиши, и без своего слова платформы жест выходил
    /// «Return»: так меню окна проекта подписывало пункт «Открыть». Проверяется меню, а не
    /// преобразователь: подпись ставит шаблон, и держать надо то, что видно.
    /// </remarks>
    [AvaloniaFact]
    public void Enter_is_written_the_way_the_key_is_labelled()
    {
        var opening = new AxMenuItem { Header = "Открыть", InputGesture = new KeyGesture(Key.Enter) };
        var running = new AxMenuItem { Header = "Выполнить", InputGesture = new KeyGesture(Key.Enter, KeyModifiers.Control) };
        var (menu, window) = Shown(opening, running);

        Assert.Equal("Enter", ((TextBlock)Part(opening, "PART_InputGestureText")).Text);
        Assert.Equal("Ctrl+Enter", ((TextBlock)Part(running, "PART_InputGestureText")).Text);

        menu.Close();
        window.Close();
    }

    /// <summary>
    /// Разделитель в подменю — линия, а не строка: и родной, и тот, которым делит меню расширение.
    /// </summary>
    /// <remarks>
    /// Пункт подменю спрашивал про разделитель наравне с пунктами и заворачивал его в строку меню —
    /// с подсветкой под курсором и щелчком, — а в корне того же меню разделитель оставался линией.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData(typeof(Separator))]
    [InlineData(typeof(AxSeparator))]
    public void A_separator_in_a_submenu_stays_a_line(Type kind)
    {
        var line = (Separator)Activator.CreateInstance(kind)!;
        var branch = new AxMenuItem { Header = "Ветка" };

        branch.Items.Add(new AxMenuItem { Header = "Один" });
        branch.Items.Add(line);
        branch.Items.Add(new AxMenuItem { Header = "Два" });

        var (menu, window) = Shown(branch);

        branch.Open();
        Dispatcher.UIThread.RunJobs();

        Assert.Same(line, branch.ContainerFromIndex(1));
        Assert.NotNull(line.Template);

        menu.Close();
        window.Close();
    }

    private static Control Part(Control item, string name) =>
        item.GetVisualDescendants().OfType<Control>().First(child => child.Name == name);

    /// <summary>Меню, открытое над окном: до показа частей шаблона у пунктов нет.</summary>
    private static (ContextMenu Menu, Window Window) Shown(params AxMenuItem[] items)
    {
        var anchor = new Border { Width = 200, Height = 40 };
        var menu = new ContextMenu { ItemsSource = items };

        var window = new Window
        {
            Width = 400,
            Height = 300,
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = anchor,
        };

        window.Show();
        window.UpdateLayout();

        anchor.ContextMenu = menu;
        menu.Open(anchor);
        window.UpdateLayout();

        return (menu, window);
    }
}
