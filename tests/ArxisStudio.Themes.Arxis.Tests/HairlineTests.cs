using ArxisStudio.Controls;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Разделитель — один пиксель устройства при любом масштабе экрана.
/// </summary>
/// <remarks>
/// Прибитая единица раскладки ровна одному пикселю только при 100 % и 200 %. При 125 % она даёт
/// 1,25 и округляется до двух, при 150 % — 1,5, тоже до двух: линия, задуманная волосяной,
/// становится вдвое толще ровно там, где соседние длины остаются на месте. Рядом с панелью это
/// читается не как разделитель, а как рамка.
/// <para>
/// Поэтому толщину линия считает сама: <c>1 / масштаб</c>. При 150 % это 0,667 — округление
/// раскладки сажает такую длину ровно на один пиксель.
/// </para>
/// </remarks>
public class HairlineTests
{
    /// <summary>Масштабы, на которых студия живёт: шаг в четверть.</summary>
    public static TheoryData<double> Scales => [1d, 1.25d, 1.5d, 1.75d, 2d];

    /// <summary>Горизонтальная линия — пиксель в высоту при любом масштабе.</summary>
    [AvaloniaTheory]
    [MemberData(nameof(Scales))]
    public void A_horizontal_divider_is_one_device_pixel(double scaling)
    {
        var divider = new AxDivider { Orientation = Orientation.Horizontal };
        var window = Shown(divider, scaling);

        Assert.Equal(1d, Math.Round(divider.Bounds.Height * scaling, 6));

        window.Close();
    }

    /// <summary>Вертикальная линия — пиксель в ширину при любом масштабе.</summary>
    [AvaloniaTheory]
    [MemberData(nameof(Scales))]
    public void A_vertical_divider_is_one_device_pixel(double scaling)
    {
        var divider = new AxDivider { Orientation = Orientation.Vertical, Height = 20 };
        var window = Shown(divider, scaling);

        Assert.Equal(1d, Math.Round(divider.Bounds.Width * scaling, 6));

        window.Close();
    }

    /// <summary>
    /// Линия разделителя областей — та же, и меряется так же.
    /// </summary>
    /// <remarks>
    /// Полоса захвата вокруг неё остаётся семью единицами раскладки: за пиксель мышью не
    /// возьмёшься, и цель попадания за масштабом не идёт.
    /// </remarks>
    [AvaloniaTheory]
    [MemberData(nameof(Scales))]
    public void The_splitter_line_is_one_device_pixel(double scaling)
    {
        var splitter = new AxSplitter { Orientation = Orientation.Horizontal };
        var window = Shown(splitter, scaling);

        var line = splitter.GetVisualDescendants().OfType<AxDivider>().Single(part => part.Name == "PART_Line");

        Assert.Equal(1d, Math.Round(line.Bounds.Height * scaling, 6));

        window.Close();
    }

    /// <summary>Линии хрома — те же разделители, и меряются так же.</summary>
    /// <remarks>
    /// Полоса заголовка, полоса вкладок и шапка панели носили линию нижней рамкой. Рамка мерится
    /// раскладочной единицей, и при 125 и 150 % под каждой полосой хрома вырастал кант в два
    /// пикселя — вдвое толще линий, которыми разрезаны доки под ними. Границу области рисует
    /// разделитель, а рамка остаётся тому, что обводит контрол: полю ввода, кнопке, попапу.
    /// </remarks>
    [AvaloniaTheory]
    [MemberData(nameof(Chrome))]
    public void A_chrome_rule_is_one_device_pixel(string part, double scaling)
    {
        var (control, name) = Band(part);
        var window = Shown(control, scaling);

        var rule = control.GetVisualDescendants().OfType<AxDivider>().Single(child => child.Name == name);

        Assert.Equal(1d, Math.Round(rule.Bounds.Height * scaling, 6));

        window.Close();
    }

    /// <summary>Полосы хрома со своей линией, на каждом масштабе.</summary>
    public static TheoryData<string, double> Chrome
    {
        get
        {
            var data = new TheoryData<string, double>();

            foreach (var part in new[] { "title-bar", "tab-strip", "tool-window", "quick-search" })
            {
                foreach (var scaling in new[] { 1d, 1.25d, 1.5d, 1.75d, 2d })
                    data.Add(part, scaling);
            }

            return data;
        }
    }

    /// <summary>Полоса хрома и имя её линии в шаблоне.</summary>
    private static (Control Band, string Rule) Band(string part) => part switch
    {
        "title-bar" => (new AxTitleBar { ShowWindowControls = false, Content = new TextBlock() }, "PART_Rule"),
        "tab-strip" => (new AxTabStrip { ItemsSource = new[] { "Program.cs" } }, "PART_Rule"),
        "tool-window" => (new AxToolWindow { Title = "Проект", ShowHeaderSeparator = true }, "PART_HeaderRule"),
        "quick-search" => (new AxQuickSearch { ItemsSource = new[] { "ChatView.axaml" } }, "PART_FieldRule"),
        _ => throw new ArgumentOutOfRangeException(nameof(part), part, null),
    };

    /// <summary>Масштаб сменился на ходу — линия пересчитала себя.</summary>
    /// <remarks>
    /// Окно переезжает между экранами с разным масштабом, и пиксель устройства становится другой
    /// длины. Линия слушает сам масштаб окна, а не меряет себя один раз при рождении.
    /// </remarks>
    [AvaloniaFact]
    public void A_divider_follows_the_window_to_another_screen()
    {
        var divider = new AxDivider { Orientation = Orientation.Horizontal };
        var window = Shown(divider, 1d);

        Assert.Equal(1d, divider.Bounds.Height);

        window.SetRenderScaling(2d);
        window.UpdateLayout();

        Assert.Equal(0.5d, divider.Bounds.Height);

        window.Close();
    }

    private static Window Shown(Control content, double scaling)
    {
        var window = new Window
        {
            Width = 200,
            Height = 100,
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = content,
        };

        window.Show();
        window.SetRenderScaling(scaling);
        window.UpdateLayout();

        return window;
    }
}
