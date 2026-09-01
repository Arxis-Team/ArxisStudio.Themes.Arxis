using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Тулбар: иконочная кнопка в пяти состояниях и полоса окна.
/// </summary>
/// <remarks>
/// Карточка «Toolbar» рисует иконочную кнопку 28 на 28, но компонент AxButton
/// объявляет её 24 на 24 — компонент над карточкой, и размер проверяет
/// <see cref="ControlGeometryTests"/>. От карточки здесь остаются состояния:
/// заливки наведения, нажатия и включённости и то, что глиф идёт за ними.
///
/// Глиф — отдельная проверка не для красоты: свой цвет AxIcon задаёт в
/// собственной теме, а тема сильнее наследования, и Foreground кнопки до глифа
/// сам по себе не доходит.
/// </remarks>
public class ToolBarTests
{
    /// <summary>Наведение и нажатие: заливка шага, глиф основным текстом.</summary>
    [AvaloniaTheory]
    [InlineData(":pointerover", "AxBg3Color", "Light")]
    [InlineData(":pointerover", "AxBg3Color", "Dark")]
    [InlineData(":pressed", "AxBg4Color", "Light")]
    [InlineData(":pressed", "AxBg4Color", "Dark")]
    public void Touched_tool_takes_the_step_of_its_state(string state, string key, string variant)
    {
        var (button, icon, window) = Shown(variant);

        ((IPseudoClasses)button.Classes).Set(state, true);
        window.UpdateLayout();

        Assert.Equal(Resource(window, key, variant), Colour(Plate(button).Background));
        Assert.Equal(Resource(window, "AxFgColor", variant), Colour(icon.Foreground));

        window.Close();
    }

    /// <summary>
    /// Включённый инструмент: заливка выделения, глиф акцентом.
    /// </summary>
    /// <remarks>
    /// Состояние записано псевдоклассом :selected по разделу 10 — своего
    /// свойства у кнопки под это нет: включённость инструмента знает
    /// приложение, а не контрол.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Selected_tool_is_filled_with_the_selection(string variant)
    {
        var (button, icon, window) = Shown(variant);

        ((IPseudoClasses)button.Classes).Set(":selected", true);
        window.UpdateLayout();

        Assert.Equal(Resource(window, "AxSelColor", variant), Colour(Plate(button).Background));
        Assert.Equal(Resource(window, "AxAccColor", variant), Colour(icon.Foreground));

        window.Close();
    }

    /// <summary>
    /// Выключенный инструмент гаснет глифом, а не плашкой.
    /// </summary>
    /// <remarks>
    /// Своей заливки у иконочной кнопки нет и в покое; фон выключенного поля
    /// остаётся кнопке с рамкой, у которой заливка есть всегда.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Disabled_tool_dims_its_glyph_and_keeps_no_plate(string variant)
    {
        var (button, icon, window) = Shown(variant);

        button.IsEnabled = false;
        window.UpdateLayout();

        Assert.Equal(0, Colour(Plate(button).Background)!.Value.A);
        Assert.Equal(Resource(window, "AxFgDisabledColor", variant), Colour(icon.Foreground));

        window.Close();
    }

    /// <summary>Свой цвет глифа переживает состояния кнопки.</summary>
    /// <remarks>
    /// Зелёный треугольник запуска в полосе окна остаётся зелёным и под
    /// курсором: значение, проставленное на месте, сильнее стиля.
    /// </remarks>
    [AvaloniaFact]
    public void Glyph_keeps_a_colour_set_on_the_spot()
    {
        var (button, icon, window) = Shown("Dark");

        icon.Foreground = Brushes.Red;
        ((IPseudoClasses)button.Classes).Set(":pointerover", true);
        window.UpdateLayout();

        Assert.Equal(Colors.Red, Colour(icon.Foreground));

        window.Close();
    }

    /// <summary>
    /// Полоса окна — 40 высотой, с линией снизу и без своего скругления.
    /// </summary>
    /// <remarks>
    /// Столько объявляет карточка «Toolbar» — единственная в проекте, где
    /// полоса показана целиком, с проектом, виджетом запуска и кнопками окна.
    /// Рамку карточка рисует вокруг всей полосы, но это её собственная рама
    /// образца: у настоящего окна полоса прижата к краям, и обводить её по
    /// периметру нечем.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Window_bar_is_forty_high(string variant)
    {
        var bar = new AxTitleBar { ShowWindowControls = false, Content = new TextBlock() };

        var window = new Window
        {
            Width = 520,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = bar,
        };

        window.Show();
        window.UpdateLayout();

        Assert.Equal(40d, bar.Bounds.Height);
        Assert.Equal(new Thickness(0, 0, 0, 1), bar.BorderThickness);
        Assert.Equal(Resource(window, "AxBg2Color", variant), Colour(bar.Background));
        Assert.Equal(Resource(window, "AxBrdColor", variant), Colour(bar.BorderBrush));

        window.Close();
    }

    /// <summary>
    /// Глифы кнопок окна — обычные иконки набора, а не мелкие.
    /// </summary>
    /// <remarks>
    /// Раздел 7 спецификации отводит иконке клетку 16×16 и обводку 1.2, а класс
    /// small с его двенадцатью заведён под мелкий шеврон в тесной строке.
    /// Кнопки окна носили его без причины — теснота там мнимая, кнопка 32×26, —
    /// и уменьшение шло дважды, клеткой и рисунком: квадрат в семь единиц из
    /// шестнадцати, сжатый до двенадцати, доезжал до экрана пятью пикселями с
    /// четвертью. Отсюда три проверки разом: клетка, обводка и то, что рисунок
    /// её заполняет.
    /// </remarks>
    [AvaloniaFact]
    public void Window_button_glyphs_are_ordinary_icons()
    {
        var controls = new AxWindowControls();

        var window = new Window
        {
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = controls,
        };

        window.Show();
        window.UpdateLayout();

        var size = Metric(window, "AxIconSize");
        var stroke = Metric(window, "AxIconStrokeThickness");
        var glyphs = controls.GetVisualDescendants().OfType<AxIcon>().ToList();

        Assert.Equal(4, glyphs.Count);

        foreach (var glyph in glyphs)
        {
            Assert.DoesNotContain("small", glyph.Classes);
            Assert.Equal(size, glyph.Width);
            Assert.Equal(size, glyph.Height);
            Assert.Equal(stroke, glyph.StrokeThickness);

            // Восстановить прячется под развернуть и площади не занимает, пока
            // окно не развёрнуто: мерить у него нечего, кроме объявленного.
            if (glyph.IsVisible)
                Assert.Equal(new Size(size, size), glyph.Bounds.Size);

            var drawn = glyph.Data!.Bounds;

            // Рисунок заполняет клетку: семь единиц из шестнадцати — столько
            // спецификация отводит самой мелкой из своих фигур, квадрату.
            Assert.True(
                Math.Max(drawn.Width, drawn.Height) >= 7,
                $"глиф занимает всего {Math.Max(drawn.Width, drawn.Height)} единиц из шестнадцати");
        }

        window.Close();
    }

    private static double Metric(Window window, string key)
    {
        Assert.True(window.TryFindResource(key, out var value), key);

        return (double)value!;
    }

    private static (AxButton Button, AxIcon Icon, Window Window) Shown(string variant)
    {
        var icon = new AxIcon { Data = AxIcons.Plus };
        var button = new AxButton { Classes = { "icon" }, Content = icon };

        var window = new Window
        {
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = button,
        };

        window.Show();
        window.UpdateLayout();

        return (button, icon, window);
    }

    /// <summary>Плашка кнопки: она несёт заливку состояния.</summary>
    private static ContentPresenter Plate(AxButton button) =>
        button.GetVisualDescendants().OfType<ContentPresenter>().First(c => c.Name == "PART_ContentPresenter");

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key, string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        Assert.True(window.TryFindResource(key, theme, out var value), key);

        return (Color)value!;
    }
}
