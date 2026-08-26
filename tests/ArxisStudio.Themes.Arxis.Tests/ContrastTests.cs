using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Контраст обоих вариантов темы — вторая половина пункта 9 приёмки.
/// </summary>
/// <remarks>
/// Правило раздела 12: текст не ниже 4,5:1, иконка не ниже 3:1, замер по
/// фактическому фону. Фактический фон здесь берётся не на глаз: пары «что на
/// чём» приходят из карты состояний дизайн-проекта — компонент сам говорит,
/// какой переменной красит фон и какой текст в каждом состоянии.
///
/// Цвета читаются из темы, а пары — из проекта. Поэтому тест ловит и правку
/// палитры, которая уронит контраст, и правку проекта, которая сведёт вместе
/// пару, раньше не встречавшуюся.
/// </remarks>
public class ContrastTests
{
    /// <summary>Порог для текста.</summary>
    private const double Readable = 4.5d;

    /// <summary>Порог для иконок и прочей графики.</summary>
    private const double Visible = 3d;

    /// <summary>
    /// Текст читается на своём фоне.
    /// </summary>
    /// <remarks>
    /// Прозрачный фон означает, что контрол стоит на поверхности хозяина, и
    /// меряется он тогда по обеим: кнопка одинаково законно стоит и на фоне
    /// окна, и на панели.
    /// </remarks>
    [AvaloniaTheory]
    [MemberData(nameof(TextPairs))]
    public void Text_keeps_the_readable_ratio(string state, string variant, string fg, string bg)
        => Assert.True(
            Ratio(fg, bg, variant) >= Readable,
            $"{state} [{variant}]: {fg} на {bg} даёт {Ratio(fg, bg, variant):F2}:1");

    /// <summary>
    /// Выключенный текст приглушён намеренно — и это единственное исключение.
    /// </summary>
    /// <remarks>
    /// WCAG 1.4.3 выводит выключенный контрол из-под требования контраста, и не
    /// по недосмотру: низкий контраст здесь и есть признак недоступности. Но
    /// «освобождён» не значит «любой» — приглушение обязано быть видно, иначе
    /// выключенное неотличимо от обычного. Это тест и проверяет.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Disabled_text_is_dimmer_than_ordinary_text(string variant)
    {
        var disabled = Ratio("fgDis", "inpDis", variant);
        var ordinary = Ratio("fg", "inp", variant);

        Assert.True(disabled < ordinary, $"выключенный текст не приглушён: {disabled:F2} против {ordinary:F2}");
        Assert.True(disabled > 1.5d, $"выключенный текст неразличим вовсе: {disabled:F2}:1");
    }

    /// <summary>
    /// Иконка различима на поверхности, на которой её рисуют.
    /// </summary>
    /// <remarks>
    /// Акцент на залитой плашке сюда не входит: раздел 6 приёмки развёл этот
    /// случай отдельно — см. тест ниже. Всё остальное держит 3:1 само.
    /// </remarks>
    [AvaloniaTheory]
    [MemberData(nameof(IconPairs))]
    public void Icon_keeps_the_visible_ratio(string fg, string bg, string variant)
        => Assert.True(
            Ratio(fg, bg, variant) >= Visible,
            $"{fg} на {bg} [{variant}] даёт {Ratio(fg, bg, variant):F2}:1");

    /// <summary>
    /// На залитой плашке акцентный текст и иконка берут AxLinkOn.
    /// </summary>
    /// <remarks>
    /// Ради этого токен и заведён: AxAcc на AxBg3 даёт 2,62:1 в тёмной теме и
    /// 3,62 в светлой, на AxSel — 2,29 и 3,28. Раздел 6 приёмки записывает
    /// правило прямо, и здесь оно измерено: AxLinkOn держит порог на обеих
    /// плашках в обоих вариантах.
    ///
    /// Заливки правило не касается: полоса прогресса и заполнение ползунка
    /// остаются на AxAcc, потому что так их красит сам проект — дорожка bg3,
    /// заполнение acc, а подпись рядом уже linkOn.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("bg3", "Light")]
    [InlineData("bg3", "Dark")]
    [InlineData("sel", "Light")]
    [InlineData("sel", "Dark")]
    public void Accent_on_a_plate_uses_the_token_made_for_it(string plate, string variant)
    {
        Assert.True(Ratio("acc", plate, variant) < Visible || variant == "Light",
            "AxAcc внезапно проходит порог — правило раздела 6 стоит перечитать");

        Assert.True(
            Ratio("linkOn", plate, variant) >= Visible,
            $"AxLinkOn на {plate} [{variant}] даёт {Ratio("linkOn", plate, variant):F2}:1");
    }

    public static TheoryData<string, string, string, string> TextPairs
    {
        get
        {
            var design = DesignProject.Load();
            var data = new TheoryData<string, string, string, string>();

            foreach (var (state, declared) in design.States.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                // Выключенное состояние освобождено — у него свой тест выше.
                if (state.Contains("Disabled", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!declared.TryGetValue("color", out var fg) || !design.Variables.ContainsKey(fg))
                    continue;

                var grounds = declared.TryGetValue("background", out var bg) && design.Variables.ContainsKey(bg)
                    ? [bg]
                    : new[] { "bg1", "bg2" };

                foreach (var ground in grounds)
                {
                    data.Add(state, "Light", fg, ground);
                    data.Add(state, "Dark", fg, ground);
                }
            }

            return data;
        }
    }

    public static TheoryData<string, string, string> IconPairs
    {
        get
        {
            var data = new TheoryData<string, string, string>();

            // Цвета, которыми красят глифы, и поверхности, на которых их рисуют.
            foreach (var fg in new[] { "fg", "fg2", "grn", "red", "yel", "org", "pur" })
            {
                foreach (var bg in new[] { "bg1", "bg2", "bg3" })
                {
                    data.Add(fg, bg, "Light");
                    data.Add(fg, bg, "Dark");
                }
            }

            // Акцент — только там, где он не на плашке: плашку разбирает
            // отдельный тест.
            data.Add("acc", "bg1", "Light");
            data.Add("acc", "bg1", "Dark");
            data.Add("acc", "bg2", "Light");
            data.Add("acc", "bg2", "Dark");

            return data;
        }
    }

    /// <summary>Отношение контраста по WCAG между двумя переменными макетов.</summary>
    private static double Ratio(string first, string second, string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        var window = new Window();
        window.Show();

        var a = Luminance(Colour(window, first, theme));
        var b = Luminance(Colour(window, second, theme));

        window.Close();

        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    /// <summary>
    /// Цвет переменной макета, взятый из темы.
    /// </summary>
    /// <remarks>
    /// Проект называет цвета короткими именами CSS, тема — токенами; таблица
    /// соответствия одна на оба теста и умещается здесь, потому что имена
    /// расходятся только регистром и приставкой.
    /// </remarks>
    private static Color Colour(Window window, string variable, ThemeVariant theme)
    {
        var key = variable switch
        {
            "fg" => "AxFgColor",
            "fg2" => "AxFg2Color",
            "fg3" => "AxFg3Color",
            "fgDis" => "AxFgDisabledColor",
            "bg1" => "AxBg1Color",
            "bg2" => "AxBg2Color",
            "bg3" => "AxBg3Color",
            "bg4" => "AxBg4Color",
            "inp" => "AxInpColor",
            "inpDis" => "AxInpDisabledColor",
            "acc" => "AxAccColor",
            "accS" => "AxAccStrongColor",
            "accSH" => "AxAccStrongHoverColor",
            "accP" => "AxAccPressedColor",
            "onacc" => "AxOnAccColor",
            "sel" => "AxSelColor",
            "linkOn" => "AxLinkOnColor",
            "redT" => "AxRedTextColor",
            "grnT" => "AxGreenTextColor",
            "yelT" => "AxYellowTextColor",
            "grn" => "AxGrnColor",
            "red" => "AxRedColor",
            "yel" => "AxYelColor",
            "org" => "AxOrgColor",
            "pur" => "AxPurColor",
            "outF" => "AxOutlineFocusedColor",
            "outE" => "AxOutlineErrorColor",
            _ => throw new ArgumentException($"нет токена под переменную {variable}"),
        };

        Assert.True(window.TryFindResource(key, theme, out var value), key);

        return (Color)value!;
    }

    /// <summary>Относительная яркость по WCAG.</summary>
    private static double Luminance(Color colour)
        => 0.2126 * Channel(colour.R) + 0.7152 * Channel(colour.G) + 0.0722 * Channel(colour.B);

    private static double Channel(byte value)
    {
        var part = value / 255d;

        return part <= 0.03928d ? part / 12.92d : Math.Pow((part + 0.055d) / 1.055d, 2.4d);
    }
}
