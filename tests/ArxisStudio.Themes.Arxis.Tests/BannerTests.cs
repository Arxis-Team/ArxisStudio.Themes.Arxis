using ArxisStudio.Controls;
using ArxisStudio.Icons;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Баннер против карточки проекта: размеры, крестик и цвет действия.
/// </summary>
/// <remarks>
/// Карточка даёт полосу по содержимому — отступ 8 сверху и снизу, 12 по бокам,
/// радиус 8, — и крестик у баннера любой строгости, даже без действий:
/// сообщение о состоянии перестаёт быть нужным раньше, чем состояние меняется.
///
/// Цель нажатия крестика — 24, меньше раздел 12 не разрешает, но распирать
/// полосу ей нельзя: отрицательные отступы держат высоту по содержимому.
/// </remarks>
public class BannerTests
{
    [AvaloniaFact]
    public void Banner_keeps_the_geometry_of_the_design_project()
    {
        var (banner, window) = Shown();

        Assert.Equal(new Avalonia.Thickness(12, 8), banner.Padding);
        Assert.Equal(new Avalonia.CornerRadius(8), banner.CornerRadius);
        Assert.Equal(new Avalonia.Thickness(1), banner.BorderThickness);

        // Полоса по содержимому: строка 16, отступ 8 сверху и снизу, рамка в
        // пиксель — всего 34. Ни цель нажатия крестика в 24, ни кольцо фокуса
        // ссылки распирать её не должны.
        Assert.Equal(34d, banner.Bounds.Height);

        window.Close();
    }

    /// <summary>Высота не зависит от того, есть ли действие.</summary>
    /// <remarks>
    /// Ссылка выходила на два пикселя выше строки текста — её кольцо фокуса
    /// снимало отрицательным отступом меньше, чем добавляло отступом и рамкой,
    /// — и баннер с действием оказывался выше баннера без него.
    /// </remarks>
    [AvaloniaFact]
    public void Action_does_not_make_the_banner_taller()
    {
        var (plain, first) = Shown();
        var plainHeight = plain.Bounds.Height;
        first.Close();

        var (withAction, second) = Shown(actions: new AxLink { Content = "Обновить" });

        Assert.Equal(plainHeight, withAction.Bounds.Height);

        second.Close();
    }

    /// <summary>Значок стоит по центру строки, а не по верху.</summary>
    [AvaloniaFact]
    public void Icon_sits_in_the_middle_of_the_row()
    {
        var (banner, window) = Shown();

        var icon = banner.GetVisualDescendants().OfType<AxIcon>().First();
        var row = icon.Parent as Control;

        Assert.Equal((row!.Bounds.Height - icon.Bounds.Height) / 2, icon.Bounds.Y);

        window.Close();
    }

    /// <summary>Крестик есть у баннера любой строгости, в том числе без действий.</summary>
    [AvaloniaTheory]
    [InlineData(AxBannerSeverity.Information)]
    [InlineData(AxBannerSeverity.Success)]
    [InlineData(AxBannerSeverity.Warning)]
    [InlineData(AxBannerSeverity.Error)]
    public void Every_banner_can_be_closed(AxBannerSeverity severity)
    {
        var (banner, window) = Shown(severity, actions: null);

        Assert.True(Close(banner).IsVisible, $"{severity}: крестика нет");

        window.Close();
    }

    [AvaloniaFact]
    public void Closing_hides_the_banner_and_says_so()
    {
        var (banner, window) = Shown();
        var told = false;

        banner.Closed += (_, _) => told = true;

        var close = Close(banner);
        close.RaiseEvent(new RoutedEventArgs(Button.ClickEvent) { Source = close });
        window.UpdateLayout();

        Assert.False(banner.IsVisible, "баннер остался на месте");
        Assert.True(told, "о закрытии никто не узнал");

        window.Close();
    }

    /// <summary>
    /// Действие на плашке светится AxLinkOn — правило раздела 6.
    /// </summary>
    [AvaloniaFact]
    public void Action_on_the_plate_takes_the_token_made_for_it()
    {
        var link = new AxLink { Content = "Обновить" };
        var (banner, window) = Shown(actions: link);

        Assert.True(window.TryFindResource("AxLinkOnColor", window.ActualThemeVariant, out var expected));
        Assert.Equal((Color)expected!, (link.Foreground as ISolidColorBrush)?.Color);

        window.Close();
    }

    private static (AxBanner Banner, Window Window) Shown(
        AxBannerSeverity severity = AxBannerSeverity.Information, object? actions = null)
    {
        var banner = new AxBanner
        {
            Severity = severity,
            Content = "Доступно обновление Arxis SDK 1.4.2",
            Actions = actions,
        };

        // В колонку, как в галерее: положенный прямо в окно баннер растянется
        // на всю его высоту, и мерить будет нечего.
        var window = new Window { Width = 600, Content = new StackPanel { Children = { banner } } };

        window.Show();
        window.UpdateLayout();

        return (banner, window);
    }

    private static Button Close(AxBanner banner)
    {
        var close = banner.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(button => button.Name == "PART_Close");

        Assert.True(close is not null, "в теме баннера нет крестика");

        return close!;
    }
}
