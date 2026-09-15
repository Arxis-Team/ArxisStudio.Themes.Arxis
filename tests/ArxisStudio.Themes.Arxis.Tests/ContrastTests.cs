using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Пороги контраста палитры в обоих вариантах темы.
/// </summary>
/// <remarks>
/// Это таблица порогов дизайн-системы студии (раздел 2.2), записанная тестами: текст
/// меряется по WCAG 1.4.3, граница контрола и графика — по 1.4.11. Роль названа именем без
/// приставки и окончания: <c>TextPrimary</c> — это <c>AxTextPrimaryColor</c>.
/// <para>
/// Наведение и нажатие полупрозрачны и своего цвета на экране не имеют: плашка ложится на ту
/// поверхность, где стоит контрол. Пара с такой плашкой меряется поверх каждой непрозрачной
/// поверхности, и в зачёт идёт худшая.
/// </para>
/// </remarks>
public class ContrastTests
{
    /// <summary>Основной текст на основном фоне и панели.</summary>
    private const double Body = 11d;

    /// <summary>Основной текст на плашке.</summary>
    private const double OnPlate = 7d;

    /// <summary>Порог для текста.</summary>
    private const double Readable = 4.5d;

    /// <summary>Порог для границ контролов, иконок и прочей графики.</summary>
    private const double Visible = 3d;

    private static readonly string[] Variants = ["Light", "Dark"];

    /// <summary>Непрозрачные поверхности, на которых стоят контролы.</summary>
    private static readonly string[] Surfaces = ["SurfaceBase", "SurfacePanel", "SurfaceOverlay", "SurfaceRaised"];

    /// <summary>Заливки сообщений: на них пишут и основным текстом, и ссылкой.</summary>
    private static readonly string[] MessageFills = ["InfoFill", "SuccessFill", "WarningFill", "ErrorFill"];

    /// <summary>
    /// Основной текст: 11:1 на основном фоне и панели, 7:1 на любой плашке.
    /// </summary>
    /// <remarks>
    /// Основной текст — то, что читают подолгу: код, имена, значения. Плашка — наведённая или
    /// выбранная строка, подсказка, сообщение — держит порог ниже, но всё ещё с запасом над
    /// 4,5: на ней пишут тот же основной текст, а не подпись.
    /// </remarks>
    [AvaloniaFact]
    public void Primary_text_reads_on_every_surface()
    {
        AtLeast(Body, "TextPrimary", "SurfaceBase", "SurfacePanel");
        AtLeast(OnPlate, "TextPrimary",
            ["SurfaceOverlay", "SurfaceRaised", "Hover", "Pressed", "SelectionActive", "SelectionInactive", "ToolTipFill", .. MessageFills]);
    }

    /// <summary>
    /// Второстепенный текст читается и на поверхностях, и на плашках.
    /// </summary>
    /// <remarks>
    /// Подпись в боковой колонке, путь в строке недавних, описание под заголовком стоят на фоне
    /// хозяина, и хозяином бывает любая поверхность — а под курсором или в выделении строка
    /// становится плашкой.
    /// </remarks>
    [AvaloniaFact]
    public void Secondary_text_reads_on_surfaces_and_plates() =>
        AtLeast(Readable, "TextSecondary",
            [.. Surfaces, "Hover", "SelectionActive", "SelectionInactive", "ToolTipFill", .. MessageFills]);

    /// <summary>
    /// Третичный текст читается на поверхностях и хотя бы различим на плашке.
    /// </summary>
    /// <remarks>
    /// Порог 3:1 на плашке — не послабление, а граница правила: на плашке третичным не пишут то,
    /// что нужно прочесть. Но плашка — это и наведённая строка, и под курсором её подсказка не
    /// должна пропадать.
    /// </remarks>
    [AvaloniaFact]
    public void Tertiary_text_reads_on_surfaces_and_stays_visible_on_plates()
    {
        AtLeast(Readable, "TextTertiary", "SurfaceBase", "SurfacePanel", "SurfaceOverlay");
        AtLeast(Visible, "TextTertiary", "SurfaceRaised", "Hover", "SelectionActive", "SelectionInactive");
    }

    /// <summary>
    /// Выключенный текст приглушён заметно сильнее третичного — и всё ещё различим.
    /// </summary>
    /// <remarks>
    /// WCAG 1.4.3 выводит выключенный контрол из-под требования контраста, и не по недосмотру:
    /// низкий контраст здесь и есть признак недоступности. Но приглушение обязано читаться как
    /// приглушение: выключенный текст, близкий к третичному, говорит человеку, что контрол
    /// работает. Так и было до записи 145 — в тёмной теме выключенный давал 4,75:1 против 3,46 у
    /// третичного. Отсюда порог: не ярче 0,6 отношения третичного. А «освобождён» не значит
    /// «невидим» — на заливке выключенного поля текст различим.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Disabled_text_is_dimmed_well_below_tertiary(string variant)
    {
        var tertiary = Ratio("TextTertiary", "SurfaceBase", variant);
        var disabled = Ratio("TextDisabled", "SurfaceBase", variant);

        Assert.True(
            disabled <= tertiary * 0.6d,
            $"выключенный {disabled:F2}:1 слишком близок к третичному {tertiary:F2}:1 [{variant}] — приглушение не читается");
        Assert.True(
            Ratio("TextDisabled", "FillDisabled", variant) > 1.5d,
            $"выключенный текст на заливке выключенного поля неразличим вовсе [{variant}]");
    }

    /// <summary>
    /// Контрол узнаётся по границе: 3:1 к основному фону и панели.
    /// </summary>
    /// <remarks>
    /// WCAG 1.4.11. До палитры второго поколения рамки служили только разделителями, лучший
    /// токен рамки давал 2,07:1, и поле ввода на панели опознавалось одной заливкой — на белом
    /// документе в светлой теме не опознавалось никак. Граница сильнее контрольной обязана и
    /// читаться сильнее: иначе её имя обманывает.
    /// </remarks>
    [AvaloniaFact]
    public void Control_border_identifies_the_control()
    {
        AtLeast(Visible, "StrokeControl", "SurfaceBase", "SurfacePanel");

        foreach (var variant in Variants)
        {
            foreach (var surface in new[] { "SurfaceBase", "SurfacePanel" })
            {
                Assert.True(
                    Ratio("StrokeStrong", surface, variant) > Ratio("StrokeControl", surface, variant),
                    $"StrokeStrong на {surface} [{variant}] не сильнее StrokeControl");
            }
        }
    }

    /// <summary>Кольцо фокуса видно на поверхностях, где стоят контролы.</summary>
    [AvaloniaFact]
    public void Focus_ring_is_visible() =>
        AtLeast(Visible, "FocusRing", "SurfaceBase", "SurfacePanel", "SurfaceOverlay");

    /// <summary>
    /// Акцентный текст — ссылка во всех состояниях — читается на поверхностях.
    /// </summary>
    /// <remarks>
    /// Роль акцентного текста несёт <c>AxLink</c>: он подобран под текст, а <c>AxAccent</c> —
    /// под графику. Отдельного ключа с тем же значением нет намеренно: синоним разошёлся бы с
    /// оригиналом при первой же настройке.
    /// </remarks>
    [AvaloniaFact]
    public void Accent_text_reads_on_surfaces()
    {
        AtLeast(Readable, "Link", "SurfaceBase", "SurfacePanel", "SurfaceOverlay");
        AtLeast(Readable, "LinkHover", "SurfaceBase", "SurfacePanel");
        AtLeast(Readable, "LinkPressed", "SurfaceBase", "SurfacePanel");
        AtLeast(Readable, "LinkVisited", "SurfaceBase", "SurfacePanel");
    }

    /// <summary>
    /// На залитой плашке акцентный текст берёт <c>AxLinkOnPlate</c> и читается.
    /// </summary>
    /// <remarks>
    /// Плашка — наведённая строка, выбранная строка, поднятая шапка, сообщение. Обычная ссылка
    /// на них теряет порог, ради этого шаг и заведён.
    /// </remarks>
    [AvaloniaFact]
    public void Accent_text_on_a_plate_takes_the_plate_token() =>
        AtLeast(Readable, "LinkOnPlate", ["Hover", "SurfaceRaised", "SelectionActive", .. MessageFills]);

    /// <summary>
    /// Текст на акцентной заливке читается; знак на акцентной графике различим.
    /// </summary>
    /// <remarks>
    /// Подпись основной кнопки — текст, 4,5:1 во всех трёх её состояниях. Галочка флажка и точка
    /// переключателя — знак, им довольно 3:1, поэтому их заливка — графика <c>AxAccent</c>, а не
    /// заливка кнопки. Отмеченный флажок с ошибкой заливается цветом ошибки, и знак на нём тоже
    /// различим.
    /// </remarks>
    [AvaloniaFact]
    public void Text_on_accent_reads()
    {
        AtLeast(Readable, "TextOnAccent", "AccentFill", "AccentFillHover", "AccentFillPressed");
        AtLeast(Visible, "TextOnAccent", "Accent", "AccentHover", "Error");
    }

    /// <summary>
    /// Акцентная графика видна там, где её рисуют.
    /// </summary>
    /// <remarks>
    /// Линия выбранной вкладки и активной панели, дорожка хода, каретка — на поверхностях;
    /// глиф включённой кнопки полосы — на выделении; заполнение ползунка — на дорожке; значок
    /// сведения — на заливке сообщения. Основная кнопка на поверхности узнаётся заливкой.
    /// </remarks>
    [AvaloniaFact]
    public void Accent_graphics_stay_visible()
    {
        AtLeast(Visible, "Accent", "SurfaceBase", "SurfacePanel", "SurfaceRaised", "SelectionActive", "Track", "InfoFill");
        AtLeast(Visible, "AccentHover", "SurfaceBase", "SurfacePanel");
        AtLeast(Visible, "AccentFill", "SurfaceBase", "SurfacePanel");
    }

    /// <summary>
    /// Текст состояний читается на поверхностях и на заливке своего сообщения.
    /// </summary>
    [AvaloniaFact]
    public void Status_text_reads()
    {
        foreach (var status in new[] { "Error", "Warning", "Success" })
            AtLeast(Readable, status + "Text", "SurfaceBase", "SurfacePanel", status + "Fill");
    }

    /// <summary>
    /// Графика и контур состояний видны на поверхностях; графика — и на заливке сообщения.
    /// </summary>
    [AvaloniaFact]
    public void Status_graphics_stay_visible()
    {
        foreach (var status in new[] { "Error", "Warning", "Success" })
            AtLeast(Visible, status, "SurfaceBase", "SurfacePanel", status + "Fill");

        AtLeast(Visible, "ErrorOutline", "SurfaceBase", "SurfacePanel");
        AtLeast(Visible, "WarningOutline", "SurfaceBase", "SurfacePanel");
    }

    /// <summary>Оттенки значков типов видны; инициалы на плитках монограмм читаются.</summary>
    [AvaloniaFact]
    public void Tints_and_monograms_keep_their_ratios()
    {
        AtLeast(Visible, "TintOrange", "SurfaceBase", "SurfacePanel");
        AtLeast(Visible, "TintPurple", "SurfaceBase", "SurfacePanel");
        AtLeast(Readable, "TextOnAccent", "MonogramOrange", "MonogramGreen", "MonogramPurple", "MonogramRed");
    }

    /// <summary>Подсветка кода читается на утопленной полосе блока кода.</summary>
    [AvaloniaFact]
    public void Code_reads_on_its_block()
    {
        foreach (var role in new[] { "CodeText", "CodeTag", "CodeAttribute", "CodeString", "CodeComment" })
            AtLeast(Readable, role, "SurfaceSunken");
    }

    /// <summary>
    /// Наведение — одна ступень на любой поверхности; нажатие и неактивное выделение заметнее.
    /// </summary>
    /// <remarks>
    /// Непрозрачная плашка наведения, подобранная под панель, на всплывающем окне давала 1,03:1
    /// при 1,13 на панели — строка в выпадающем списке под курсором не менялась. Прозрачная
    /// плашка даёт почти тот же шаг везде, разброс — только от яркости самой поверхности; порог
    /// в три четверти шага на панели ловит возврат к непрозрачной.
    /// <para>
    /// Нажатие заметнее наведения на любой поверхности, иначе нажатия не видно. Неактивное
    /// выделение заметнее наведения: строка, отмеченная в списке без фокуса, не должна
    /// выглядеть строкой под курсором.
    /// </para>
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Hover_is_one_step_on_every_surface(string variant)
    {
        var onPanel = Step("Hover", "SurfacePanel", variant);

        foreach (var surface in Surfaces)
        {
            var hover = Step("Hover", surface, variant);

            Assert.True(
                hover - 1d >= (onPanel - 1d) * 0.75d,
                $"наведение на {surface} [{variant}] даёт {hover:F2}:1 против {onPanel:F2} на панели");
            Assert.True(
                Step("Pressed", surface, variant) > hover,
                $"нажатие на {surface} [{variant}] не заметнее наведения");
        }

        Assert.True(
            Step("SelectionInactive", "SurfacePanel", variant) > onPanel,
            $"неактивное выделение [{variant}] не заметнее наведения — отмеченная строка неотличима от строки под курсором");
    }

    /// <summary>Проверяет порог для роли на каждом из фонов в обоих вариантах.</summary>
    private static void AtLeast(double threshold, string foreground, params string[] grounds)
    {
        foreach (var variant in Variants)
        {
            foreach (var ground in grounds)
            {
                var (ratio, surface) = Worst(foreground, ground, variant);
                var placed = surface is null ? string.Empty : $" поверх {surface}";

                Assert.True(
                    ratio >= threshold,
                    $"{foreground} на {ground}{placed} [{variant}] даёт {ratio:F2}:1 при пороге {threshold}");
            }
        }
    }

    /// <summary>
    /// Худшее отношение пары: прозрачный фон кладётся на каждую непрозрачную поверхность.
    /// </summary>
    private static (double Ratio, string? Surface) Worst(string foreground, string ground, string variant)
    {
        var front = Colour(foreground, variant);

        Assert.True(front.A == byte.MaxValue, $"{foreground} прозрачен — передний план меряется только непрозрачным");

        var back = Colour(ground, variant);

        if (back.A == byte.MaxValue)
            return (Ratio(front, back), null);

        return Surfaces
            .Select(surface => (Ratio(front, Over(back, Colour(surface, variant))), (string?)surface))
            .MinBy(pair => pair.Item1);
    }

    /// <summary>Шаг плашки на поверхности: отношение плашки, положенной на неё, к ней самой.</summary>
    private static double Step(string plate, string surface, string variant)
    {
        var ground = Colour(surface, variant);

        return Ratio(Over(Colour(plate, variant), ground), ground);
    }

    private static double Ratio(string first, string second, string variant) =>
        Ratio(Colour(first, variant), Colour(second, variant));

    /// <summary>Отношение контраста по WCAG между двумя непрозрачными цветами.</summary>
    private static double Ratio(Color first, Color second)
    {
        var a = Luminance(first);
        var b = Luminance(second);

        return (Math.Max(a, b) + 0.05d) / (Math.Min(a, b) + 0.05d);
    }

    /// <summary>Цвет, положенный с его прозрачностью на непрозрачный.</summary>
    private static Color Over(Color top, Color bottom)
    {
        var alpha = top.A / 255d;

        byte Mix(byte front, byte back) => (byte)Math.Round(front * alpha + back * (1d - alpha));

        return Color.FromRgb(Mix(top.R, bottom.R), Mix(top.G, bottom.G), Mix(top.B, bottom.B));
    }

    /// <summary>Цвет роли в варианте темы.</summary>
    private static Color Colour(string role, string variant)
    {
        var key = $"Ax{role}Color";
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        Assert.True(Application.Current!.TryFindResource(key, theme, out var value), $"в теме нет {key}");

        return Assert.IsType<Color>(value);
    }

    /// <summary>Относительная яркость по WCAG.</summary>
    private static double Luminance(Color colour)
        => 0.2126d * Channel(colour.R) + 0.7152d * Channel(colour.G) + 0.0722d * Channel(colour.B);

    private static double Channel(byte value)
    {
        var part = value / 255d;

        return part <= 0.03928d ? part / 12.92d : Math.Pow((part + 0.055d) / 1.055d, 2.4d);
    }
}
