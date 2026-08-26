using ArxisStudio.Controls;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Выключенное состояние: раздел 10 спецификации против темы.
/// </summary>
/// <remarks>
/// Раздел 10 записывает пять состояний каждой карточки селекторами, и
/// <c>:disabled</c> среди них: текст уходит на AxFgDisabled, поле — на
/// AxInpDisabled. Пропустить это состояние легко и заметить трудно: строка
/// списка, которую нельзя выбрать, выглядит обычной, и человек жмёт на неё,
/// пока не поймёт, что она не отвечает.
///
/// Прозрачности здесь нет намеренно. Погасить контрол через Opacity выглядит
/// тем же приёмом, но даёт другой цвет: половина прозрачности зависит от того,
/// что лежит под контролом, и в панели он выглядел бы иначе, чем в диалоге.
/// </remarks>
public class DisabledStateTests
{
    /// <summary>
    /// Всё, что человек выбирает или нажимает, гаснет текстом.
    /// </summary>
    /// <remarks>
    /// Контейнеры строк перечислены поимённо: их выключают чаще всего — команда
    /// недоступна, файл занят, вкладка ещё не собрана.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData(typeof(AxListBoxItem))]
    [InlineData(typeof(AxComboBoxItem))]
    [InlineData(typeof(AxTreeViewItem))]
    [InlineData(typeof(AxTabItem))]
    [InlineData(typeof(AxSegmentItem))]
    [InlineData(typeof(AxBreadcrumbItem))]
    [InlineData(typeof(AxMenuItem))]
    [InlineData(typeof(AxLink))]
    [InlineData(typeof(AxButton))]
    [InlineData(typeof(AxTextBox))]
    [InlineData(typeof(AxSearchField))]
    [InlineData(typeof(AxTextArea))]
    [InlineData(typeof(AxComboBox))]
    [InlineData(typeof(AxCheckBox))]
    [InlineData(typeof(AxRadioButton))]
    [InlineData(typeof(AxToggleSwitch))]
    public void Disabled_control_takes_the_disabled_foreground(Type controlType)
    {
        var control = (TemplatedControl)Activator.CreateInstance(controlType)!;
        control.IsEnabled = false;

        var window = Shown(control);

        Assert.Equal(Resource(window, "AxFgDisabledBrush"), Colour(control.Foreground));

        window.Close();
    }

    /// <summary>
    /// Ползунок гаснет заполнением и ручкой — текста у него нет.
    /// </summary>
    [AvaloniaFact]
    public void Disabled_slider_paints_its_fill_and_thumb()
    {
        var slider = new AxSlider { Minimum = 0, Maximum = 100, Value = 60, Width = 140, IsEnabled = false };
        var window = Shown(slider);

        var disabled = Resource(window, "AxFgDisabledBrush");

        var fill = slider.GetVisualDescendants().OfType<RepeatButton>().First();
        var thumb = slider.GetVisualDescendants().OfType<Thumb>().Single();
        var knob = thumb.GetVisualDescendants().OfType<Ellipse>().Single();

        Assert.Equal(disabled, Colour(fill.Background));
        Assert.Equal(disabled, Colour(knob.Stroke));

        window.Close();
    }

    /// <summary>
    /// Ни один контрол не гаснет прозрачностью: цвет не должен зависеть от
    /// того, что лежит под контролом.
    /// </summary>
    [AvaloniaTheory]
    [InlineData(typeof(AxToggleSwitch))]
    [InlineData(typeof(AxSlider))]
    [InlineData(typeof(AxButton))]
    [InlineData(typeof(AxCheckBox))]
    public void Disabled_control_keeps_full_opacity(Type controlType)
    {
        var control = (TemplatedControl)Activator.CreateInstance(controlType)!;
        control.IsEnabled = false;

        var window = Shown(control);

        Assert.Equal(1d, control.Opacity);

        window.Close();
    }

    private static Window Shown(Control control)
    {
        var window = new Window { Content = control };

        window.Show();
        window.UpdateLayout();

        return window;
    }

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color? Resource(Window window, string key)
    {
        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value), key);

        return ((ISolidColorBrush)value!).Color;
    }
}
