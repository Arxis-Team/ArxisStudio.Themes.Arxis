using ArxisStudio.Controls;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Поле только для чтения — значение, которое видно и выделяется, а не поле, в которое пишут.
/// </summary>
/// <remarks>
/// Рамка зовёт поставить курсор и ждать, что набранное ляжет, а выключенный вид гасит текст, который
/// человек пришёл прочесть. Поэтому у такого поля нет ни рамки, ни подложки, а текст остаётся основным.
/// </remarks>
public class ReadOnlyFieldTests
{
    [AvaloniaFact]
    public void A_read_only_field_has_no_frame_and_keeps_its_text_primary()
    {
        var (field, window) = Shown(readOnly: true);
        var frame = Frame(field);

        Assert.Equal(Colors.Transparent, Color(frame.Background));
        Assert.Equal(Colors.Transparent, Color(frame.BorderBrush));
        Assert.Equal(Color(Brush("AxTextPrimaryBrush")), Color(field.Foreground));

        window.Close();

        var (editable, second) = Shown(readOnly: false);

        Assert.NotEqual(Colors.Transparent, Color(Frame(editable).BorderBrush));

        second.Close();
    }

    /// <summary>С клавиатуры кольцо есть — каретка должна быть видна, — а щелчок мышью его не зажигает.</summary>
    [AvaloniaFact]
    public void The_ring_shows_for_the_keyboard_and_not_for_a_click()
    {
        var (field, window) = Shown(readOnly: true);
        var ring = field.GetVisualDescendants().OfType<Control>().Single(part => part.Name == "PART_FocusRing");

        field.Focus(NavigationMethod.Pointer);
        window.UpdateLayout();
        Assert.False(ring.IsVisible, "щелчок по полю только для чтения зажёг кольцо");

        Assert.True(Beside(window).Focus(), "сосед не взял каретку");
        field.Focus(NavigationMethod.Tab);
        window.UpdateLayout();
        Assert.True(ring.IsVisible, "с клавиатуры у поля только для чтения нет кольца");

        window.Close();
    }

    private static (AxTextBox Field, Window Window) Shown(bool readOnly)
    {
        var field = new AxTextBox { Text = "Button", IsReadOnly = readOnly };
        var window = new Window
        {
            Width = 300,
            Height = 100,
            Content = new StackPanel { Children = { field, new AxButton { Content = "beside" } } },
        };

        window.Show();
        window.UpdateLayout();

        return (field, window);
    }

    private static AxButton Beside(Window window) =>
        window.GetVisualDescendants().OfType<AxButton>().First(button => Equals(button.Content, "beside"));

    private static Border Frame(AxTextBox field) =>
        field.GetVisualDescendants().OfType<Border>().Single(part => part.Name == "PART_BorderElement");

    private static IBrush Brush(string key)
    {
        var application = Avalonia.Application.Current!;

        Assert.True(application.TryFindResource(key, application.ActualThemeVariant, out var value), $"в теме нет {key}");

        return Assert.IsAssignableFrom<IBrush>(value);
    }

    private static Color Color(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
}
