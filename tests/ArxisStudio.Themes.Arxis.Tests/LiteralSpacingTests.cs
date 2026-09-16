using System.Text.RegularExpressions;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Храповик: сколько отступов в теме написано числом.
/// </summary>
/// <remarks>
/// Правило репозитория — «значение цвета или размера объявляется в теме и
/// больше нигде» — про саму тему молчит, и в ней это молчание стоило девяноста
/// пяти чисел: один и тот же смысловой зазор написан шестёркой, семёркой и
/// восьмёркой в трёх шаблонах. Заменить их разом нельзя: правка сотни отступов
/// одним коммитом непроверяема на обзоре, непригодна для бисекта, и её
/// регрессии припишут следующему коммиту. Значит — по файлу за раз, а между
/// коммитами кто-то должен держать дверь.
///
/// Держит её этот тест. Число ниже опускается коммитом миграции и не растёт
/// никогда.
///
/// Сверка точная, а не «не больше». Потолок с запасом под ним — это не
/// храповик, а бюджет, и бюджет тратят: перенёс один отступ в токен, написал
/// числом другой — счёт сошёлся, а тема осталась где была. Равенство стоит
/// одной правки в коммите миграции и взамен делает каждое движение видимым.
/// </remarks>
public class LiteralSpacingTests
{
    /// <summary>
    /// Сколько отступов темы сегодня написано числом.
    /// </summary>
    /// <remarks>
    /// Ноль: храповик дошёл до пола в вехе метрик, и теперь это запрет, а не
    /// счёт. Отступ, которому не нашлось ступени, — это или отступ внутрь
    /// контрола, и тогда ему место в Metrics.axaml рядом с AxButtonPadding, или
    /// разговор о самой шкале.
    /// </remarks>
    private const int Ceiling = 0;

    /// <summary>
    /// Объявления отступа: атрибутом и сеттером.
    /// </summary>
    /// <remarks>
    /// Две формы, потому что в теме живут обе. Третьей нет, и это утверждает
    /// <see cref="The_counter_sees_every_declaration"/>: счётчик, не знающий
    /// какой-то формы, молча считал бы нули.
    /// </remarks>
    private static readonly Regex Spacings = new(
        """(?:\b(?:Margin|Padding|Spacing)="([^"]*)")|(?:<Setter\s+Property="(?:Margin|Padding|Spacing)"\s+Value="([^"]*)")""",
        RegexOptions.Compiled);

    /// <summary>
    /// Место, где отступ вообще упомянут: им меряется полнота счётчика.
    /// </summary>
    /// <remarks>
    /// Про форму сеттера здесь не сказано ничего — только что имя свойства
    /// названо. В этом весь смысл: мерить строгий счётчик его же
    /// предположениями бессмысленно, они слепнут вместе. Переставленные
    /// местами <c>Property</c> и <c>Value</c> оба счётчика мимо строгого
    /// прошли бы молча, и первая же проверка поломкой это показала.
    /// </remarks>
    private static readonly Regex Declarations = new(
        """(?:\b(?:Margin|Padding|Spacing)=")|(?:\bProperty="(?:Margin|Padding|Spacing)")""",
        RegexOptions.Compiled);

    /// <summary>Литералов ровно столько, сколько стояло на прошлом коммите.</summary>
    [Fact]
    public void Literal_spacings_are_exactly_as_many_as_the_ceiling_says()
    {
        var counted = ThemeSources.All()
            .Select(source => (source.Name, Count: Literals(source.Text)))
            .Where(row => row.Count > 0)
            .OrderByDescending(row => row.Count)
            .ToList();

        var total = counted.Sum(row => row.Count);
        var worst = string.Join(", ", counted.Take(3).Select(row => $"{row.Name} — {row.Count}"));

        Assert.True(
            total == Ceiling,
            total > Ceiling
                ? $"литералов отступа стало {total} при потолке {Ceiling}: отступ объявляют ступенью шкалы " +
                  $"из Spacing.axaml, а отступ внутрь контрола — ключом в Metrics.axaml. Больше всего в {worst}"
                : $"литералов отступа осталось {total} при потолке {Ceiling}: опустите Ceiling до {total} " +
                  "тем же коммитом, которым их убрали");
    }

    /// <summary>
    /// Счётчик видит каждое объявление отступа.
    /// </summary>
    /// <remarks>
    /// Храповик считает текстом, и любая форма записи, которой он не знает,
    /// становится дырой ровно того размера, сколько в неё влезет. Сеттер со
    /// значением-элементом или с переставленными атрибутами счётчик не
    /// разберёт — и здесь это видно как расхождение, а не как упавший до нуля
    /// счёт.
    /// </remarks>
    [Fact]
    public void The_counter_sees_every_declaration()
    {
        var sources = ThemeSources.All().ToList();

        Assert.NotEmpty(sources);

        foreach (var (name, text) in sources)
        {
            Assert.True(
                Declarations.Count(text) == Spacings.Count(text),
                $"{name}: объявлений отступа {Declarations.Count(text)}, а счётчик разобрал " +
                $"{Spacings.Count(text)} — значит в файле форма записи, которой он не знает");
        }
    }

    /// <summary>
    /// Считает отступы, написанные числом.
    /// </summary>
    /// <remarks>
    /// Ноль не считается: это не выбранное расстояние, а его отсутствие —
    /// «отступа здесь нет», чаще всего поверх унаследованного. Ступени «ноль»
    /// в шкале нет и не будет, спрятать в нём настоящий отступ нельзя, и
    /// держать дюжину таких мест в счёте значит навсегда оставить у храповика
    /// пол, которого он не достигнет.
    /// <para>
    /// Отрицательное считается. Минус три под флажком — такое же выбранное
    /// руками число, как восьмёрка, и у одного из них токен уже есть
    /// (AxFocusOutlineOuterMargin). К тому же в «8,-4,0,-4» настоящая восьмёрка
    /// соседствует с поправкой, и выброси мы такие значения целиком — вот и
    /// дыра.
    /// </para>
    /// </remarks>
    private static int Literals(string text) =>
        Spacings.Matches(text)
            .Select(match => match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value)
            .Count(value => !value.Contains('{') && value.Any(symbol => symbol is >= '1' and <= '9'));
}
