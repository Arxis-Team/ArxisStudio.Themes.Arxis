using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Контраст обоих вариантов темы.
/// </summary>
/// <remarks>
/// Текст не ниже 4,5:1, иконка не ниже 3:1, замер по фактическому фону. Пары
/// «что на чём» названы здесь по шаблонам темы: текст контрола на его
/// собственной заливке и текст на поверхностях, на которых он стоит.
/// </remarks>
public class ContrastTests
{
    /// <summary>Порог для текста.</summary>
    private const double Readable = 4.5d;

    /// <summary>Порог для иконок и прочей графики.</summary>
    private const double Visible = 3d;

    /// <summary>
    /// Текст контрола читается на его собственной заливке.
    /// </summary>
    /// <remarks>
    /// Поле ввода и выпадающий список пишут основным текстом по фону поля,
    /// акцентная кнопка — текстом на акценте по сильной заливке. Опасная кнопка
    /// своей заливки не имеет и стоит на поверхности хозяина, поэтому меряется
    /// по обеим: одинаково законно она стоит и на фоне окна, и на панели.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("fg", "inp")]
    [InlineData("onacc", "accS")]
    [InlineData("redT", "bg1")]
    [InlineData("redT", "bg2")]
    public void Control_text_reads_on_its_own_fill(string fg, string ground)
    {
        foreach (var variant in new[] { "Light", "Dark" })
        {
            Assert.True(
                Ratio(fg, ground, variant) >= Readable,
                $"{fg} на {ground} [{variant}] даёт {Ratio(fg, ground, variant):F2}:1 при пороге {Readable}");
        }
    }

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
    /// Третичный текст читается лучше выключенного, а не хуже.
    /// </summary>
    /// <remarks>
    /// Порядок, а не порог, и он обязателен. Приглушение — признак
    /// недоступности; выключенный текст, читающийся лучше включённого
    /// второстепенного, говорит человеку обратное тому, что есть. Так и было до
    /// записи 145: в тёмной теме 4,75:1 у выключенного против 3,46 у
    /// третичного, в светлой — один и тот же цвет у обоих.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Tertiary_text_reads_better_than_disabled_text(string variant)
    {
        var tertiary = Ratio("fg3", "bg1", variant);
        var disabled = Ratio("fgDis", "bg1", variant);

        Assert.True(
            tertiary > disabled,
            $"третичный {tertiary:F2}:1 не ярче выключенного {disabled:F2}:1 — смысл состояния перевёрнут");
    }

    /// <summary>
    /// Текст читается на тех поверхностях, где он стоит, — по порогу 4,5.
    /// </summary>
    /// <remarks>
    /// Пары контрола с его заливкой этого не ловят: подпись в боковой колонке
    /// или путь в списке недавних стоит на фоне хозяина, и хозяином бывает
    /// панель. Живой замер нашёл третичный
    /// текст на панели с 3,98:1 в тёмной и 3,46:1 в светлой, второстепенный на
    /// плашке — с 4,28:1.
    /// <para>
    /// Правило по поверхностям: основной текст — на любой, второстепенный — на
    /// основном фоне, панели и плашке, третичный — на основном фоне и панели.
    /// На плашке третичным не пишут: ниже проверено, что он там хотя бы
    /// различим, но читаемым он там не обязан быть и не будет.
    /// </para>
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("fg", "bg1")]
    [InlineData("fg", "bg2")]
    [InlineData("fg", "bg3")]
    [InlineData("fg", "bg4")]
    [InlineData("fg2", "bg1")]
    [InlineData("fg2", "bg2")]
    [InlineData("fg2", "bg3")]
    [InlineData("fg3", "bg1")]
    [InlineData("fg3", "bg2")]
    public void Text_reads_on_the_surfaces_it_stands_on(string fg, string ground)
    {
        foreach (var variant in new[] { "Light", "Dark" })
        {
            Assert.True(
                Ratio(fg, ground, variant) >= Readable,
                $"{fg} на {ground} [{variant}] даёт {Ratio(fg, ground, variant):F2}:1 при пороге {Readable}");
        }
    }

    /// <summary>
    /// Третичный текст на плашке хотя бы различим.
    /// </summary>
    /// <remarks>
    /// Порог 3:1, а не 4,5, и это не послабление, а граница правила выше: на
    /// плашке третичным не пишут то, что нужно прочесть. Но плашка — это и
    /// наведённая строка, и под курсором её подсказка не должна пропадать.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Tertiary_text_stays_visible_on_a_plate(string variant)
        => Assert.True(
            Ratio("fg3", "bg3", variant) >= Visible,
            $"третичный на bg3 [{variant}] даёт {Ratio("fg3", "bg3", variant):F2}:1");

    /// <summary>
    /// Рамка и наведённая плашка — один цвет, и это решение палитры.
    /// </summary>
    /// <remarks>
    /// Здесь закреплено не качество, а намерение. Рамки этой палитры —
    /// разделители, а не опознаватели: контрол опознаётся заливкой, и порога
    /// 3:1 не берёт ни один токен рамки ни на одной поверхности (лучшее —
    /// 2,07:1 у <c>AxBrd2</c> на поле ввода в тёмной теме). Совпадение
    /// <c>AxBrd</c> с <c>AxBg3</c> — крайний случай того же решения: на
    /// наведённой плашке рамка не даёт ни одного своего пикселя.
    /// <para>
    /// Следствие, ради которого тест и стоит: рамкой на плашке <c>AxBg3</c>
    /// ничего не размечают — размечает сама плашка. Понадобится обратное —
    /// понадобится новый токен, и этот тест заставит сказать об этом вслух, а
    /// не подвинуть значение молча.
    /// </para>
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_border_and_the_hover_plate_are_one_colour_on_purpose(string variant)
        => Assert.Equal(1d, Ratio("brd", "bg3", variant), 3);

    /// <summary>
    /// Иконка различима на поверхности, на которой её рисуют.
    /// </summary>
    /// <remarks>
    /// Акцент на залитой плашке сюда не входит: у этого случая свой токен и
    /// свой тест ниже. Всё остальное держит 3:1 само.
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
    /// 3,62 в светлой, на AxSel — 2,29 и 3,28. Здесь правило измерено: AxLinkOn
    /// держит порог на обеих плашках в обоих вариантах.
    ///
    /// Заливки правило не касается: полоса прогресса и заполнение ползунка
    /// остаются на AxAcc — дорожка bg3, заполнение acc, а подпись рядом уже
    /// linkOn.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("bg3", "Light")]
    [InlineData("bg3", "Dark")]
    [InlineData("sel", "Light")]
    [InlineData("sel", "Dark")]
    public void Accent_on_a_plate_uses_the_token_made_for_it(string plate, string variant)
    {
        Assert.True(Ratio("acc", plate, variant) < Visible || variant == "Light",
            "AxAcc внезапно проходит порог — отдельный токен для плашки стоит пересмотреть");

        Assert.True(
            Ratio("linkOn", plate, variant) >= Visible,
            $"AxLinkOn на {plate} [{variant}] даёт {Ratio("linkOn", plate, variant):F2}:1");
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

    /// <summary>Отношение контраста по WCAG между двумя цветами темы.</summary>
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
    /// Цвет темы по короткому имени.
    /// </summary>
    /// <remarks>
    /// Короткие имена держат случаи теста читаемыми в одну строку; таблица
    /// соответствия умещается здесь, потому что с ключами темы имена
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
            "brd" => "AxBrdColor",
            "brd2" => "AxBrd2Color",
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
