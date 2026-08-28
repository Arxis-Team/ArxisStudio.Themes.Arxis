using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;
using Path = Avalonia.Controls.Shapes.Path;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Флажок: три типа по пяти состояниям.
/// </summary>
/// <remarks>
/// Карточка «Checkbox» разводит Unchecked, Checked и Indeterminate по
/// состояниям Active, Hovered, Focused, Invalid и Disabled. Коробка везде одна:
/// 16 на 16 с малым радиусом — так её объявляет компонент AxCheckBox, и радиус
/// там 3, а не 4 из карточки.
///
/// Заливка отмеченного идёт на AxAcc, а не на AxAccStrong, как у залитых
/// кнопок: раздел 6 приёмки оставляет AxAcc флажкам — белому знаку на нём
/// довольно 3:1.
/// </remarks>
public class CheckBoxTests
{
    /// <summary>Коробка: 16 на 16 с малым радиусом и рамкой контрола.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Box_is_sixteen_with_the_small_radius(string variant)
    {
        var (box, _, window) = Shown(variant: variant);

        Assert.Equal(16d, box.Width);
        Assert.Equal(16d, box.Height);
        Assert.Equal(new CornerRadius(3), box.CornerRadius);
        Assert.Equal(Resource(window, "AxBrd2Color", variant), Colour(box.BorderBrush));
        Assert.Equal(Resource(window, "AxInpColor", variant), Colour(box.Background));

        window.Close();
    }

    /// <summary>Отмеченный: заливка акцентом, знак — цветом текста на акценте.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Checked_box_is_filled_with_the_accent(string variant)
    {
        var (box, check, window) = Shown(isChecked: true, variant: variant);

        Assert.True(check.IsVisible);
        Assert.Equal(Resource(window, "AxAccColor", variant), Colour(box.Background));
        Assert.Equal(Resource(window, "AxOnAccColor", variant), Colour(check.Stroke));

        window.Close();
    }

    /// <summary>Ошибка видна и по рамке, и по заливке — без текста рядом.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Invalid_box_speaks_by_itself(string variant)
    {
        var (box, _, window) = Shown(error: true, variant: variant);

        Assert.Equal(Resource(window, "AxOutlineErrorColor", variant), Colour(box.BorderBrush));
        Assert.Equal(Resource(window, "AxErrorBackgroundColor", variant), Colour(box.Background));

        window.Close();
    }

    /// <summary>
    /// Выключенный отмеченный гаснет знаком, а не только заливкой.
    /// </summary>
    /// <remarks>
    /// Белый знак оставался белым и на погашенной заливке — флажок читался как
    /// включённый, только побледневший.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Disabled_checked_box_dims_its_mark(string variant)
    {
        var (box, check, window) = Shown(isChecked: true, enabled: false, variant: variant);

        Assert.Equal(Resource(window, "AxBg3Color", variant), Colour(box.Background));
        Assert.Equal(Resource(window, "AxFgDisabledColor", variant), Colour(check.Stroke));

        window.Close();
    }

    /// <summary>Неотмеченный выключенный: фон выключенного поля.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Disabled_box_takes_the_disabled_field_fill(string variant)
    {
        var (box, _, window) = Shown(enabled: false, variant: variant);

        Assert.Equal(Resource(window, "AxInpDisabledColor", variant), Colour(box.Background));

        window.Close();
    }

    /// <summary>Третье состояние показывает черту, а не галочку.</summary>
    [AvaloniaFact]
    public void Indeterminate_box_shows_the_dash()
    {
        var box = new AxCheckBox { IsThreeState = true, IsChecked = null };
        var window = new Window { RequestedThemeVariant = ThemeVariant.Dark, Content = box };

        window.Show();
        window.UpdateLayout();

        Assert.True(Part<Path>(box, "PART_Dash").IsVisible);
        Assert.False(Part<Path>(box, "PART_Check").IsVisible);

        window.Close();
    }

    private static (Border Box, Path Mark, Window Window) Shown(
        bool isChecked = false, bool error = false, bool enabled = true, string variant = "Dark")
    {
        var control = new AxCheckBox { IsChecked = isChecked, IsEnabled = enabled, Content = "Флажок" };

        if (error)
            control.Classes.Add("error");

        var window = new Window
        {
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = control,
        };

        window.Show();
        window.UpdateLayout();

        return (Part<Border>(control, "PART_Box"), Part<Path>(control, "PART_Check"), window);
    }

    private static T Part<T>(Control control, string name) where T : Control
    {
        var part = control.GetVisualDescendants().OfType<T>().FirstOrDefault(c => c.Name == name);

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
