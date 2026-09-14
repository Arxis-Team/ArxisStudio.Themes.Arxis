using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Контрол с подписью растёт вместе с кеглем темы, а не срезает её.
/// </summary>
/// <remarks>
/// Высоты контролов — наименьшие, а не прибитые. Двадцать восемь у кнопки,
/// тридцать четыре у вкладки, сорок у полосы заголовка — это их высоты при
/// кегле темы; вырасти кегль — настройкой размера текста или крупным шрифтом
/// плагина, — и прибитая высота срезала бы подпись снизу, ни словом об этом не
/// сказав. Прогон студии с удвоенной шкалой кеглей так и срезал все её кнопки.
/// <para>
/// Смотрится показанный текст, а не свойства контрола: срезанная строка
/// получает от раскладки меньше, чем попросила, или выходит за край того, что
/// её обрезает. Оба признака видны по границам, каким бы путём высоту ни
/// прибили — сеттером темы, частью шаблона или разметкой.
/// </para>
/// </remarks>
public class TextGrowthTests
{
    /// <summary>Во сколько раз растёт шкала кеглей.</summary>
    /// <remarks>
    /// Втрое, а не вдвое: при двойном кегле строка основного текста выходит
    /// около тридцати пяти — меньше сорока полосы заголовка, — и прибитая
    /// полоса прошла бы проверку, ничего не показав.
    /// </remarks>
    private const double Factor = 3;

    /// <summary>Полпикселя на округление раскладки.</summary>
    private const double Tolerance = 0.5;

    /// <summary>Вся шкала кеглей темы.</summary>
    private static readonly string[] Sizes =
    [
        "AxFontSize", "AxFontSizeSmall", "AxFontSizeCaption",
        "AxFontSizeLarge", "AxFontSizeTitle", "AxFontSizeDisplay",
    ];

    private static readonly Dictionary<string, Func<Control>> Samples = new(StringComparer.Ordinal)
    {
        ["AxButton"] = () => new AxButton { Content = "Открыть" },
        ["AxButton.compact"] = () => new AxButton { Classes = { "compact" }, Content = "Открыть" },
        ["AxDropDownButton"] = () => new AxDropDownButton { Content = "Сборка" },
        ["AxSplitButton"] = () => new AxSplitButton { Content = "Выключить" },
        ["AxSegmentedControl"] = () => new AxSegmentedControl
        {
            ItemsSource = new[] { new AxSegmentItem { Content = "Тёмная" }, new AxSegmentItem { Content = "Светлая" } },
            SelectedIndex = 0,
        },
        ["AxTabItem"] = () => new AxTabStrip { ItemsSource = new[] { new AxTabItem { Content = "App.axaml" } } },
        ["AxToolWindow"] = () => new AxToolWindow { Title = "Проект" },
        ["AxToolWindow.tabs"] = () => new AxToolWindow { Title = "Проект", Tabs = HeaderTabs() },
        ["AxTreeView"] = () => new AxTreeView { ItemsSource = new[] { new AxTreeViewItem { Header = "App.axaml" } } },
        ["AxBreadcrumbBar.framed"] = () => new AxBreadcrumbBar
        {
            Classes = { "framed" },
            ItemsSource = new[] { new AxBreadcrumbItem { Content = "App" }, new AxBreadcrumbItem { Content = "Views" } },
        },
        ["AxTitleBar"] = () => new AxTitleBar { ShowWindowControls = false, Content = new TextBlock { Text = "Настройки" } },
        ["AxBadge"] = () => new AxBadge { Content = "12" },
        ["AxChip"] = () => new AxChip { Content = "Изменено" },
        ["AxChip.kbd"] = () => new AxChip { Classes = { "kbd" }, Content = "Ctrl K" },
        ["AxAvatar"] = () => new AxAvatar { Initials = "FF" },
        ["AxBanner"] = () => new AxBanner { Content = "Плагин выключен" },
        ["AxGroupHeader"] = () => new AxGroupHeader { Content = "Недавние" },
        ["AxLink"] = () => new AxLink { Content = "Подробнее" },
        ["AxCheckBox"] = () => new AxCheckBox { Content = "Показывать" },
        ["AxRadioButton"] = () => new AxRadioButton { Content = "Тёмная" },
        ["AxToggleSwitch"] = () => new AxToggleSwitch { Content = "Включено" },
        ["AxComboBox"] = () => new AxComboBox { ItemsSource = new[] { "Русский" }, SelectedIndex = 0 },
        ["AxListBox"] = () => new AxListBox { ItemsSource = new[] { "Консоль" } },
        ["AxTextBox"] = () => new AxTextBox { PlaceholderText = "Поиск" },
    };

    public static TheoryData<string> Controls => new(Samples.Keys);

    /// <summary>Подпись цела и при тройной шкале кеглей.</summary>
    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void A_label_stays_whole_when_the_type_grows(string sample)
    {
        var control = Samples[sample]();
        var window = Shown(control);
        var height = control.Bounds.Height;

        foreach (var label in Labels(control))
            Assert.True(Whole(label, control, out var why), $"{sample}: «{label.Text}» срезана уже в обычном кегле — {why}");

        Enlarge(window);

        var labels = Labels(control);

        Assert.NotEmpty(labels);
        // Подпись, оставшаяся ниже исходной высоты контрола, поместилась бы и
        // в прибитую: такой прогон ничего не доказал бы.
        Assert.True(
            labels.Max(Needed) > height,
            $"{sample}: подписи в тройном кегле не выше исходных {height} — проверять нечего");

        foreach (var label in labels)
            Assert.True(Whole(label, control, out var why), $"{sample}: «{label.Text}» срезана — {why}");

        window.Close();
    }

    /// <summary>Обычный кегль контрол не распирает: высота остаётся той, что в проекте.</summary>
    /// <remarks>
    /// Обратная сторона той же правки. Наименьшая высота без выравнивания по
    /// центру растянула бы контрол на всю ячейку, а подпись, которой высоты
    /// вдруг не хватило, увеличила бы его и в обычном кегле.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("AxButton", 28d)]
    [InlineData("AxButton.compact", 24d)]
    [InlineData("AxDropDownButton", 28d)]
    [InlineData("AxSplitButton", 28d)]
    [InlineData("AxSegmentedControl", 28d)]
    [InlineData("AxTitleBar", 40d)]
    [InlineData("AxBreadcrumbBar.framed", 28d)]
    [InlineData("AxBadge", 16d)]
    [InlineData("AxChip.kbd", 20d)]
    public void A_control_in_a_taller_cell_keeps_its_height(string sample, double height)
    {
        var control = Samples[sample]();
        var window = new Window
        {
            Width = 600,
            Height = 300,
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = control,
        };

        window.Show();
        window.UpdateLayout();

        Assert.Equal(height, control.Bounds.Height);
        Assert.Equal((300 - height) / 2, control.Bounds.Y);

        window.Close();
    }

    /// <summary>Вкладки шапки панели — со своей темой, как их ставит студия.</summary>
    private static AxTabStrip HeaderTabs()
    {
        var strip = new AxTabStrip
        {
            ItemsSource = new[]
            {
                new AxTabItem { Classes = { "compact" }, Content = "Консоль" },
                new AxTabItem { Classes = { "compact" }, Content = "Терминал" },
            },
        };

        Assert.True(Application.Current!.TryFindResource("AxToolWindowTabStrip", out var theme));
        strip.Theme = (ControlTheme)theme!;

        return strip;
    }

    private static Window Shown(Control control)
    {
        // Стопка, а не окно: содержимое окна растягивается на всю высоту, и
        // контрол занял бы её, не спросив подписи.
        var window = new Window
        {
            Width = 1200,
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = new StackPanel { Children = { control } },
        };

        window.Show();
        window.UpdateLayout();

        return window;
    }

    private static void Enlarge(Window window)
    {
        foreach (var key in Sizes)
        {
            Assert.True(Application.Current!.TryFindResource(key, out var size), $"в теме нет кегля {key}");
            window.Resources[key] = (double)size! * Factor;
        }

        window.UpdateLayout();
    }

    private static List<TextBlock> Labels(Control control) =>
    [
        .. control.GetVisualDescendants()
            .OfType<TextBlock>()
            .Where(label => label.IsEffectivelyVisible && !string.IsNullOrEmpty(label.Text)),
    ];

    /// <summary>Сколько строке нужно на самом деле.</summary>
    /// <remarks>
    /// Не <c>DesiredSize</c>: раскладка урезает его до того, что родитель
    /// предложил, и в прибитой высоте срезанная строка «просит» ровно столько,
    /// сколько ей дали. Набранный текст урезать нельзя — первая строка
    /// раскладывается при любом ограничении, и её высота настоящая.
    /// </remarks>
    private static double Needed(TextBlock label) =>
        label.TextLayout.Height + label.Padding.Top + label.Padding.Bottom;

    /// <summary>Строка получила всю высоту, какая ей нужна, и ничто её не обрезает.</summary>
    /// <remarks>
    /// Обрезает контрол и всякий предок внутри него с <c>ClipToBounds</c>.
    /// Сравнение идёт через преобразование, а не через смещения: инициалы
    /// аватара вписаны в плитку уменьшением, и настоящий их размер — после него.
    /// </remarks>
    private static bool Whole(TextBlock label, Control control, out string why)
    {
        if (label.Bounds.Height + Tolerance < Needed(label))
        {
            why = $"строке нужно {Needed(label):0.#}, а дали {label.Bounds.Height:0.#}";
            return false;
        }

        for (var clip = label.GetVisualParent(); clip is not null; clip = clip.GetVisualParent())
        {
            if (clip == control || clip.ClipToBounds)
            {
                var drawn = new Rect(label.Bounds.Size).TransformToAABB(label.TransformToVisual(clip)!.Value);

                if (drawn.Top < -Tolerance || drawn.Bottom > clip.Bounds.Height + Tolerance)
                {
                    why = $"строка {drawn.Top:0.#}…{drawn.Bottom:0.#} выходит из {clip.GetType().Name} высотой {clip.Bounds.Height:0.#}";
                    return false;
                }
            }

            if (clip == control)
                break;
        }

        why = string.Empty;
        return true;
    }
}
