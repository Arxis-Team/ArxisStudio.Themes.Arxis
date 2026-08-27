using ArxisStudio.Controls;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Крестик очистки поля поиска.
/// </summary>
/// <remarks>
/// Крестик — единственный способ отменить поиск, не выделяя набранное. Он
/// появляется от текста и вместе с текстом исчезает: пустое поле с крестиком
/// предлагает стереть то, чего нет.
///
/// Кнопка живёт в <c>InnerRightContent</c>, а это своя область имён, и найти
/// её через <c>OnApplyTemplate</c> нельзя — контрол слушает нажатие по
/// маршруту события. Тест проверяет именно это: нажатие настоящей кнопкой, а
/// не вызов метода.
/// </remarks>
public class SearchFieldTests
{
    [AvaloniaFact]
    public void Empty_field_shows_no_clear()
    {
        var (field, window) = Shown(text: null);

        Assert.False(Clear(field).IsVisible, "пустое поле показывает крестик");

        window.Close();
    }

    [AvaloniaFact]
    public void Field_with_text_shows_the_clear()
    {
        var (field, window) = Shown("send");

        Assert.True(Clear(field).IsVisible, "поле с текстом не показывает крестик");

        window.Close();
    }

    [AvaloniaFact]
    public void Clear_wipes_the_text_and_takes_itself_away()
    {
        var (field, window) = Shown("send");
        var clear = Clear(field);

        clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent) { Source = clear });
        window.UpdateLayout();

        Assert.True(string.IsNullOrEmpty(field.Text), $"текст остался: {field.Text}");
        Assert.False(clear.IsVisible, "крестик остался после очистки");

        // Курсор остаётся в поле: стёрли, чтобы набрать другое.
        Assert.True(field.IsFocused, "поле потеряло фокус после очистки");

        window.Close();
    }

    private static (AxSearchField Field, Window Window) Shown(string? text)
    {
        var field = new AxSearchField { Text = text };
        var window = new Window { Content = field };

        window.Show();
        window.UpdateLayout();

        return (field, window);
    }

    private static Button Clear(AxSearchField field)
    {
        var clear = field.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(button => button.Name == "PART_Clear");

        Assert.True(clear is not null, "в теме поля поиска нет крестика очистки");

        return clear!;
    }
}
