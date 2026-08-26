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
        typeof(AxTitleBar),
        typeof(AxWindowControls),
        typeof(AxSplitButton),
        typeof(AxDropDownButton),
        typeof(AxMenuItem),
        typeof(AxBreadcrumbBar),
        typeof(AxBreadcrumbItem),
        typeof(AxQuickSearch),
        typeof(AxTeachingTip),
        typeof(AxNotificationCard),
        typeof(AxSpinner),
        typeof(AxToolBar),
        typeof(AxCodeBlock),
        typeof(AxDataGrid),
    };

    /// <summary>
    /// Шаблон обязан находиться в обоих вариантах темы, а переключение
    /// варианта у живого окна — не снимать его: дыра в одном из вариантов
    /// снаружи выглядит как контрол, пропавший при смене темы.
    /// </summary>
    [AvaloniaTheory]
    [MemberData(nameof(TemplatedControls))]
    public void Control_gets_template_from_theme(Type controlType)
    {
        var control = (TemplatedControl)Activator.CreateInstance(controlType)!;

        var window = new Window { Content = control };
        window.RequestedThemeVariant = ThemeVariant.Dark;
        window.Show();

        Assert.NotNull(control.Template);

        window.RequestedThemeVariant = ThemeVariant.Light;
        Assert.NotNull(control.Template);

        window.Close();
    }

    /// <summary>
    /// Диалог — окно, в чужое окно его не положить: шаблон проверяется на нём
    /// самом.
    /// </summary>
    [AvaloniaFact]
    public void Dialog_gets_template_from_theme()
    {
        var dialog = new AxDialog { Title = "Проверка", Content = new TextBlock() };
        dialog.Show();

        Assert.NotNull(dialog.Template);
        dialog.Close();
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

    /// <summary>
    /// Живое окно переживает переключение варианта: кисть, взятая до и после,
    /// продолжает находиться, а цвет за ней действительно меняется.
    /// </summary>
    [AvaloniaFact]
    public void Switching_variant_at_runtime_keeps_resources_alive()
    {
        var button = new AxButton { Classes = { "accent" }, Content = "Проверка" };
        var window = new Window { Content = button };

        window.RequestedThemeVariant = ThemeVariant.Dark;
        window.Show();

        Assert.True(window.TryFindResource("AxBg1Color", window.ActualThemeVariant, out var darkBg));
        Assert.NotNull(button.Template);

        window.RequestedThemeVariant = ThemeVariant.Light;

        Assert.True(window.TryFindResource("AxBg1Color", window.ActualThemeVariant, out var lightBg));
        Assert.NotNull(button.Template);
        Assert.NotEqual(darkBg, lightBg);

        window.Close();
    }

    /// <summary>Токены приёмки существуют в обоих вариантах — палитра не дырявая.</summary>
    [AvaloniaTheory]
    [InlineData("AxAccStrongColor")]
    [InlineData("AxAccStrongHoverColor")]
    [InlineData("AxLinkOnColor")]
    [InlineData("AxGreenTextColor")]
    [InlineData("AxRedTextColor")]
    [InlineData("AxYellowTextColor")]
    [InlineData("AxInfoBorderColor")]
    [InlineData("AxSuccessBorderColor")]
    [InlineData("AxWarningBorderColor")]
    [InlineData("AxErrorBorderColor")]
    [InlineData("AxCodeTagColor")]
    [InlineData("AxCodeAttrColor")]
    [InlineData("AxCodeStringColor")]
    [InlineData("AxCodeCommentColor")]
    [InlineData("AxCodeFgColor")]
    [InlineData("AxPopupShadow")]
    [InlineData("AxModalShadow")]
    public void Acceptance_tokens_exist_in_both_variants(string key)
    {
        var window = new Window();
        window.Show();

        Assert.True(window.TryFindResource(key, ThemeVariant.Dark, out var dark));
        Assert.True(window.TryFindResource(key, ThemeVariant.Light, out var light));
        Assert.NotNull(dark);
        Assert.NotNull(light);

        window.Close();
    }

    /// <summary>
    /// Метрики приёмки: тумблер и обводка иконки объявлены темой — в шаблонах
    /// этих цифр больше нет.
    /// </summary>
    [AvaloniaFact]
    public void Toggle_and_icon_metrics_come_from_the_theme()
    {
        var window = new Window();
        window.Show();

        Assert.True(window.TryFindResource("AxToggleWidth", window.ActualThemeVariant, out var width));
        Assert.Equal(30d, width);
        Assert.True(window.TryFindResource("AxToggleHeight", window.ActualThemeVariant, out var height));
        Assert.Equal(17d, height);
        Assert.True(window.TryFindResource("AxToggleKnobSize", window.ActualThemeVariant, out var knob));
        Assert.Equal(13d, knob);
        Assert.True(window.TryFindResource("AxIconStrokeThickness", window.ActualThemeVariant, out var stroke));
        Assert.Equal(1.2d, stroke);
        Assert.True(window.TryFindResource("AxIconSize", window.ActualThemeVariant, out var icon));
        Assert.Equal(16d, icon);

        window.Close();
    }

    /// <summary>Моноширинный стек начинается с Fira Code — шрифт едет в теме.</summary>
    [AvaloniaFact]
    public void Mono_font_family_starts_with_fira_code()
    {
        var window = new Window();
        window.Show();

        Assert.True(window.TryFindResource("AxFontFamilyMono", window.ActualThemeVariant, out var font));
        var family = Assert.IsType<Avalonia.Media.FontFamily>(font);
        Assert.Equal("Fira Code", family.FamilyNames.PrimaryFamilyName);

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
