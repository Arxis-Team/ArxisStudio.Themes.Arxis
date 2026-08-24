using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Контракт темы: каждый публичный Ax*-контрол получает от ArxisTheme свой
/// ControlTheme — контрол, оказавшийся в окне без шаблона, означает дыру в теме.
/// </summary>
public class ThemeAppliesTemplatesTests
{
    /// <summary>Все templated-контролы библиотеки; новые добавляются сюда осознанно.</summary>
    public static TheoryData<Type> TemplatedControls => new()
    {
        typeof(AxButton),
        typeof(AxTextBox),
        typeof(AxSearchField),
        typeof(AxCheckBox),
        typeof(AxToggleSwitch),
        typeof(AxComboBox),
        typeof(AxComboBoxItem),
        typeof(AxListBox),
        typeof(AxListBoxItem),
        typeof(AxSegmentedControl),
        typeof(AxSegmentItem),
        typeof(AxBadge),
        typeof(AxChip),
        typeof(AxCard),
        typeof(AxProgressBar),
    };

    [AvaloniaTheory]
    [MemberData(nameof(TemplatedControls))]
    public void Control_gets_template_from_theme(Type controlType)
    {
        var control = (TemplatedControl)Activator.CreateInstance(controlType)!;

        var window = new Window { Content = control };
        window.Show();

        Assert.NotNull(control.Template);
        window.Close();
    }

    [AvaloniaFact]
    public void Palette_switches_with_theme_variant()
    {
        var window = new Window();
        window.Show();

        window.RequestedThemeVariant = ThemeVariant.Dark;
        Assert.True(window.TryFindResource("AxBg1Color", ThemeVariant.Dark, out var dark));

        window.RequestedThemeVariant = ThemeVariant.Light;
        Assert.True(window.TryFindResource("AxBg1Color", ThemeVariant.Light, out var light));

        Assert.NotEqual(dark, light);
        window.Close();
    }

    [AvaloniaFact]
    public void Segmented_control_creates_segment_containers()
    {
        var segmented = new AxSegmentedControl { ItemsSource = new[] { "Design", "XAML", "Split" } };

        var window = new Window { Content = segmented };
        window.Show();

        var container = segmented.ContainerFromIndex(0);
        Assert.IsType<AxSegmentItem>(container);
        window.Close();
    }
}
