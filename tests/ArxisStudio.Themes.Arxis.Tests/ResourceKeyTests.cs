using System.Text.RegularExpressions;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Ключ, названный разметкой темы, тема объявляет, а зависящий от варианта берётся динамически.
/// </summary>
/// <remarks>
/// Опечатка в ключе не валит ни сборку, ни показ: DynamicResource молча отдаёт пустоту, и контрол
/// остаётся без фона или с нулевым отступом. Заметить такое можно только глазами и только в том
/// состоянии контрола, где ключ стоит, — наведение, выключенный, ошибка. Переименование ролей
/// прошло по семидесяти с лишним файлам, и ключ, забытый в одном из них, этот тест называет по
/// имени файла.
/// <para>
/// StaticResource читает значение один раз, при загрузке, и смены темы не видит: кисть, взятая
/// так, остаётся тёмной в светлой студии. Статически берут только то, что от варианта не зависит, —
/// шаблон, метрику, ступень шкалы расстояний. Исключение одно — сама палитра: там кисть роли берёт
/// свой цвет внутри словаря того же варианта, и другого способа связать их нет.
/// </para>
/// </remarks>
public class ResourceKeyTests
{
    private static readonly Regex Reference = new(
        """\{(?:\w+:)?(Dynamic|Static)Resource\s+(?:ResourceKey\s*=\s*)?([A-Za-z_][\w.]*)\s*\}|<StaticResource\s[^>]*?ResourceKey="([A-Za-z_][\w.]*)(?=")""",
        RegexOptions.Compiled);

    /// <summary>Каждый ключ разметки темы объявлен темой — в обоих вариантах.</summary>
    [AvaloniaFact]
    public void Every_key_the_theme_markup_names_is_declared_in_both_variants()
    {
        var theme = new ArxisTheme();
        var named = References().ToList();

        Assert.Contains(named, reference => reference.Key == "AxSurfacePanelBrush");
        Assert.Contains(named, reference => reference.Key == "AxSpaceSnug" && reference.Static);

        var missing = named
            .Where(reference => !Found(theme, reference.Key, ThemeVariant.Dark, out _) || !Found(theme, reference.Key, ThemeVariant.Light, out _))
            .Select(reference => $"{reference.File}: {reference.Key}")
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0, "темой не объявлены: " + string.Join("; ", missing));
    }

    /// <summary>Ключ, у которого в тёмном и светлом варианте разные значения, статически не берётся.</summary>
    [AvaloniaFact]
    public void A_key_that_follows_the_variant_is_never_taken_statically()
    {
        var theme = new ArxisTheme();

        var frozen = References()
            .Where(reference => reference.Static && reference.File != "Palette.axaml")
            .Where(reference =>
                Found(theme, reference.Key, ThemeVariant.Dark, out var dark) &&
                Found(theme, reference.Key, ThemeVariant.Light, out var light) &&
                !Equals(dark, light))
            .Select(reference => $"{reference.File}: {reference.Key}")
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.True(frozen.Count == 0, "берутся статически, а зависят от варианта: " + string.Join("; ", frozen));
    }

    private static IEnumerable<(string File, string Key, bool Static)> References() =>
        ThemeSources.All().SelectMany(source => Reference.Matches(source.Text).Select(match => match.Groups[3].Success
            ? (source.Name, match.Groups[3].Value, true)
            : (source.Name, match.Groups[2].Value, match.Groups[1].Value == "Static")));

    private static bool Found(ArxisTheme theme, string key, ThemeVariant variant, out object? value) =>
        theme.TryGetResource(key, variant, out value);
}
