using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Шкала отступов: ступени, их вторая форма и смысловые имена.
/// </summary>
/// <remarks>
/// Остальные токены темы сверяются с дизайн-проектом, и тест там пересказывает
/// чужое решение. Здесь пересказывать нечего: шкалы у проекта нет — раздел 5
/// перечисляет отступы контролов поштучно, сняв их с макетов, — и шкала
/// принята здесь. Значит этот файл и есть запись решения, а не его проверка.
///
/// Что проверяется по-настоящему, так это правило шага: ступень кратна
/// четырём, а обе половины названы поимённо. Без него шкала обрастёт десяткой
/// и четырнадцаткой за два коммита и перестанет быть шкалой — ровно так, как
/// это уже случилось с числами в разметке.
///
/// Живой проверки — «поменяли токен, интерфейс пошёл за ним», как в
/// <see cref="SizeTokenTests"/>, — здесь пока нет и быть не может: точек
/// вызова у шкалы ноль. Они появляются в миграции, и живая проверка приходит
/// вместе с первой из них.
/// </remarks>
public class SpacingTokenTests
{
    /// <summary>Ступени шкалы снизу вверх и принятые им значения.</summary>
    private static readonly (string Key, double Length)[] Scale =
    [
        ("AxSpaceHair", 2),
        ("AxSpaceTight", 4),
        ("AxSpaceSnug", 6),
        ("AxSpace", 8),
        ("AxSpaceWide", 12),
        ("AxSpaceLoose", 16),
        ("AxSpaceSection", 24),
        ("AxSpaceScreen", 40),
    ];

    /// <summary>
    /// Половины ступени, названные шкалой: волосяной зазор и расстояние от
    /// иконки до подписи. Третьей половины быть не должно.
    /// </summary>
    private static readonly string[] Halves = ["AxSpaceHair", "AxSpaceSnug"];

    /// <summary>Ступени шкалы и принятые им значения.</summary>
    public static TheoryData<string, double> Steps
    {
        get
        {
            var data = new TheoryData<string, double>();

            foreach (var (key, length) in Scale)
                data.Add(key, length);

            return data;
        }
    }

    /// <summary>Смысловое имя и ступень, на которую оно ссылается.</summary>
    private static readonly (string Alias, string Step)[] Named =
    [
        ("AxGapIconText", "AxSpaceSnug"),
        ("AxGapControls", "AxSpace"),
        ("AxGapFormRow", "AxSpaceWide"),
        ("AxGapGroup", "AxSpaceLoose"),
    ];

    /// <summary>Псевдоним и ступень, на которую он ссылается.</summary>
    public static TheoryData<string, string> Aliases
    {
        get
        {
            var data = new TheoryData<string, string>();

            foreach (var (alias, step) in Named)
                data.Add(alias, step);

            return data;
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(Steps))]
    public void A_step_carries_the_value_the_scale_decided(string key, double expected)
    {
        var window = Shown();

        Assert.Equal(expected, Length(window, key));

        window.Close();
    }

    /// <summary>
    /// Ступень кратна четырём, и обе половины названы.
    /// </summary>
    /// <remarks>
    /// Это и есть шкала — остальное её значения. Правило, а не список,
    /// отвечает на вопрос «а можно сюда десятку»: нельзя, и отказ приходит от
    /// теста, а не от памяти того, кто смотрит правку.
    /// </remarks>
    [AvaloniaTheory]
    [MemberData(nameof(Steps))]
    public void A_step_is_a_multiple_of_four_unless_it_is_a_named_half(string key, double expected)
    {
        Assert.Equal(Halves.Contains(key), expected % 4 != 0);
    }

    /// <summary>У ступени две формы, и число в них одно.</summary>
    /// <remarks>
    /// Обе нужны по той же причине, что у контура фокуса: Spacing= берёт
    /// Double, Margin= и Padding= — Thickness. Разъедься они, шкала
    /// показывала бы одно расстояние в стопке и другое в отступе.
    /// </remarks>
    [AvaloniaTheory]
    [MemberData(nameof(Steps))]
    public void Both_forms_of_a_step_carry_one_number(string key, double expected)
    {
        var window = Shown();

        Assert.True(window.TryFindResource(key + "Thickness", window.ActualThemeVariant, out var value));
        Assert.Equal(new Thickness(expected), Assert.IsType<Thickness>(value));

        window.Close();
    }

    /// <summary>Псевдоним — это ступень, а не второе мнение о ней.</summary>
    /// <remarks>
    /// Ради этого псевдонимы и объявлены ссылкой: повтори они число, правка
    /// ступени оставила бы имя на прежнем значении, и разошлись бы они молча.
    /// Здесь видно разошедшееся значение, а саму замену ссылки на число —
    /// <see cref="The_scale_of_the_file_is_the_scale_of_this_test"/>.
    /// </remarks>
    [AvaloniaTheory]
    [MemberData(nameof(Aliases))]
    public void An_alias_is_the_step_it_names(string alias, string step)
    {
        var window = Shown();

        Assert.Equal(Length(window, step), Length(window, alias));

        window.Close();
    }

    /// <summary>
    /// Шкала в файле и шкала в этом тесте — одна шкала.
    /// </summary>
    /// <remarks>
    /// Остальные проверки здесь спрашивают тему по ключу, и то, чего в списке
    /// выше нет, для них не существует: ступень, добавленная в Spacing.axaml
    /// мимо списка, прошла бы и правило шага, и рост шкалы, ни разу не будучи
    /// увиденной. Поэтому список сверяется с объявлениями файла, а не только с
    /// ответами словаря.
    /// <para>
    /// Заодно это единственное место, где видно <b>форму</b> объявления:
    /// псевдоним, переписанный числом вместо ссылки на ступень, словарь
    /// показывает тем же самым значением.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_scale_of_the_file_is_the_scale_of_this_test()
    {
        // Закрывающая кавычка ключа не нужна: [^"]+ и так доходит ровно до неё.
        var declared = new Regex("""<(x:Double|Thickness|StaticResource)\s+x:Key="([^"]+)""")
            .Matches(ThemeSources.Text("Spacing.axaml"))
            .GroupBy(match => match.Groups[1].Value, match => match.Groups[2].Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(key => key, StringComparer.Ordinal).ToList());

        var steps = Scale.Select(step => step.Key).OrderBy(key => key, StringComparer.Ordinal);
        var thicknesses = Scale.Select(step => step.Key + "Thickness").OrderBy(key => key, StringComparer.Ordinal);
        var aliases = Named.Select(name => name.Alias).OrderBy(key => key, StringComparer.Ordinal);

        Assert.Equal(steps, declared.GetValueOrDefault("x:Double", []));
        Assert.Equal(thicknesses, declared.GetValueOrDefault("Thickness", []));
        Assert.Equal(aliases, declared.GetValueOrDefault("StaticResource", []));
    }

    /// <summary>Шкала растёт.</summary>
    [AvaloniaFact]
    public void The_scale_ascends()
    {
        var window = Shown();

        var lengths = Scale.Select(step => Length(window, step.Key)).ToList();

        Assert.Equal(lengths.OrderBy(length => length), lengths);
        Assert.Equal(lengths.Distinct(), lengths);

        window.Close();
    }

    private static double Length(Window window, string key)
    {
        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value), $"нет ключа {key}");

        return Assert.IsType<double>(value);
    }

    private static Window Shown()
    {
        var window = new Window();

        window.Show();

        return window;
    }
}
