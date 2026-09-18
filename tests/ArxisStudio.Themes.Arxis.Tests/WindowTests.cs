using ArxisStudio.Controls;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Окно студии: своя полоса заголовка при системной рамке.
/// </summary>
/// <remarks>
/// Рамку студия оставляет системе — она даёт тень, привязку к краям экрана и
/// изменение размера, — а полосу заголовка рисует сама. Цвет рамки окно просит
/// у системы само; проверить это в отрыве от Windows нечем, зато видно, что
/// окно осталось окном и тему носит общую.
/// </remarks>
public class WindowTests
{
    /// <summary>Окно студии оставляет системную рамку и снимает полосу заголовка.</summary>
    [AvaloniaFact]
    public void A_studio_window_keeps_the_border_and_drops_the_system_bar()
    {
        Assert.Equal(WindowDecorations.BorderOnly, new AxWindow().WindowDecorations);
    }

    /// <summary>
    /// Своей темы у окна студии нет — оно носит тему обычного окна.
    /// </summary>
    /// <remarks>
    /// Ключ темы наследнику окна Avalonia отдаёт от <see cref="Window"/>, и
    /// собственный <c>StyleKeyOverride</c> здесь не нужен. Проверка стоит
    /// против соблазна его завести: окно с ключом <c>AxWindow</c> осталось бы
    /// вовсе без темы — с прозрачным фоном вместо студийного.
    /// </remarks>
    [AvaloniaFact]
    public void A_studio_window_wears_the_theme_of_a_plain_window()
    {
        var plain = new Window { RequestedThemeVariant = ThemeVariant.Dark };
        var studio = new AxWindow { RequestedThemeVariant = ThemeVariant.Dark };

        plain.Show();
        studio.Show();
        plain.UpdateLayout();
        studio.UpdateLayout();

        Assert.NotNull(Colour(studio.Background));
        Assert.Equal(Colour(plain.Background), Colour(studio.Background));

        studio.Close();
        plain.Close();
    }

    /// <summary>
    /// Окно не открывается больше рабочей области экрана — ни размером, ни наименьшим размером.
    /// </summary>
    /// <remarks>
    /// Числа окна заданы под обычный монитор, а ноутбук 1920 × 1080 при 150 % — это 1280 × 720 точек
    /// без панели задач. Окно крупнее экрана вставало по центру с заголовком выше верхнего края, и
    /// сдвинуть его было не за что, а наименьший размер крупнее экрана не давал его и сжать.
    /// </remarks>
    [AvaloniaFact]
    public void A_window_larger_than_its_screen_opens_within_the_working_area()
    {
        var window = new AxWindow { Width = 100000, Height = 100000, MinWidth = 90000, MinHeight = 90000 };

        window.Show();

        var screen = window.Screens?.Primary;

        Assert.NotNull(screen);

        var area = screen.WorkingArea.ToRect(screen.Scaling).Size;

        Assert.True(window.Width <= area.Width && window.Height <= area.Height, $"окно {window.Width} × {window.Height} больше экрана {area}");
        Assert.True(window.MinWidth <= area.Width && window.MinHeight <= area.Height, $"наименьший размер {window.MinWidth} × {window.MinHeight} больше экрана {area}");

        window.Close();
    }

    /// <summary>
    /// Окно, которое на экран влезает, остаётся своего размера, и наименьший размер у него — от темы,
    /// а не прибитый.
    /// </summary>
    [AvaloniaFact]
    public void A_window_that_fits_keeps_its_size_and_leaves_its_minimum_to_the_style()
    {
        var window = new AxWindow { Width = 400, Height = 300 };

        window.Show();

        Assert.Equal(400, window.Width);
        Assert.Equal(300, window.Height);
        Assert.False(window.IsSet(Avalonia.Layout.Layoutable.MinWidthProperty), "наименьший размер прибит местным значением");

        window.Close();
    }

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;
}
