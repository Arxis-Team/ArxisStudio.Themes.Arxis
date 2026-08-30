using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Вкладки: две разновидности, разведённые проектом по виду.
/// </summary>
/// <remarks>
/// Вкладка документа и вкладка панели различаются не только высотой. У панели
/// нет ни своего фона, ни рамки — выбранной её делают цвет, полоса снизу
/// толщиной 3 и усиленное начертание. У документа для этого есть фон и полоса
/// 2, и веса проект ей не даёт: жирное имя файла в ряду имён читается как
/// другой уровень, а не как выбор.
///
/// Наведение у обеих — AxBg3. Раздел 3 закрепляет за этим токеном роль
/// наведения, раздел 10 повторяет её селектором; AxBg2 сходился с витриной в
/// светлой теме случайно, а в тёмной вкладка под курсором была темнее, чем
/// нужно.
/// </remarks>
public class TabStripTests
{
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Hovered_tab_takes_the_hover_token(string variant)
    {
        foreach (var compact in new[] { false, true })
        {
            var (tab, window) = Shown(compact, variant);

            ((IPseudoClasses)tab.Classes).Set(":pointerover", true);
            window.UpdateLayout();

            Assert.Equal(
                Resource(window, "AxBg3Color", variant),
                Colour(Part(tab, "PART_Root").GetValue(Border.BackgroundProperty)));

            window.Close();
        }
    }

    /// <summary>Полоса выбора: 3 у вкладки панели, 2 у вкладки документа.</summary>
    [AvaloniaTheory]
    [InlineData(false, 2d)]
    [InlineData(true, 3d)]
    public void Selected_tab_marks_itself_with_the_bar_of_its_kind(bool compact, double thickness)
    {
        var (tab, window) = Shown(compact, "Dark");

        ((IPseudoClasses)tab.Classes).Set(":selected", true);
        window.UpdateLayout();

        var marker = Part(tab, "PART_ActiveMarker");

        Assert.True(marker.IsVisible, "полосы выбора не видно");
        Assert.Equal(thickness, marker.Bounds.Height);
        Assert.Equal(
            Resource(window, "AxAccColor", "Dark"),
            Colour(marker.GetValue(Border.BackgroundProperty)));

        window.Close();
    }

    /// <summary>Начертание усиливает только вкладку панели.</summary>
    [AvaloniaTheory]
    [InlineData(false, "Normal")]
    [InlineData(true, "Medium")]
    public void Weight_belongs_to_the_panel_tab_alone(bool compact, string weight)
    {
        var (tab, window) = Shown(compact, "Dark");

        ((IPseudoClasses)tab.Classes).Set(":selected", true);
        window.UpdateLayout();

        Assert.Equal(weight, tab.FontWeight.ToString());

        window.Close();
    }

    /// <summary>Высота: 34 у вкладки документа, 32 у вкладки панели.</summary>
    [AvaloniaTheory]
    [InlineData(false, 34d)]
    [InlineData(true, 32d)]
    public void Height_matches_the_design_project(bool compact, double height)
    {
        var (tab, window) = Shown(compact, "Dark");

        Assert.Equal(height, tab.Bounds.Height);
        Assert.Equal(new Avalonia.Thickness(10, 0), tab.Padding);

        window.Close();
    }

    /// <summary>
    /// Крестик закрытия — площадка, а не линия.
    /// </summary>
    /// <remarks>
    /// Попадать мышью по контуру в полтора пикселя человек не должен, поэтому
    /// мышь ловит вся площадка четырнадцать на четырнадцать. Без прозрачного
    /// фона она прозрачна и для попадания: нажатие ушло бы во вкладку, и та бы
    /// просто выбралась.
    /// </remarks>
    [AvaloniaFact]
    public void The_close_cross_is_an_area_and_not_a_line()
    {
        var (tab, window) = Shown(compact: false, "Dark");
        var close = Part(tab, "PART_Close");

        Assert.Equal(new Size(14, 14), close.Bounds.Size);
        Assert.NotNull(close.GetValue(Panel.BackgroundProperty));

        window.Close();
    }

    /// <summary>Щелчок по крестику просит закрыть вкладку.</summary>
    /// <remarks>
    /// Просит, а не закрывает: у документа могут быть несохранённые правки, и
    /// спрашивать о них — дело хозяина вкладки, а не темы.
    /// </remarks>
    [AvaloniaFact]
    public void Clicking_the_cross_asks_to_close()
    {
        var (tab, window) = Shown(compact: false, "Dark");
        var close = Part(tab, "PART_Close");

        var asked = 0;
        tab.CloseRequested += (_, _) => asked++;

        var at = close.TranslatePoint(new Point(7, 7), window);

        Assert.NotNull(at);

        window.MouseMove(at.Value);
        window.MouseDown(at.Value, MouseButton.Left);
        window.MouseUp(at.Value, MouseButton.Left);
        window.UpdateLayout();

        Assert.Equal(1, asked);

        window.Close();
    }

    /// <summary>
    /// Правая кнопка вкладку не закрывает.
    /// </summary>
    /// <remarks>
    /// За правой кнопкой человек идёт за меню, а не за закрытием. Закройся
    /// вкладка от неё — он потерял бы документ там, где ждал список действий.
    /// </remarks>
    [AvaloniaFact]
    public void The_right_button_does_not_close()
    {
        var (tab, window) = Shown(compact: false, "Dark");
        var close = Part(tab, "PART_Close");

        var asked = 0;
        tab.CloseRequested += (_, _) => asked++;

        var at = close.TranslatePoint(new Point(7, 7), window);

        Assert.NotNull(at);

        window.MouseMove(at.Value);
        window.MouseDown(at.Value, MouseButton.Right);
        window.MouseUp(at.Value, MouseButton.Right);
        window.UpdateLayout();

        Assert.Equal(0, asked);

        window.Close();
    }

    private static (AxTabItem Tab, Window Window) Shown(bool compact, string variant)
    {
        var tab = new AxTabItem { Content = "MainWindow.axaml", IsClosable = true };

        if (compact)
            tab.Classes.Add("compact");

        var window = new Window
        {
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = tab,
        };

        window.Show();
        window.UpdateLayout();

        return (tab, window);
    }

    private static Control Part(Control control, string name)
    {
        var part = control.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Name == name);

        Assert.True(part is not null, $"в шаблоне нет части {name}");
        return part!;
    }

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key, string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        Assert.True(window.TryFindResource(key, theme, out var value), key);

        return (Color)value!;
    }
}
