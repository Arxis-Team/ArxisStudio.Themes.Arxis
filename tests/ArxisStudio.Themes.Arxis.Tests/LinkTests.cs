using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Ссылка: её нажимают мышью.
/// </summary>
/// <remarks>
/// Подпись ссылки лежала внутри кольца фокуса, а кольцо мышь не ловит — и вместе с ним не ловила
/// подпись: щелчок уходил в то, что под ссылкой. Клавишей ссылка нажималась, и тесты кольца,
/// гонявшие её Tab, этого не видели. Нашлось в окне настроек студии, на «Сбросить» в шапке
/// страницы, — первой ссылке, которую там поставили.
/// </remarks>
public class LinkTests
{
    /// <summary>Щелчок по подписи нажимает ссылку.</summary>
    [AvaloniaFact]
    public void A_click_on_the_label_presses_the_link()
    {
        var (link, window, pressed) = Shown();

        Click(window, link, new Point(link.Bounds.Width / 2, link.Bounds.Height / 2));

        Assert.Equal(1, pressed());

        window.Close();
    }

    /// <summary>
    /// Ссылка ловит мышь всей строкой, а не одними буквами.
    /// </summary>
    /// <remarks>
    /// Угол строки лежит мимо букв: подложка ссылки прозрачна, но есть, и промах на полпикселя
    /// мимо штриха остаётся щелчком по ссылке.
    /// </remarks>
    [AvaloniaFact]
    public void A_click_beside_the_letters_still_presses_the_link()
    {
        var (link, window, pressed) = Shown();

        Click(window, link, new Point(1, link.Bounds.Height - 1));

        Assert.Equal(1, pressed());

        window.Close();
    }

    private static void Click(Window window, Control control, Point inside)
    {
        var at = control.TranslatePoint(inside, window)!.Value;

        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        window.UpdateLayout();
    }

    private static (AxLink Link, Window Window, Func<int> Pressed) Shown()
    {
        var link = new AxLink { Content = "Сбросить", VerticalAlignment = VerticalAlignment.Center };
        var count = 0;

        link.Click += (_, _) => count++;

        // Подложка окна ловит мышь: щелчок, не доставшийся ссылке, уходит в неё, а не в пустоту.
        var window = new Window
        {
            Width = 240,
            Height = 80,
            Content = new Border { Background = Avalonia.Media.Brushes.Transparent, Child = link },
        };

        window.Show();
        window.UpdateLayout();

        return (link, window, () => count);
    }
}
