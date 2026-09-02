using ArxisStudio.Controls;
using ArxisStudio.Icons;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Диалоги: подтверждение с шапкой и алерт со значком.
/// </summary>
/// <remarks>
/// Карточка «Диалоги» показывает два вида. У подтверждения шапка с заголовком
/// и крестиком, под ней текст, внизу полоса кнопок за линией. У алерта шапки
/// нет вовсе: значок стоит слева, заголовок и текст — колонкой рядом с ним, и
/// уйти оттуда можно только кнопкой — решение обязательно.
///
/// Вид переключает состояние :alert, которое поднимает сам диалог: селектора
/// «свойство не пусто» в Avalonia нет, а по состоянию тема правит и шапку, и
/// отбивку тела разом.
/// </remarks>
public class DialogTests
{
    /// <summary>Карточка диалога: радиус плавающей поверхности и её тень.</summary>
    [AvaloniaFact]
    public void Dialog_is_a_floating_card()
    {
        var (dialog, window) = Shown(alert: false);

        var card = Root(dialog);

        Assert.Equal(new CornerRadius(10), card.CornerRadius);
        Assert.NotEqual(default, card.BoxShadow);

        Close(dialog, window);
    }

    /// <summary>Шапка: заголовок усиленным начертанием крупным кеглем.</summary>
    [AvaloniaFact]
    public void Header_carries_the_title()
    {
        var (dialog, window) = Shown(alert: false);

        var header = (DockPanel)Part(dialog, "PART_Header");
        var title = header.GetVisualDescendants().OfType<TextBlock>().First();

        Assert.True(header.IsVisible);
        Assert.Equal(new Thickness(16, 12), header.Margin);
        Assert.Equal(FontWeight.SemiBold, title.FontWeight);
        Assert.Equal(14d, title.FontSize);

        Close(dialog, window);
    }

    /// <summary>Полоса кнопок отбита линией разделителя.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Buttons_sit_behind_a_rule(string variant)
    {
        var (dialog, window) = Shown(alert: false, variant);

        var footer = (Border)Part(dialog, "PART_Footer");

        Assert.Equal(new Thickness(0, 1, 0, 0), footer.BorderThickness);
        Assert.Equal(Resource(window, "AxBrdColor", variant), Colour(footer.BorderBrush));
        Assert.Equal(new Thickness(16, 12), footer.Padding);

        Close(dialog, window);
    }

    /// <summary>Значок отменяет шапку и переносит заголовок в тело.</summary>
    [AvaloniaFact]
    public void Alert_drops_the_header_for_the_sign()
    {
        var (dialog, window) = Shown(alert: true);

        Assert.Contains(":alert", dialog.Classes);
        Assert.False(Part(dialog, "PART_Header").IsVisible);
        Assert.True(Part(dialog, "PART_AlertIcon").IsVisible);
        Assert.True(Part(dialog, "PART_AlertTitle").IsVisible);

        Close(dialog, window);
    }

    /// <summary>
    /// Без шапки тело берёт верхнюю отбивку на себя.
    /// </summary>
    /// <remarks>
    /// У подтверждения её давала шапка; у алерта над значком иначе остался бы
    /// ноль, и он прилип бы к краю карточки.
    /// </remarks>
    [AvaloniaFact]
    public void Alert_body_takes_the_padding_the_header_used_to_give()
    {
        var (plain, first) = Shown(alert: false);
        Assert.Equal(new Thickness(16, 0, 16, 16), ((DockPanel)Part(plain, "PART_Body")).Margin);
        Close(plain, first);

        var (alert, second) = Shown(alert: true);
        Assert.Equal(new Thickness(16), ((DockPanel)Part(alert, "PART_Body")).Margin);
        Close(alert, second);
    }

    /// <summary>Обычный диалог значка не показывает и заголовок не двоит.</summary>
    [AvaloniaFact]
    public void Plain_dialog_shows_the_title_once()
    {
        var (dialog, window) = Shown(alert: false);

        Assert.False(Part(dialog, "PART_AlertIcon").IsVisible);
        Assert.False(Part(dialog, "PART_AlertTitle").IsVisible);

        Close(dialog, window);
    }

    private static (AxDialog Dialog, Window Owner) Shown(bool alert, string variant = "Dark")
    {
        var owner = new Window
        {
            Width = 600,
            Height = 400,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
        };

        owner.Show();

        var dialog = new AxDialog
        {
            Width = alert ? 360 : 400,
            Title = "Удалить форму LoginView.axaml?",
            Content = new TextBlock { Text = "Действие нельзя отменить." },
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
        };

        if (alert)
            dialog.AlertIcon = new AxIcon { Width = 28, Height = 28, Data = AxIcons.WarningTriangle };

        dialog.Show(owner);
        dialog.UpdateLayout();

        return (dialog, owner);
    }

    private static void Close(AxDialog dialog, Window owner)
    {
        dialog.Close();
        owner.Close();
    }

    /// <summary>Карточка диалога: она несёт радиус и тень.</summary>
    private static Border Root(AxDialog dialog) =>
        dialog.GetVisualDescendants().OfType<Border>().First(b => b.BoxShadow.Count > 0);

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
