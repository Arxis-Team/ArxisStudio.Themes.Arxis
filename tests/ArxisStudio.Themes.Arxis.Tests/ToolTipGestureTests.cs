using System.Globalization;
using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Подсказка называет и жест: как зовут это действие с клавиатуры.
/// </summary>
/// <remarks>
/// Кнопка полосы несёт один значок, и узнать её сочетание было негде — подсказка называла
/// действие, а жест прятался в настройках. В Rider и в Visual Studio он стоит в той же подсказке.
/// <para>
/// К подсказке жест приходит наследованием: показывает её отдельное окно, и своего жеста у того
/// окна нет, как нет и своего контекста данных. Поэтому проверка смотрит не на привязку, а на
/// показанную чипу: наследование через окно попапа — как раз то, что может сломаться молча.
/// </para>
/// </remarks>
public class ToolTipGestureTests
{
    /// <summary>Жест объявлен кнопке — чипа в подсказке его называет.</summary>
    [AvaloniaFact]
    public void The_tip_names_the_gesture_of_the_control_under_the_pointer()
    {
        var gesture = new KeyGesture(Key.B, KeyModifiers.Control | KeyModifiers.Shift);
        var button = new AxButton { Content = "Собрать" };

        ToolTip.SetTip(button, "Собрать решение");
        AxToolTip.SetGesture(button, gesture);

        ToolTip.SetShouldUseOverlayLayer(button, true);

        var window = Shown(button);

        ToolTip.SetIsOpen(button, true);
        window.UpdateLayout();

        var chip = Chip(window);

        Assert.NotNull(chip);
        Assert.True(chip!.IsVisible, "чипы с жестом не видно");
        Assert.Equal(gesture.ToString("p", CultureInfo.CurrentCulture), chip.Content);

        ToolTip.SetIsOpen(button, false);
        window.Close();
    }

    /// <summary>Жеста нет — нет и чипы: пустая рамка рядом с текстом ничего не значит.</summary>
    [AvaloniaFact]
    public void A_tip_without_a_gesture_shows_no_chip()
    {
        var button = new AxButton { Content = "Собрать" };

        ToolTip.SetTip(button, "Собрать решение");

        ToolTip.SetShouldUseOverlayLayer(button, true);

        var window = Shown(button);

        ToolTip.SetIsOpen(button, true);
        window.UpdateLayout();

        var chip = Chip(window);

        Assert.True(chip is null || !chip.IsVisible, "чипа с жестом у подсказки без жеста");

        ToolTip.SetIsOpen(button, false);
        window.Close();
    }

    /// <summary>Чипа жеста в открытой подсказке — она там одна.</summary>
    /// <remarks>
    /// Подсказка живёт в слое оверлеев окна, а не своим окном: так её видно из дерева, и так же
    /// её показывает сама Avalonia там, где окон попапа нет вовсе. Путь наследования от этого не
    /// меняется — жест приходит к подсказке от того, над кем она стоит.
    /// </remarks>
    private static AxChip? Chip(Window window) =>
        window.GetVisualDescendants().OfType<AxChip>().FirstOrDefault(chip => chip.Name == "PART_Gesture");

    private static Window Shown(Control content)
    {
        var window = new Window
        {
            Width = 400,
            Height = 200,
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = content,
        };

        window.Show();
        window.UpdateLayout();

        return window;
    }
}
