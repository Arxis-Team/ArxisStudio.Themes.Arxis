using System.Text.RegularExpressions;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Запрет: длина, толщина или разрядка в теме, написанная числом.
/// </summary>
/// <remarks>
/// Отступы и скругления держат свои храповики (<see cref="LiteralSpacingTests"/>,
/// <see cref="LiteralRadiusTests"/>), а размеры до вехи метрик не держал никто — и число
/// расползлось: высота вкладки стояла в одном шаблоне, вкладки панели в другом, шапки над ними в
/// третьем, и ни одно из трёх не шло за плотностью. Рамка в пиксель была написана сорок раз.
/// <para>
/// Правило то же, что у цвета: значение объявляется в словаре токенов и больше нигде. Число в
/// шаблоне — это второе место, где живёт решение, и расходится оно с первым молча: на экране
/// видно не «здесь 34, а там 32», а «вкладка чуть выше шапки», и то не сразу.
/// </para>
/// </remarks>
public class LiteralSizeTests
{
    /// <summary>
    /// Сколько длин темы написано числом.
    /// </summary>
    /// <remarks>
    /// Ноль с самого начала: храповик заведён вехой, которая их и убрала. Поднять его нечем —
    /// новой длине место в Metrics.axaml, и там у неё будет имя.
    /// </remarks>
    private const int Ceiling = 0;

    /// <summary>Свойства, которыми задают длину, толщину и разрядку.</summary>
    private const string Properties =
        "Width|Height|MinWidth|MinHeight|MaxWidth|MaxHeight|BorderThickness|StrokeThickness|LetterSpacing";

    /// <summary>Объявления длины: атрибутом и сеттером.</summary>
    private static readonly Regex Sizes = new(
        $"""(?:\b(?:{Properties})="([^"]*)")|(?:<Setter\s+Property="(?:{Properties})"\s+Value="([^"]*)")""",
        RegexOptions.Compiled);

    /// <summary>Место, где длина вообще упомянута: им меряется полнота счётчика.</summary>
    private static readonly Regex Declarations = new(
        $"""(?:\b(?:{Properties})=")|(?:\bProperty="(?:{Properties})")""",
        RegexOptions.Compiled);

    /// <summary>Длин числом ровно столько, сколько говорит потолок.</summary>
    [Fact]
    public void Literal_sizes_are_exactly_as_many_as_the_ceiling_says()
    {
        var counted = ThemeSources.All()
            .Where(source => !Tokens(source.Name))
            .Select(source => (source.Name, Count: Literals(source.Text)))
            .Where(row => row.Count > 0)
            .OrderByDescending(row => row.Count)
            .ToList();

        var total = counted.Sum(row => row.Count);
        var worst = string.Join(", ", counted.Take(3).Select(row => $"{row.Name} — {row.Count}"));

        Assert.True(
            total == Ceiling,
            total > Ceiling
                ? $"длин числом стало {total} при потолке {Ceiling}: длину объявляют ключом в Metrics.axaml, " +
                  $"а высоту хрома — ещё и в обеих ступенях плотности. Больше всего в {worst}"
                : $"длин числом осталось {total} при потолке {Ceiling}: опустите Ceiling до {total} " +
                  "тем же коммитом, которым их убрали");
    }

    /// <summary>Счётчик видит каждое объявление длины.</summary>
    /// <remarks>
    /// Храповик считает текстом, и форма записи, которой он не знает, стала бы дырой ровно того
    /// размера, сколько в неё влезет.
    /// </remarks>
    [Fact]
    public void The_counter_sees_every_declaration()
    {
        var sources = ThemeSources.All().ToList();

        Assert.NotEmpty(sources);

        foreach (var (name, text) in sources)
        {
            Assert.True(
                Declarations.Count(text) == Sizes.Count(text),
                $"{name}: объявлений длины {Declarations.Count(text)}, а счётчик разобрал " +
                $"{Sizes.Count(text)} — значит в файле форма записи, которой он не знает");
        }
    }

    /// <summary>
    /// Словари токенов: в них числам и место.
    /// </summary>
    /// <remarks>
    /// Плотность объявляет те же ключи заново, поэтому ступени тоже словари токенов, а не
    /// исключение из правила.
    /// </remarks>
    private static bool Tokens(string name) =>
        name is "Palette.axaml" or "Metrics.axaml" or "Spacing.axaml" or "Typography.axaml"
            or "Normal.axaml" or "Compact.axaml" or "Comfortable.axaml";

    /// <summary>Считает длины, написанные числом; ноль и «auto» не в счёт.</summary>
    private static int Literals(string text) =>
        Sizes.Matches(text)
            .Select(match => match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value)
            .Count(value => !value.Contains('{') && value.Any(symbol => symbol is >= '1' and <= '9'));
}
