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

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;
}
