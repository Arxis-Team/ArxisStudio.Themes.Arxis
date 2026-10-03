using System.Text;
using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Просмотр кода: вид из токенов темы, только видимые строки, каретка и выделение без правки.
/// </summary>
/// <remarks>
/// Рисование безголовое и метрик шрифта не знает: ширина знака здесь условная. Поэтому колонки
/// сверяются друг с другом, а не с пикселями, а строка — с долей кегля темы, которая от шрифта не
/// зависит: 20 при кегле 13.
/// </remarks>
public class CodeViewTests
{
    /// <summary>Строка просмотра при кегле темы.</summary>
    private const double Line = 20;

    /// <summary>
    /// Вид — из токенов: документ на основном фоне, роли кода, номера третичным, отметка и каретка.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_view_wears_the_code_tokens(string variant)
    {
        var (view, window) = Shown(Sample, variant);

        Assert.Equal(Token("AxSurfaceBaseColor", variant), Colour(view.Background));
        Assert.Equal(Token("AxCodeTextColor", variant), Colour(view.Foreground));
        Assert.Equal(Token("AxCodeTagColor", variant), Colour(view.TagBrush));
        Assert.Equal(Token("AxCodeAttributeColor", variant), Colour(view.AttributeBrush));
        Assert.Equal(Token("AxCodeStringColor", variant), Colour(view.StringBrush));
        Assert.Equal(Token("AxCodeCommentColor", variant), Colour(view.CommentBrush));
        Assert.Equal(Token("AxCodeExtensionColor", variant), Colour(view.ExtensionBrush));
        Assert.Equal(Token("AxCodePrefixColor", variant), Colour(view.PrefixBrush));
        Assert.Equal(Token("AxCodeDirectiveColor", variant), Colour(view.DirectiveBrush));
        Assert.Equal(Token("AxCodeErrorColor", variant), Colour(view.ErrorBrush));
        Assert.Equal(Token("AxTextTertiaryColor", variant), Colour(view.LineNumberBrush));
        Assert.Equal(Token("AxCodeHighlightFillColor", variant), Colour(view.HighlightBrush));
        Assert.Equal(Token("AxAccentColor", variant), Colour(view.HighlightMarkerBrush));
        Assert.Equal(Token("AxAccentColor", variant), Colour(view.CaretBrush));
        Assert.Equal(Token("AxSelectionInactiveColor", variant), Colour(view.SelectionBrush));

        window.Close();
    }

    /// <summary>Шрифт, строка, отступы и каретка — ключами темы, а не числами контрола.</summary>
    [AvaloniaFact]
    public void The_view_takes_its_measures_from_the_theme()
    {
        var (view, window) = Shown(Sample, "Dark");

        Assert.Contains("Cascadia Code", view.FontFamily.ToString());
        Assert.Equal(13d, view.FontSize);
        Assert.True(Math.Abs(view.Lines!.LineHeight - Line) < 0.001, $"строка просмотра {view.Lines.LineHeight}, а должно быть {Line}");
        Assert.Equal(Metric<Thickness>("AxCodeViewPadding"), view.Padding);
        Assert.Equal(Metric<Thickness>("AxCodeGutterPadding"), view.GutterPadding);
        Assert.Equal(Metric<double>("AxCaretWidth"), view.CaretThickness);
        Assert.Equal(Metric<double>("AxRowMarkerWidth"), view.HighlightMarkerWidth);

        window.Close();
    }

    /// <summary>
    /// Выделение горит полным цветом, пока клавиатура в просмотре, и гаснет, когда она ушла.
    /// </summary>
    [AvaloniaFact]
    public void The_selection_burns_while_the_keyboard_is_inside()
    {
        var view = new AxCodeView { Text = Sample };
        var other = new AxButton { Content = "Готово" };
        var window = Window(new StackPanel { Children = { view, other } }, "Dark");

        view.Focus();
        window.UpdateLayout();

        Assert.Equal(Token("AxSelectionActiveColor", "Dark"), Colour(view.SelectionBrush));

        other.Focus();
        window.UpdateLayout();

        Assert.Equal(Token("AxSelectionInactiveColor", "Dark"), Colour(view.SelectionBrush));

        window.Close();
    }

    /// <summary>Выключенный просмотр гаснет текстом целиком, вместе с подсветкой и номерами.</summary>
    [AvaloniaFact]
    public void A_disabled_view_fades_all_of_its_text()
    {
        var (view, window) = Shown(Sample, "Light");

        view.IsEnabled = false;
        window.UpdateLayout();

        var disabled = Token("AxTextDisabledColor", "Light");

        Assert.Equal(disabled, Colour(view.Foreground));
        Assert.Equal(disabled, Colour(view.LineNumberBrush));

        foreach (var role in Enum.GetValues<AxCodeRole>())
            Assert.Equal(disabled, Colour(view.BrushFor(role)));

        window.Close();
    }

    /// <summary>
    /// Раскладываются только строки у экрана: длинный документ стоит столько же, сколько экран.
    /// </summary>
    /// <remarks>
    /// Запас — экран сверху и экран снизу: мелкая прокрутка не раскладывает строки заново. Дальше
    /// запаса раскладка отпускается, и документ, пролистанный до конца, держит в памяти не больше,
    /// чем открытый.
    /// </remarks>
    [AvaloniaFact]
    public void Only_the_lines_near_the_screen_are_laid_out()
    {
        var (view, window) = Shown(Lines(20_000), "Dark", height: 200);

        Rendered();

        var screen = (int)Math.Ceiling(200 / Line) + 1;

        Assert.InRange(view.Lines!.RealizedLines, 1, screen * 3);

        view.CaretOffset = view.Text!.Length;
        view.ScrollIntoView(view.CaretOffset);
        window.UpdateLayout();
        Rendered();

        Assert.True(Scroll(view).Offset.Y > 0, "просмотр не уехал к концу документа");
        Assert.InRange(view.Lines.RealizedLines, 1, screen * 3);

        window.Close();
    }

    /// <summary>
    /// Прокрутка доходит до каждой строки: протяжённость — все строки и отступы.
    /// </summary>
    /// <remarks>
    /// Перевод строки в конце текста открывает ещё одну, пустую, — как в редакторе, где каретку
    /// можно поставить за последний перевод.
    /// </remarks>
    [AvaloniaFact]
    public void The_scroll_reaches_every_line()
    {
        var (view, window) = Shown(Lines(500), "Dark", height: 200);
        var padding = view.Padding;

        Assert.Equal(501, view.Document.LineCount);
        Assert.True(
            Math.Abs(Scroll(view).Extent.Height - (padding.Top + 501 * Line + padding.Bottom)) < 0.01,
            $"протяжённость {Scroll(view).Extent.Height} не покрывает 501 строку");

        window.Close();
    }

    /// <summary>Каретка не встаёт внутрь перевода строки и за конец текста.</summary>
    [AvaloniaFact]
    public void The_caret_never_stands_inside_a_line_break()
    {
        var (view, window) = Shown("ab\r\ncd", "Dark");

        view.CaretOffset = 3;
        Assert.Equal(2, view.CaretOffset);

        view.CaretOffset = 99;
        Assert.Equal(6, view.CaretOffset);

        window.Close();
    }

    /// <summary>
    /// Стрелки ведут каретку по тексту, а по вертикали она держит колонку, с которой ушла.
    /// </summary>
    /// <remarks>
    /// Короткая строка посередине сажает каретку в свой конец, но следующая длинная возвращает ей
    /// прежнюю колонку — как в любом редакторе.
    /// </remarks>
    [AvaloniaFact]
    public void Arrows_walk_the_text_and_keep_the_column()
    {
        var (view, window) = Shown("abcdef\nab\nabcdef", "Dark");

        view.Focus();
        view.CaretOffset = 4;

        Press(window, Key.Down);
        Assert.Equal(9, view.CaretOffset);

        Press(window, Key.Down);
        Assert.Equal(14, view.CaretOffset);

        Press(window, Key.Right);
        Assert.Equal(15, view.CaretOffset);

        Press(window, Key.Up);
        Press(window, Key.Up);
        Assert.Equal(5, view.CaretOffset);

        Press(window, Key.End, RawInputModifiers.Control);
        Assert.Equal(view.Text!.Length, view.CaretOffset);

        Press(window, Key.Home, RawInputModifiers.Control);
        Assert.Equal(0, view.CaretOffset);

        window.Close();
    }

    /// <summary>Home ведёт к началу текста строки, а с него — к её краю.</summary>
    [AvaloniaFact]
    public void Home_goes_to_the_text_and_then_to_the_margin()
    {
        var (view, window) = Shown("    <Button/>", "Dark");

        view.Focus();
        view.CaretOffset = 9;

        Press(window, Key.Home);
        Assert.Equal(4, view.CaretOffset);

        Press(window, Key.Home);
        Assert.Equal(0, view.CaretOffset);

        Press(window, Key.End);
        Assert.Equal(13, view.CaretOffset);

        window.Close();
    }

    /// <summary>Ctrl со стрелкой шагает словами: имя, знаки и пробелы — разного рода.</summary>
    [AvaloniaFact]
    public void Ctrl_arrows_step_over_words()
    {
        var (view, window) = Shown("<Button Content=\"Ok\"/>", "Dark");

        view.Focus();
        view.CaretOffset = 1;

        Press(window, Key.Right, RawInputModifiers.Control);
        Assert.Equal(8, view.CaretOffset);

        Press(window, Key.Right, RawInputModifiers.Control);
        Assert.Equal(15, view.CaretOffset);

        Press(window, Key.Left, RawInputModifiers.Control);
        Assert.Equal(8, view.CaretOffset);

        window.Close();
    }

    /// <summary>Shift тянет выделение, Ctrl+A берёт всё, стрелка без Shift сворачивает его к краю.</summary>
    [AvaloniaFact]
    public void Shift_extends_the_selection_and_ctrl_a_takes_everything()
    {
        var (view, window) = Shown("abc\ndef", "Dark");

        view.Focus();

        Press(window, Key.Right, RawInputModifiers.Shift);
        Press(window, Key.Right, RawInputModifiers.Shift);

        Assert.Equal(new AxCodeRange(0, 2), view.Selection);
        Assert.Equal("ab", view.SelectedText);

        Press(window, Key.A, RawInputModifiers.Control);

        Assert.Equal(new AxCodeRange(0, 7), view.Selection);

        Press(window, Key.Left);

        Assert.True(view.Selection.IsEmpty, "стрелка без Shift не сняла выделение");
        Assert.Equal(0, view.CaretOffset);

        window.Close();
    }

    /// <summary>Каретка, поставленная кодом, и новый текст снимают выделение.</summary>
    [AvaloniaFact]
    public void A_caret_set_by_code_drops_the_selection()
    {
        var (view, window) = Shown("abc\ndef", "Dark");

        view.Select(1, 4);
        Assert.Equal("bc\nd", view.SelectedText);
        Assert.Equal(5, view.CaretOffset);

        view.CaretOffset = 2;

        Assert.True(view.Selection.IsEmpty, "каретка из кода оставила выделение");

        view.Select(0, 3);
        view.Text = "xyz\nuvw";

        Assert.True(view.Selection.IsEmpty, "новый текст оставил выделение старого");

        window.Close();
    }

    /// <summary>Ctrl+C кладёт выделенный текст в буфер обмена.</summary>
    [AvaloniaFact]
    public async Task Ctrl_c_copies_the_selection()
    {
        var (view, window) = Shown("<Button Content=\"Ok\"/>", "Dark");

        view.Focus();
        view.Select(1, 6);

        Press(window, Key.C, RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Button", await window.Clipboard!.TryGetTextAsync());

        window.Close();
    }

    /// <summary>
    /// Щелчок ставит каретку и говорит об этом; каретка из кода молчит.
    /// </summary>
    /// <remarks>
    /// Хозяин отвечает на событие выбором элемента под кареткой и сам ставит отметку. Отвечай
    /// просмотр событием и на каретку из кода, хозяин, ставящий каретку к выбранному на холсте,
    /// ходил бы по кругу.
    /// </remarks>
    [AvaloniaFact]
    public void A_click_places_the_caret_and_tells_but_code_does_not()
    {
        var (view, window) = Shown("first\nsecond\nthird", "Dark");
        var heard = new List<int>();

        view.CaretMoved += (_, e) => heard.Add(e.Offset);

        view.CaretOffset = 3;
        Assert.Empty(heard);

        Click(window, view, line: 1, column: 2);

        Assert.True(view.IsFocused, "щелчок не отдал клавиатуру просмотру");
        Assert.Single(heard);
        Assert.Equal(1, view.Document.LineOf(heard[0]));
        Assert.Equal(heard[0], view.CaretOffset);

        window.Close();
    }

    /// <summary>Каретка, сдвинутая клавишей, тоже говорит о себе — хозяин ведёт выбор за ней.</summary>
    [AvaloniaFact]
    public void A_key_that_moves_the_caret_tells()
    {
        var (view, window) = Shown("first\nsecond", "Dark");
        var heard = new List<int>();

        view.Focus();
        view.CaretMoved += (_, e) => heard.Add(e.Offset);

        Press(window, Key.Down);
        Press(window, Key.Up, RawInputModifiers.Control);

        Assert.Equal([view.CaretOffset], heard);

        window.Close();
    }

    /// <summary>Щелчок по номеру строки берёт строку целиком, с её переводом.</summary>
    [AvaloniaFact]
    public void A_click_on_a_line_number_takes_the_whole_line()
    {
        var (view, window) = Shown("first\nsecond\nthird", "Dark");
        var gutter = view.Lines!.TranslatePoint(new Point(1, view.Padding.Top + 1.5 * Line), window)!.Value;

        window.MouseDown(gutter, MouseButton.Left);
        window.MouseUp(gutter, MouseButton.Left);

        Assert.Equal("second\n", view.SelectedText);

        window.Close();
    }

    /// <summary>Двойной щелчок берёт слово под курсором.</summary>
    [AvaloniaFact]
    public void A_double_click_takes_the_word()
    {
        var (view, window) = Shown("<Button Content=\"Ok\"/>", "Dark");

        Click(window, view, line: 0, column: 10);
        Click(window, view, line: 0, column: 10);

        Assert.Equal("Content", view.SelectedText);

        window.Close();
    }

    /// <summary>
    /// PageDown листает на экран: каретка и текст едут вместе, и каретку видно.
    /// </summary>
    [AvaloniaFact]
    public void Page_down_turns_the_page_with_the_caret()
    {
        var (view, window) = Shown(Lines(1000), "Dark", height: 200);

        view.Focus();
        Press(window, Key.PageDown);
        Press(window, Key.PageDown);
        window.UpdateLayout();

        var line = view.Document.LineOf(view.CaretOffset);
        var top = Scroll(view).Offset.Y;

        Assert.True(line > 10, $"каретка на строке {line} после двух страниц");
        Assert.True(top > 0, "текст не уехал вместе с кареткой");
        Assert.InRange(view.Padding.Top + line * Line - top, 0, 200 - Line);

        window.Close();
    }

    /// <summary>Ctrl со стрелкой листает строку, не трогая каретку.</summary>
    [AvaloniaFact]
    public void Ctrl_down_scrolls_without_the_caret()
    {
        var (view, window) = Shown(Lines(100), "Dark", height: 200);

        view.Focus();
        Press(window, Key.Down, RawInputModifiers.Control);
        window.UpdateLayout();

        Assert.Equal(0, view.CaretOffset);
        Assert.True(Math.Abs(Scroll(view).Offset.Y - Line) < 0.01, $"смещение {Scroll(view).Offset.Y}, а должна быть строка");

        window.Close();
    }

    /// <summary>
    /// Отрезок, ушедший за экран целиком, приезжает посередине; видный — остаётся на месте.
    /// </summary>
    /// <remarks>
    /// Так ведут себя переходы к месту в редакторах: далёкое ставят в центр, чтобы было видно, что
    /// вокруг, а уже видное не дёргают — выбор на холсте не должен трясти текст под глазами.
    /// </remarks>
    [AvaloniaFact]
    public void A_far_range_comes_to_the_middle_and_a_near_one_stays()
    {
        var text = Lines(1000);
        var (view, window) = Shown(text, "Dark", height: 200);
        var start = view.Document.StartOf(500);

        view.ScrollIntoView(new AxCodeRange(start, view.Document.LengthOf(500)));
        window.UpdateLayout();

        var middle = Scroll(view).Offset.Y + 100;
        var line = view.Padding.Top + 500 * Line + Line / 2;

        Assert.True(Math.Abs(line - middle) <= Line, $"строка 500 стоит на {line - Scroll(view).Offset.Y}, а не посередине");

        var before = Scroll(view).Offset;

        view.ScrollIntoView(view.Document.StartOf(499));
        window.UpdateLayout();

        Assert.Equal(before, Scroll(view).Offset);

        window.Close();
    }

    /// <summary>Просьба показать место, данная вместе с текстом, дожидается раскладки.</summary>
    [AvaloniaFact]
    public void A_reveal_asked_before_layout_waits_for_it()
    {
        var view = new AxCodeView { Height = 200 };
        var window = Window(view, "Dark");

        view.Text = Lines(1000);
        view.ScrollIntoView(view.Document.StartOf(700));
        window.UpdateLayout();

        Assert.True(Scroll(view).Offset.Y > 600 * Line, $"просмотр стоит на {Scroll(view).Offset.Y}, а не у строки 700");

        window.Close();
    }

    /// <summary>Tab уходит из просмотра к следующему контролу: только для чтения — не ловушка.</summary>
    [AvaloniaFact]
    public void Tab_leaves_the_view()
    {
        var view = new AxCodeView { Text = Sample };
        var next = new AxButton { Content = "Готово" };
        var window = Window(new StackPanel { Children = { view, next } }, "Dark");

        view.Focus();
        Press(window, Key.Tab);

        Assert.True(next.IsFocused, "Tab остался в просмотре кода");

        window.Close();
    }

    /// <summary>Куски укладываются по порядку, обрезаются по тексту и друг по другу.</summary>
    [Fact]
    public void Spans_are_laid_in_order_and_trimmed()
    {
        var text = AxCodeText.Create("0123456789", [
            new AxCodeSpan(5, 3, AxCodeRole.Tag),
            new AxCodeSpan(0, 2, AxCodeRole.Attribute),
            new AxCodeSpan(1, 3, AxCodeRole.String),
            new AxCodeSpan(8, 10, AxCodeRole.Comment),
            new AxCodeSpan(3, 0, AxCodeRole.Error),
        ]);

        Assert.Equal(
            [
                new AxCodeSpan(0, 2, AxCodeRole.Attribute),
                new AxCodeSpan(2, 2, AxCodeRole.String),
                new AxCodeSpan(5, 3, AxCodeRole.Tag),
                new AxCodeSpan(8, 2, AxCodeRole.Comment),
            ],
            text.SpansIn(0, 10).ToArray());

        Assert.Equal([new AxCodeSpan(5, 3, AxCodeRole.Tag)], text.SpansIn(4, 6).ToArray());
    }

    /// <summary>Строки делятся по любому переводу строки; табуляция считается до своей остановки.</summary>
    [Fact]
    public void Lines_split_on_every_break_and_tabs_count_to_their_stop()
    {
        var text = AxCodeText.Create("a\r\nb\nc\rd\t!", null);

        Assert.Equal(4, text.LineCount);
        Assert.Equal(["a", "b", "c", "d\t!"], Enumerable.Range(0, 4).Select(line => text.LineText(line).ToString()));
        Assert.Equal(5, text.Columns);
        Assert.Equal(0, text.LineOf(2));
        Assert.Equal(1, text.LineOf(3));
    }

    private const string Sample = """
        <Window xmlns="https://github.com/avaloniaui"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
          <!-- Форма -->
          <Button x:Name="Ok" Content="{Binding Title}"/>
        </Window>
        """;

    private static string Lines(int count)
    {
        var text = new StringBuilder();

        for (var line = 0; line < count; line++)
            text.Append("<Item Index=\"").Append(line).Append("\"/>\n");

        return text.ToString();
    }

    private static (AxCodeView View, Window Window) Shown(string text, string variant, double height = double.NaN)
    {
        var view = new AxCodeView { Text = text, Height = height };

        return (view, Window(view, variant));
    }

    private static Window Window(Control content, string variant)
    {
        var window = new Window
        {
            Width = 480,
            Height = 360,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = content,
        };

        window.Show();
        window.UpdateLayout();

        return window;
    }

    private static ScrollViewer Scroll(AxCodeView view) =>
        Assert.IsType<ScrollViewer>(view.Lines!.Parent);

    /// <summary>Проводит кадр: поверхность раскладывает строки, когда рисует их.</summary>
    private static void Rendered()
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private static void Press(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None) =>
        window.KeyPress(key, modifiers, PhysicalKey.None, string.Empty);

    /// <summary>Щелчок в колонку строки: точку называет сама поверхность, по своей раскладке.</summary>
    private static void Click(Window window, AxCodeView view, int line, int column)
    {
        var lines = view.Lines!;
        var point = lines.TranslatePoint(lines.PointOf(view.Document.StartOf(line) + column), window)!.Value;

        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
    }

    private static Color Token(string key, string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        Assert.True(Application.Current!.TryFindResource(key, theme, out var value), $"в теме нет {key}");

        return Assert.IsType<Color>(value);
    }

    private static T Metric<T>(string key)
    {
        Assert.True(Application.Current!.TryFindResource(key, out var value), $"в теме нет {key}");

        return Assert.IsType<T>(value);
    }

    private static Color Colour(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
}
