using System.Globalization;
using ArxisStudio.Controls;
using ArxisStudio.Icons;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
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
