using System.Text.RegularExpressions;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Храповик: сколько скруглений в теме написано числом.
/// </summary>
/// <remarks>
/// Шестёрка стояла числом в пяти кольцах фокуса и в полосе вкладок, восьмёрка и десятка — у
/// бейджа и чипа. Каждое по отдельности было объяснимо, вместе — ровно тот дрейф, от которого
/// шкала и заводилась: одно и то же скругление, написанное в шести местах порознь, расходится в
/// первой же правке одного из них.
/// <para>
/// Сверка точная, по той же причине, что у отступов (<see cref="LiteralSpacingTests"/>): потолок с
/// запасом — это бюджет, и его тратят.
/// </para>
/// </remarks>
public class LiteralRadiusTests
{
    /// <summary>
    /// Сколько скруглений темы сегодня написано числом.
    /// </summary>
    /// <remarks>
    /// Ноль. Последние пять были выведены из соседней геометрии — радиус дорожки в половину её
    /// высоты и четвёрки половин кнопки с разделённым действием, — и в вехе метрик получили свои
    /// ключи: AxCornerRadiusTrack, AxCornerRadiusSplitLeading и AxCornerRadiusSplitTrailing.
    /// </remarks>
    private const int Ceiling = 0;

    /// <summary>Объявления скругления: атрибутом и сеттером.</summary>
    private static readonly Regex Radii = new(
        """(?:\bCornerRadius="([^"]*)")|(?:<Setter\s+Property="CornerRadius"\s+Value="([^"]*)")""",
        RegexOptions.Compiled);

    /// <summary>Место, где скругление вообще упомянуто: им меряется полнота счётчика.</summary>
    private static readonly Regex Declarations = new(
        """(?:\bCornerRadius=")|(?:\bProperty="CornerRadius")""",
        RegexOptions.Compiled);

    /// <summary>Литералов ровно столько, сколько стояло на прошлом коммите.</summary>
    [Fact]
    public void Literal_radii_are_exactly_as_many_as_the_ceiling_says()
    {
        var counted = ThemeSources.All()
            .Select(source => (source.Name, Count: Literals(source.Text)))
            .Where(row => row.Count > 0)
            .OrderByDescending(row => row.Count)
            .ToList();

        var total = counted.Sum(row => row.Count);
        var where = string.Join(", ", counted.Select(row => $"{row.Name} — {row.Count}"));

        Assert.True(
            total == Ceiling,
            total > Ceiling
                ? $"литералов скругления стало {total} при потолке {Ceiling}: скругление объявляют ключом " +
                  $"AxCornerRadius* в Metrics.axaml. Числа стоят в {where}"
                : $"литералов скругления осталось {total} при потолке {Ceiling}: опустите Ceiling до {total} " +
                  "тем же коммитом, которым их убрали");
    }

    /// <summary>Счётчик видит каждое объявление скругления.</summary>
    /// <remarks>
    /// Храповик считает текстом, и форма записи, которой он не знает, стала бы дырой: сеттер с
    /// переставленными атрибутами прошёл бы мимо обоих строгих выражений молча.
    /// </remarks>
    [Fact]
    public void The_counter_sees_every_declaration()
    {
        var sources = ThemeSources.All().ToList();

        Assert.NotEmpty(sources);

        foreach (var (name, text) in sources)
        {
            Assert.True(
                Declarations.Count(text) == Radii.Count(text),
                $"{name}: объявлений скругления {Declarations.Count(text)}, а счётчик разобрал " +
                $"{Radii.Count(text)} — значит в файле форма записи, которой он не знает");
        }
    }

    /// <summary>Считает скругления, написанные числом.</summary>
    /// <remarks>
    /// Ноль не считается: это не выбранное скругление, а его отсутствие — «здесь углы прямые»,
    /// чаще всего поверх унаследованного.
    /// </remarks>
    private static int Literals(string text) =>
        Radii.Matches(text)
            .Select(match => match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value)
            .Count(value => !value.Contains('{') && value.Any(symbol => symbol is >= '1' and <= '9'));
}
