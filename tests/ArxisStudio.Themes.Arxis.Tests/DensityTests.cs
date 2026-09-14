using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Плотность интерфейса: три ступени поверх темы.
/// </summary>
/// <remarks>
/// Ступень — словарь переопределений, а не вторая тема. Обычная пуста: её
/// значения объявлены в Metrics.axaml и Spacing.axaml, и повторить их собой
/// значило бы завести второе место, где они написаны.
/// <para>
/// Проверяется здесь не то, что числа именно такие — их выбрали, — а три
/// правила, без которых ступень тихо разойдётся с темой: состав словаря
/// (ничего лишнего и ничего забытого), порядок ступеней и то, что направленный
/// зазор берёт величину своей ступени, а не соседней.
/// </para>
/// </remarks>
public class DensityTests
{
    /// <summary>
    /// Что плотность двигает помимо шкалы расстояний.
    /// </summary>
    /// <remarks>
    /// Список закрытый, и в этом его смысл: всё остальное в теме плотности не
    /// подчиняется. Контур фокуса, скругления, цели попадания (флажок, тумблер,
    /// полоса разделителя, ширина скроллбара), размер иконки и отступы
    /// поверхностей — каждое со своей причиной, и причины записаны в шапке
    /// Compact.axaml.
    /// </remarks>
    private static readonly string[] Sized =
    [
        "AxRowHeight",
        "AxControlHeight",
        "AxControlHeightCompact",
        "AxControlHeightSmall",
        "AxButtonMinWidth",
        "AxButtonMinWidthCompact",
        "AxDialogButtonMinWidth",
        "AxButtonPadding",
        "AxTextFieldPadding",
        "AxComboBoxPadding",
    ];

    private static readonly string[] Tiers = ["Compact.axaml", "Comfortable.axaml"];

    private static readonly string[] Ends = ["Leading", "Trailing", "Above", "Below", "Sides", "Ends"];

    private static readonly Regex Declaration = new(
        """<(x:Double|Thickness|StaticResource)\s+x:Key="([^"]+)"(?:\s+ResourceKey="([^"]+)"\s*/>|>([^<]*)<)""",
        RegexOptions.Compiled);

    /// <summary>Обычная ступень ничего не переопределяет.</summary>
    /// <remarks>
    /// Пустой файл нужен не теме, а коду: у переключателя три ступени и ни
    /// одного особого случая. Ступень без словаря заставила бы писать «если
    /// обычная — ничего не подмешивать», и первая же правка этого условия
    /// сломала бы возврат к обычной.
    /// </remarks>
    [Fact]
    public void The_normal_tier_overrides_nothing()
    {
        Assert.Empty(Declared("Normal.axaml"));
    }

    /// <summary>
    /// Ступень объявляет ровно то, что плотности подчиняется.
    /// </summary>
    /// <remarks>
    /// Сверка точная в обе стороны сразу. Забытый ключ — это значение, которое
    /// в плотной ступени осталось обычным, и заметить такое на глаз можно
    /// только зная, что искать. Лишний ключ — это скругление или цель
    /// попадания, уехавшая вместе с шагом строки; список выше говорит, чего
    /// плотность не касается, и здесь это проверяется, а не подразумевается.
    /// </remarks>
    [Theory]
    [InlineData("Compact.axaml")]
    [InlineData("Comfortable.axaml")]
    public void A_tier_declares_exactly_what_density_moves(string tier)
    {
        var expected = Sized.Concat(Declared("Spacing.axaml").Keys)
            .OrderBy(key => key, StringComparer.Ordinal);

        Assert.Equal(expected, Declared(tier).Keys.OrderBy(key => key, StringComparer.Ordinal));
    }

    /// <summary>
    /// Плотная ступень не больше обычной, а просторная не меньше.
    /// </summary>
    /// <remarks>
    /// Равенство допускается, и одно такое есть: волосяной зазор в плотной
    /// ступени остаётся двойкой. Ниже двух пикселей зазора нет вовсе — шкала у
    /// своего пола выпрямляется, а не выдумывает ступень в один пиксель.
    /// </remarks>
    [Fact]
    public void Compact_is_no_larger_than_normal_and_comfortable_no_smaller()
    {
        var tight = Declared("Compact.axaml");
        var loose = Declared("Comfortable.axaml");
        var moved = 0;

        foreach (var (key, middle) in Base())
        {
            var less = Numbers(tight[key]);
            var more = Numbers(loose[key]);
            var normal = Numbers(middle);

            for (var side = 0; side < normal.Count; side++)
            {
                Assert.True(less[side] <= normal[side], $"{key}: плотная {less[side]} больше обычной {normal[side]}");
                Assert.True(more[side] >= normal[side], $"{key}: просторная {more[side]} меньше обычной {normal[side]}");
            }

            if (less.Zip(more).Any(pair => pair.First != pair.Second))
                moved++;
        }

        // Ступень, не сдвинувшая почти ничего, — это настройка без последствий.
        Assert.True(moved > 20, $"плотность двигает всего {moved} значений — это не ступень, а видимость");
    }

    /// <summary>
    /// Направленный зазор несёт величину своей ступени.
    /// </summary>
    /// <remarks>
    /// Имя механическое — ступень плюс сторона, — и оно обещает величину.
    /// Обещание это соблюдается руками в трёх местах: в самой шкале и в двух
    /// ступенях плотности. Здесь оно проверяется, иначе <c>AxSpaceWideLeading</c>
    /// в плотной ступени мог бы нести восьмёрку от соседней и не сказать об
    /// этом ничем.
    /// </remarks>
    [Theory]
    [InlineData("Spacing.axaml")]
    [InlineData("Compact.axaml")]
    [InlineData("Comfortable.axaml")]
    public void A_directed_gap_carries_the_value_of_its_own_step(string file)
    {
        var declared = Declared(file);
        var checkedGaps = 0;

        foreach (var (key, value) in declared)
        {
            if (!key.EndsWith("Thickness", StringComparison.Ordinal))
                continue;

            var name = key[..^"Thickness".Length];
            var side = Ends.FirstOrDefault(end => name.EndsWith(end, StringComparison.Ordinal));
            var step = side is null ? name : name[..^side.Length];

            // Смысловое имя ссылается на ступень и своей величины не несёт:
            // разбирать его как «ступень плюс сторона» нечего.
            if (!declared.TryGetValue(step, out var length) ||
                !double.TryParse(length, CultureInfo.InvariantCulture, out var expected))
                continue;

            Assert.Equal(Shape(side, expected), Numbers(value));
            checkedGaps++;
        }

        Assert.True(checkedGaps >= 20, $"{file}: сверено всего {checkedGaps} зазоров — разбор имён сломался");
    }

    /// <summary>Плотная ступень укорачивает контрол, просторная удлиняет.</summary>
    /// <remarks>
    /// Всё выше читает текст. Здесь ступень подмешивают в живое окно и смотрят
    /// на кнопку: словарь, который ни на что не влияет, весь разбор имён прошёл
    /// бы и остался бы мёртвым.
    /// <para>
    /// Кнопка, а не строка списка: у строки высота своя только пока текст в неё
    /// помещается, и первый же замер показал не токен, а высоту, которую
    /// потребовал текст, — одинаковую во всех трёх ступенях.
    /// </para>
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Compact", 24d)]
    [InlineData("Normal", 28d)]
    [InlineData("Comfortable", 32d)]
    public void A_tier_changes_the_height_of_a_control(string tier, double expected)
    {
        var button = new ArxisStudio.Controls.AxButton { Content = "Готово" };

        // Стопка, а не окно: содержимое окна растягивается на всю высоту.
        var window = new Window { Content = new StackPanel { Children = { button } } };

        window.Resources.MergedDictionaries.Add(Tier(tier));
        window.Show();
        window.UpdateLayout();

        Assert.Equal(expected, button.Bounds.Height);

        window.Close();
    }

    private static ResourceInclude Tier(string tier)
    {
        var uri = new Uri($"avares://ArxisStudio.Themes.Arxis/Density/{tier}.axaml");

        return new ResourceInclude(uri) { Source = uri };
    }

    /// <summary>Обычные значения тех ключей, которые двигает плотность.</summary>
    private static Dictionary<string, string> Base()
    {
        var spacing = Declared("Spacing.axaml");
        var metrics = Declared("Metrics.axaml");
        var all = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var key in Sized)
            all[key] = metrics[key];

        foreach (var (key, value) in spacing)
            all[key] = value;

        // Псевдонимы ссылаются на ступень и своей величины не несут.
        return all.Where(entry => entry.Value.Length > 0 && char.IsDigit(entry.Value[0]))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
    }

    private static Dictionary<string, string> Declared(string file) =>
        Declaration.Matches(ThemeSources.Text(file))
            .ToDictionary(
                match => match.Groups[2].Value,
                match => match.Groups[3].Success ? match.Groups[3].Value : match.Groups[4].Value,
                StringComparer.Ordinal);

    private static List<double> Numbers(string value) =>
        [.. value.Split(',').Select(part => double.Parse(part, CultureInfo.InvariantCulture))];

    private static List<double> Shape(string? side, double length) => side switch
    {
        "Leading" => [length, 0, 0, 0],
        "Trailing" => [0, 0, length, 0],
        "Above" => [0, length, 0, 0],
        "Below" => [0, 0, 0, length],
        "Sides" => [length, 0],
        "Ends" => [0, length],
        _ => [length],
    };
}
