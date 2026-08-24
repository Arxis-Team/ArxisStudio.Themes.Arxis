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
        typeof(AxAvatar),
        typeof(AxIcon),
        typeof(AxTextArea),
        typeof(AxLink),
        typeof(AxRadioButton),
        typeof(AxDivider),
        typeof(AxGroupHeader),
        typeof(AxBanner),
        typeof(AxTabStrip),
        typeof(AxTabItem),
        typeof(AxTreeView),
        typeof(AxTreeViewItem),
        typeof(AxSlider),
        typeof(AxToolWindow),
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
    public void Focus_ring_uses_the_int_ui_outline_width()
    {
        var window = new Window();
        window.Show();

        Assert.True(window.TryFindResource("AxFocusOutlineWidth", ActualThemeVariantOf(window), out var width));
        Assert.Equal(2d, width);

        Assert.True(window.TryFindResource("AxRowHeight", ActualThemeVariantOf(window), out var rowHeight));
        Assert.Equal(24d, rowHeight);

        window.Close();

        static ThemeVariant ActualThemeVariantOf(Window window) => window.ActualThemeVariant;
    }

    [AvaloniaTheory]
    [InlineData("AxGray1")]
    [InlineData("AxGray14")]
    [InlineData("AxBlue6")]
    [InlineData("AxOutlineFocusedColor")]
    [InlineData("AxErrorBackgroundColor")]
    public void Palette_scales_are_available_in_both_variants(string key)
    {
        var window = new Window();
        window.Show();

        Assert.True(window.TryFindResource(key, ThemeVariant.Dark, out var dark));
        Assert.True(window.TryFindResource(key, ThemeVariant.Light, out var light));
        Assert.NotNull(dark);
        Assert.NotNull(light);

        window.Close();
    }

    [AvaloniaFact]
    public void Tool_window_shows_title_tabs_and_actions_together()
    {
        var tabs = new AxTabStrip { ItemsSource = new[] { "Проект", "Консоль" } };
        var toolWindow = new AxToolWindow
        {
            Title = "Проект",
            Tabs = tabs,
            Actions = new AxButton { Classes = { "icon" } },
            Content = new TextBlock { Text = "содержимое" },
        };

        var window = new Window { Content = toolWindow, Width = 400, Height = 300 };
        window.Show();

        Assert.NotNull(toolWindow.Template);

        // Заголовок и вкладки в Int UI живут в одной строке шапки: вкладки
        // добавляются к заголовку, а не заменяют его.
        Assert.Equal("Проект", toolWindow.Title);
        Assert.Same(tabs, toolWindow.Tabs);

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
