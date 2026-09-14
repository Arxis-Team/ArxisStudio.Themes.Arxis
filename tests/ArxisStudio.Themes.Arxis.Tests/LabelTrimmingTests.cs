using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Подпись, которой не хватает ширины, кончается многоточием и читается целиком в подсказке.
/// </summary>
/// <remarks>
/// Кнопка и вкладка в узкой панели обрывали подпись краем на полуслове: ни многоточия, ни
/// подсказки, и ничто не говорило, что подпись длиннее видимого. Вкладка к тому же уходила за
/// край полосы целиком, вместе с крестиком.
/// </remarks>
public class LabelTrimmingTests
{
    private const string Long = "Сосчитать файлы проекта и показать итог";

    /// <summary>Узкая кнопка сокращает подпись многоточием, и подпись не выходит за кнопку.</summary>
    [AvaloniaTheory]
    [InlineData("button")]
    [InlineData("dropdown")]
    [InlineData("split")]
    public void A_label_too_long_for_its_button_ends_in_an_ellipsis(string kind)
    {
        var (control, window) = Shown(Button(kind), width: 120);
        var label = Label(control);
        var right = label.TranslatePoint(new Point(label.Bounds.Width, 0), control)!.Value.X;

        Assert.True(Trimmed(label), $"подпись «{label.Text}» шире кнопки, а не сокращена");
        Assert.True(right <= control.Bounds.Width + 0.5, $"подпись кончается на {right:0.#}, за краем кнопки {control.Bounds.Width:0.#}");

        window.Close();
    }

    /// <summary>Одинокая вкладка не шире своей полосы: имя сокращено, крестик на месте.</summary>
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_lone_tab_is_never_wider_than_its_strip(bool panel)
    {
        var strip = Strip(panel, Long);
        var (_, window) = Shown(strip, width: 140);
        var tab = strip.GetVisualDescendants().OfType<AxTabItem>().Single();
        var viewer = strip.GetVisualDescendants().OfType<ScrollViewer>().First();
        var cross = Part(tab, "PART_Close");
        var right = cross.TranslatePoint(new Point(cross.Bounds.Width, 0), viewer)!.Value.X;

        Assert.True(Trimmed(Label(tab)), "имя вкладки шире полосы, а не сокращено");
        Assert.True(right <= viewer.Viewport.Width + 0.5, $"крестик кончается на {right:0.#}, а полоса — на {viewer.Viewport.Width:0.#}");
        Assert.True(viewer.Extent.Width <= viewer.Viewport.Width + 0.5, "полоса прокручивается ради одной вкладки");

        window.Close();
    }

    /// <summary>Полосу сузили после раскладки — вкладка сокращает имя вслед за ней.</summary>
    /// <remarks>
    /// Панель тянут за границу, и полоса сужается под уже разложенной вкладкой. Сама вкладка об
    /// этом не узнаёт: полоса даёт ей бесконечную ширину и прежде давала такую же.
    /// </remarks>
    [AvaloniaFact]
    public void A_tab_follows_its_strip_when_the_strip_narrows()
    {
        var strip = Strip(panel: true, Long);
        var (_, window) = Shown(strip, width: 2000);
        var tab = strip.GetVisualDescendants().OfType<AxTabItem>().Single();

        Assert.False(Trimmed(Label(tab)), "в широкой полосе имя вкладки сокращено");

        ((Control)strip.Parent!).Width = 140;
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        var viewer = strip.GetVisualDescendants().OfType<ScrollViewer>().First();

        Assert.True(Trimmed(Label(tab)), "полоса сузилась, а имя вкладки не сокращено");
        Assert.True(tab.Bounds.Width <= viewer.Viewport.Width + 0.5, $"вкладка {tab.Bounds.Width:0.#} шире полосы {viewer.Viewport.Width:0.#}");

        window.Close();
    }

    /// <summary>Вкладки, которые не влезают вместе, остаются своей ширины, и полоса прокручивается.</summary>
    /// <remarks>
    /// Предел — ширина окна полосы на каждую вкладку, а не на все вместе: ряд документов не
    /// сжимается в нечитаемые обрубки, а прокручивается, как прокручивался.
    /// </remarks>
    [AvaloniaFact]
    public void Tabs_that_do_not_fit_together_keep_their_names_and_scroll()
    {
        var strip = Strip(panel: false, "MainWindow.axaml", "App.axaml", "Settings.axaml");
        var (_, window) = Shown(strip, width: 2000);
        var tabs = strip.GetVisualDescendants().OfType<AxTabItem>().ToList();
        var natural = tabs.Select(tab => tab.Bounds.Width).ToList();

        strip.Width = natural.Max() + 8;
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        var viewer = strip.GetVisualDescendants().OfType<ScrollViewer>().First();

        Assert.Equal(natural, tabs.Select(tab => tab.Bounds.Width));
        Assert.All(tabs, tab => Assert.False(Trimmed(Label(tab)), "вкладка, которой хватает окна полосы, сокращена"));
        Assert.True(viewer.Extent.Width > viewer.Viewport.Width, "ряд вкладок шире полосы, а прокручивать нечего");

        window.Close();
    }

    /// <summary>У сокращённой подписи подсказка — её полный текст.</summary>
    [AvaloniaTheory]
    [InlineData("button")]
    [InlineData("dropdown")]
    [InlineData("tab")]
    public void A_trimmed_label_opens_its_full_text_as_a_tip(string kind)
    {
        var (control, window) = Shown(Tipped(kind), width: 120);
        var target = Target(control);

        Assert.True(Hover(target, window), "у сокращённой подписи подсказка не открылась");
        Assert.Equal(Long, ToolTip.GetTip(target));

        window.Close();
    }

    /// <summary>У половины действия разделённой кнопки то же правило подсказки, что у кнопки.</summary>
    /// <remarks>
    /// Наведением здесь не проверить: у половины скругление неравное — «4,0,0,4», — а безголовое
    /// рисование тестов темы попадания в такую фигуру не считает, и мышь её не находит. Под
    /// настоящим рисованием Skia, в тестах студии, наведение на половину подсказку открывает — это
    /// замерено пробой.
    /// </remarks>
    [AvaloniaFact]
    public void The_action_half_of_a_split_button_carries_the_tip_rule()
    {
        var (control, window) = Shown(Button("split"), width: 120);
        var half = Target(control);

        Assert.True(AxTrimmedTip.GetIsEnabled(half), "у половины действия нет правила подсказки");
        Assert.Equal(string.Empty, ToolTip.GetTip(half));

        window.Close();
    }

    /// <summary>У целой подписи подсказки нет: повторять видимое незачем.</summary>
    [AvaloniaTheory]
    [InlineData("button")]
    [InlineData("tab")]
    public void A_whole_label_opens_no_tip(string kind)
    {
        var (control, window) = Shown(Tipped(kind), width: 2000);

        Assert.False(Hover(Target(control), window), "подсказка повторяет подпись, которая видна целиком");

        window.Close();
    }

    /// <summary>Подпись снова уместилась — подсказка больше не открывается.</summary>
    /// <remarks>
    /// Решение принимается в миг открытия, а не один раз: панель расширили, язык сменили, кегль
    /// уменьшили — и подсказка, однажды показанная, не должна остаться навсегда.
    /// </remarks>
    [AvaloniaFact]
    public void A_label_that_fits_again_opens_no_tip()
    {
        var (control, window) = Shown(Button("button"), width: 120);

        Assert.True(Hover(control, window), "у сокращённой подписи подсказка не открылась");

        window.Width = 2000;
        ((Control)control.Parent!).Width = 2000;
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        Assert.False(Trimmed(Label(control)), "подпись по-прежнему сокращена — проверять нечего");
        Assert.False(Hover(control, window), "подпись уместилась, а подсказка открывается по-прежнему");

        window.Close();
    }

    /// <summary>Своя подсказка кнопки остаётся своей, даже когда подпись сокращена.</summary>
    [AvaloniaFact]
    public void An_explicit_tip_is_left_alone()
    {
        const string own = "Считает файлы в папке открытого проекта";

        var button = Button("button");

        ToolTip.SetTip(button, own);

        var (_, window) = Shown(button, width: 120);

        Assert.True(Hover(button, window), "своя подсказка кнопки не открылась");
        Assert.Equal(own, ToolTip.GetTip(button));

        window.Close();
    }

    private static Control Button(string kind) => kind switch
    {
        "button" => new AxButton { Content = Long },
        "dropdown" => new AxDropDownButton { Content = Long },
        "split" => new AxSplitButton { Content = Long },
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static Control Tipped(string kind) =>
        kind == "tab" ? Strip(panel: true, Long) : Button(kind);

    /// <summary>Кому открывают подсказку: у разделённой кнопки — половине действия, у полосы — вкладке.</summary>
    private static Control Target(Control control) => control switch
    {
        AxSplitButton split => Part(split, "PART_PrimaryButton"),
        AxTabStrip strip => strip.GetVisualDescendants().OfType<AxTabItem>().Single(),
        _ => control,
    };

    private static AxTabStrip Strip(bool panel, params string[] names)
    {
        var strip = new AxTabStrip();

        if (panel)
            strip.Theme = (ControlTheme)Application.Current!.FindResource("AxToolWindowTabStrip")!;

        foreach (var name in names)
        {
            var tab = new AxTabItem { Content = name, IsClosable = true };

            if (panel)
                tab.Classes.Add("compact");

            strip.Items.Add(tab);
        }

        return strip;
    }

    private static (Control Control, Window Window) Shown(Control control, double width)
    {
        control.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;

        var host = new Border { Width = width, Child = control };

        if (control is not AxTabStrip)
            control.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;

        var window = new Window
        {
            Width = Math.Max(width, 400),
            Height = 300,
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = host,
        };

        window.Show();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        return (control, window);
    }

    /// <summary>Наводит мышь на контрол, как человек, и говорит, открылась ли подсказка.</summary>
    /// <remarks>
    /// Наведением, а не свойством IsOpen: по наведению подсказку открывает служба подсказок, и
    /// контрол без подсказки она не спрашивает вовсе — открытие через свойство этого не видит.
    /// Задержка снята: время в безголовом режиме стоит.
    /// </remarks>
    private static bool Hover(Control target, Window window)
    {
        ToolTip.SetShowDelay(target, 0);

        var centre = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window)!.Value;

        window.MouseMove(centre);
        Dispatcher.UIThread.RunJobs();

        var open = ToolTip.GetIsOpen(target);

        window.MouseMove(new Point(window.Bounds.Width - 2, window.Bounds.Height - 2));
        Dispatcher.UIThread.RunJobs();

        return open;
    }

    private static TextBlock Label(Control control) =>
        control.GetVisualDescendants().OfType<TextBlock>().First(label => !string.IsNullOrEmpty(label.Text));

    private static bool Trimmed(TextBlock label) =>
        label.TextLayout.TextLines.Any(line => line.HasCollapsed);

    private static Control Part(Control control, string name)
    {
        var part = control.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Name == name);

        Assert.True(part is not null, $"в шаблоне нет части {name}");
        return part!;
    }
}
