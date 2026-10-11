using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Пипетка: застывший снимок экрана, лупа у курсора и пиксель, взятый щелчком или с клавиатуры.
/// </summary>
/// <remarks>Экран здесь — поддельный снимок (<see cref="Eyedroppers.Screen"/>): снять настоящий безголовому прогону нечем.</remarks>
public class EyedropperTests
{
    /// <summary>Щелчок берёт пиксель под курсором; лупа показывает его код.</summary>
    [AvaloniaFact]
    public void A_click_takes_the_pixel_under_the_cursor_and_the_loupe_names_it()
    {
        using var screen = Eyedroppers.Screen(200, 150, (120, 60, Colors.DodgerBlue));
        var (picking, window) = Open();
        var overlay = Assert.Single(AxEyedropper.Overlays);

        overlay.MouseMove(new Point(120.5, 60.5));
        Pump();

        Assert.Equal(new PixelPoint(120, 60), overlay.Pixel);
        Assert.Equal(AxColorField.Format(Colors.DodgerBlue, alpha: false), overlay.Loupe.Code);

        overlay.MouseDown(new Point(120.5, 60.5), MouseButton.Left);
        Pump();

        Assert.Equal(Colors.DodgerBlue, picking.Result);
        Assert.False(AxEyedropper.IsOpen, "пипетка осталась открытой");

        window.Close();
    }

    /// <summary>С клавиатуры: стрелки двигают точку на пиксель, с Shift — на десять, Enter берёт.</summary>
    [AvaloniaFact]
    public void Arrows_move_the_point_and_enter_takes_it()
    {
        using var screen = Eyedroppers.Screen(200, 150, (111, 61, Colors.Gold));
        var (picking, window) = Open();
        var overlay = Assert.Single(AxEyedropper.Overlays);

        overlay.MouseMove(new Point(100.5, 60.5));
        Pump();
        overlay.KeyPress(Key.Right, RawInputModifiers.Shift, PhysicalKey.None, string.Empty);
        overlay.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.None, string.Empty);
        overlay.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.None, string.Empty);

        Assert.Equal(new PixelPoint(111, 61), overlay.Pixel);

        overlay.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, string.Empty);
        Pump();

        Assert.Equal(Colors.Gold, picking.Result);

        window.Close();
    }

    /// <summary>Правая кнопка — отмена, как Esc.</summary>
    [AvaloniaFact]
    public void The_right_button_cancels()
    {
        using var screen = Eyedroppers.Screen(200, 150);
        var (picking, window) = Open();
        var overlay = Assert.Single(AxEyedropper.Overlays);

        overlay.MouseDown(new Point(20, 20), MouseButton.Right);
        Pump();

        Assert.True(picking.IsCompleted, "правая кнопка не закрыла пипетку");
        Assert.Null(picking.Result);

        window.Close();
    }

    /// <summary>
    /// Лупа стоит у курсора, а у правого и нижнего края — по другую сторону от него: за экран она не выходит.
    /// </summary>
    [AvaloniaFact]
    public void The_loupe_stays_on_the_screen_at_its_edge()
    {
        using var screen = Eyedroppers.Screen(300, 200);
        var (picking, window) = Open();
        var overlay = Assert.Single(AxEyedropper.Overlays);

        overlay.MouseMove(new Point(30.5, 30.5));
        Pump();

        Assert.True(Canvas.GetLeft(overlay.Loupe) > 30.5, "в середине экрана лупа не справа от курсора");

        overlay.MouseMove(new Point(298.5, 198.5));
        Pump();

        var left = Canvas.GetLeft(overlay.Loupe);
        var top = Canvas.GetTop(overlay.Loupe);

        Assert.True(left + overlay.Loupe.Bounds.Width <= 300 + 0.5, $"лупа ушла за правый край: {left} + {overlay.Loupe.Bounds.Width}");
        Assert.True(top + overlay.Loupe.Bounds.Height <= 200 + 0.5, $"лупа ушла за нижний край: {top} + {overlay.Loupe.Bounds.Height}");
        Assert.True(left < 298.5, "у правого края лупа осталась справа от курсора");

        overlay.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, string.Empty);
        Pump();

        Assert.Null(picking.Result);

        window.Close();
    }

    /// <summary>Пипетка одна: второй вызов, пока открыта первая, сразу отвечает «ничего».</summary>
    [AvaloniaFact]
    public void A_second_eyedropper_while_one_is_open_answers_nothing()
    {
        using var screen = Eyedroppers.Screen(200, 150);
        var (picking, window) = Open();
        var second = AxEyedropper.PickAsync(window);

        Assert.True(second.IsCompleted, "вторая пипетка ждёт");
        Assert.Null(second.Result);

        Assert.Single(AxEyedropper.Overlays).KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, string.Empty);
        Pump();

        Assert.True(picking.IsCompleted);

        window.Close();
    }

    private static (Task<Color?> Picking, Window Window) Open()
    {
        var window = new Window { Width = 300, Height = 200 };

        window.Show();

        var picking = AxEyedropper.PickAsync(window);

        Pump();

        return (picking, window);
    }

    private static void Pump()
    {
        for (var at = 0; at < 5; at++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }
}

/// <summary>Поддельный экран для пипетки: снимок нужного размера с отмеченными пикселями.</summary>
internal static class Eyedroppers
{
    /// <summary>Подменяет снимок экрана; отпускание возвращает настоящий.</summary>
    /// <param name="width">Ширина в пикселях.</param>
    /// <param name="height">Высота в пикселях.</param>
    /// <param name="dots">Пиксели своего цвета; остальные — тёмно-серые.</param>
    public static IDisposable Screen(int width, int height, params (int X, int Y, Color Color)[] dots)
    {
        var pixels = new byte[width * height * 4];

        for (var at = 0; at < pixels.Length; at += 4)
        {
            pixels[at] = 0x30;
            pixels[at + 1] = 0x30;
            pixels[at + 2] = 0x30;
            pixels[at + 3] = 0xFF;
        }

        foreach (var (x, y, color) in dots)
        {
            var at = (y * width + x) * 4;

            pixels[at] = color.B;
            pixels[at + 1] = color.G;
            pixels[at + 2] = color.R;
        }

        AxEyedropper.Capture = _ => [new AxScreenShot(new PixelRect(0, 0, width, height), 1, (byte[])pixels.Clone())];

        return new Restore();
    }

    private sealed class Restore : IDisposable
    {
        public void Dispose() => AxEyedropper.Capture = null;
    }
}
