using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
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
        ["AxButton.compact"] = () => new AxButton { Size = AxControlSize.Compact, Content = "Открыть" },
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
        ["AxTitleBar"] = () => new AxTitleBar { ShowWindowControls = false, Content = new TextBlock { Text = "Настройки" } },
        ["AxBadge"] = () => new AxBadge { Content = "12" },
        ["AxChip"] = () => new AxChip { Content = "Изменено" },
        ["AxChip.kbd"] = () => new AxChip { Kind = AxChipKind.Key, Content = "Ctrl K" },
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

    /// <summary>Обычный кегль контрол не распирает: высота остаётся прежней.</summary>
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

    /// <summary>Пилюля остаётся пилюлей, когда растёт с кеглем.</summary>
    /// <remarks>
    /// Бейдж скруглялся числом — половиной своих шестнадцати, — чип десяткой при высоте около
    /// двадцати. Выросши с кеглем, оба стали бы прямоугольниками со скруглёнными углами, а
    /// полукруглый торец и есть то, чем пилюля отличается от плашки.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("AxBadge")]
    [InlineData("AxChip")]
    public void A_pill_stays_a_pill_when_it_grows(string sample)
    {
        var control = Assert.IsAssignableFrom<Avalonia.Controls.Primitives.TemplatedControl>(Samples[sample]());
        var window = Shown(control);
        var height = control.Bounds.Height;

        Enlarge(window);

        Assert.True(control.Bounds.Height > height, $"{sample}: высота {height} не выросла с кеглем — проверять нечего");
        Assert.True(
            control.CornerRadius.TopLeft * 2 >= control.Bounds.Height,
            $"{sample}: радиус {control.CornerRadius.TopLeft} при высоте {control.Bounds.Height} — торцы уже не полукруглые");

        window.Close();
    }

    /// <summary>
    /// Переносимый текст получает межстрочный интервал долей кегля.
    /// </summary>
    /// <remarks>
    /// Абзац читают по строкам, и расстояние между ними — часть набора. Доля, а не
    /// пиксели: при выросшем кегле прибитая высота строки оставила бы строки на
    /// месте, а буквы подняла бы друг на друга.
    /// </remarks>
    [AvaloniaFact]
    public void Wrapping_text_takes_the_line_height_of_the_scale()
    {
        var text = new TextBlock { Text = "Описание, которое переносится на вторую строку", TextWrapping = TextWrapping.Wrap };
        var window = Shown(text);

        Assert.True(Application.Current!.TryFindResource("AxLineHeightRatio", out var ratio));
        Assert.True(Application.Current!.TryFindResource("AxFontSize", out var size));

        var expected = Math.Round((double)size! * (double)ratio!);

        Assert.Equal(expected, text.LineHeight);

        Enlarge(window);

        Assert.Equal(Math.Round((double)size! * Factor * (double)ratio!), text.LineHeight);

        window.Close();
    }

    /// <summary>Однострочной подписи интервал не ставят: второй строки у неё нет.</summary>
    /// <remarks>
    /// Высота строки подняла бы ряд, в котором подпись стоит, — а поднимать его
    /// нечему: строку в ряду мерит контрол, а не абзац.
    /// </remarks>
    [AvaloniaFact]
    public void A_single_line_label_keeps_the_line_height_of_its_font()
    {
        var text = new TextBlock { Text = "Имя формы" };
        var window = Shown(text);

        Assert.True(double.IsNaN(text.LineHeight), $"подпись получила высоту строки {text.LineHeight}");

        window.Close();
    }

    /// <summary>Вкладки шапки панели — со своей темой, как их ставит студия.</summary>
    private static AxTabStrip HeaderTabs()
    {
        var strip = new AxTabStrip
        {
            ItemsSource = new[]
            {
                new AxTabItem { Content = "Консоль" },
                new AxTabItem { Content = "Терминал" },
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
