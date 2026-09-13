using ArxisStudio.Controls;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Активная панель: та, в которой сейчас клавиатура.
/// </summary>
/// <remarks>
/// У среды с десятком панелей вопрос «куда пойдёт нажатие» должен иметь ответ
/// до нажатия. В Rider и в Unity Editor активная панель названа всегда; здесь
/// этого состояния не было ни в одной теме, и отличить панель с кареткой от
/// соседней было нельзя ничем.
/// <para>
/// Проверяется наблюдаемое: чем покрашена шапка и чем проведена линия под ней.
/// Заголовок для этой роли не годится — в докинге он всегда пуст.
/// </para>
/// </remarks>
public class ToolWindowFocusTests
{
    /// <summary>Панель с кареткой поднимает шапку.</summary>
    /// <remarks>
    /// Ступень фона видно боковым зрением: искать глазами тонкую линию, чтобы
    /// узнать, где ты, — не ответ.
    /// </remarks>
    [AvaloniaFact]
    public void The_panel_holding_the_keyboard_lifts_its_header()
    {
        var (panel, inside, window) = Shown();
        var header = Header(panel);
        var calm = Paint(header.Background);

        Assert.True(inside.Focus(), "контролу внутри панели не досталось фокуса");
        window.UpdateLayout();

        Assert.NotEqual(calm, Paint(header.Background));

        window.Close();
    }

    /// <summary>Линия под шапкой активной панели становится акцентной.</summary>
    /// <remarks>
    /// Точное указание в придачу к ступени фона, и тем же цветом, что у полосы
    /// выбранной вкладки: одно состояние — один цвет.
    /// </remarks>
    [AvaloniaFact]
    public void The_line_under_an_active_header_turns_accent()
    {
        var (panel, inside, window) = Shown();
        var header = Header(panel);
        var calm = Paint(header.BorderBrush);

        Assert.True(inside.Focus());
        window.UpdateLayout();

        var lit = Paint(header.BorderBrush);

        Assert.NotEqual(calm, lit);
        Assert.Equal(Token(panel, "AxAccBrush"), lit);

        window.Close();
    }

    /// <summary>
    /// Панель без каретки выглядит спокойно.
    /// </summary>
    /// <remarks>
    /// Состояние, которое видно всегда, не состояние: две панели, обе
    /// подсвеченные, отвечают на вопрос «куда пойдёт нажатие» так же плохо, как
    /// две одинаково тусклые.
    /// </remarks>
    [AvaloniaFact]
    public void A_panel_without_the_keyboard_stays_calm()
    {
        var (panel, inside, window) = Shown();
        var away = new Border { Focusable = true, Height = 20 };

        ((StackPanel)window.Content!).Children.Add(away);
        window.UpdateLayout();

        Assert.True(inside.Focus());
        window.UpdateLayout();

        var lit = Paint(Header(panel).BorderBrush);

        Assert.True(away.Focus());
        window.UpdateLayout();

        Assert.NotEqual(lit, Paint(Header(panel).BorderBrush));

        window.Close();
    }

    /// <summary>Шапка панели — она в шаблоне одна.</summary>
    private static Border Header(AxToolWindow panel) =>
        panel.GetVisualDescendants().OfType<Border>().Single(border => border.Name == "PART_Header");

    /// <summary>Цвет кисти; <c>null</c> — кисти нет вовсе.</summary>
    private static Color? Paint(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    /// <summary>
    /// Цвет токена темы — того самого, что стоит в словаре.
    /// </summary>
    /// <remarks>
    /// Вариант спрашивается явно: без него поиск не доходит до словарей темы,
    /// где токены и разложены по светлому и тёмному.
    /// </remarks>
    private static Color? Token(Control control, string key) =>
        control.TryFindResource(key, control.ActualThemeVariant, out var found) ? Paint(found as IBrush) : null;

    /// <summary>Панель с местом для каретки, показанная в окне.</summary>
    private static (AxToolWindow Panel, Border Inside, Window Window) Shown()
    {
        var inside = new Border { Focusable = true, Height = 20 };

        var panel = new AxToolWindow
        {
            ShowHeader = true,
            ShowHeaderSeparator = true,
            Title = "Проект",
            Content = inside,
            Width = 240,
            Height = 120,
        };

        var window = new Window { Content = new StackPanel { Children = { panel } }, Width = 400, Height = 300 };

        window.Show();
        window.UpdateLayout();

        return (panel, inside, window);
    }
}
