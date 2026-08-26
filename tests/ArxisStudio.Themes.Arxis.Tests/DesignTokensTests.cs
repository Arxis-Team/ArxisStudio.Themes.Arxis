using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Сплошная сверка темы с дизайн-проектом: каждая ступень каждой шкалы и
/// каждый семантический токен в обоих вариантах.
/// </summary>
/// <remarks>
/// Значения не переписаны сюда руками — они вынуты из самого дизайн-проекта
/// («10 Основы»: шкалы раздела 2, токены раздела 3, метрики раздела 5) и лежат
/// рядом в <c>design-tokens.json</c>. Поэтому тест ловит расхождение в обе
/// стороны: и когда тему правят мимо проекта, и когда проект уезжает от темы.
/// Выборочная сверка глазами такого не даёт: 172 значения карточками не
/// пересмотришь.
/// </remarks>
public class DesignTokensTests
{
    /// <summary>
    /// Единственное место, где тема намеренно расходится с таблицей раздела 3.
    /// </summary>
    /// <remarks>
    /// Ссылка в светлой теме: таблица даёт #3574F0, но это 4,28:1 на белом —
    /// ниже порога. Раздел 6 приёмки решает «взять AxBlue3 #3369D6 — 4,9:1», и
    /// макеты студии определяют свой <c>--link</c> ровно этим значением. Три
    /// источника из четырёх сходятся против одной строки таблицы.
    /// </remarks>
    private static readonly Dictionary<(string Token, string Variant), string> Decided = new()
    {
        [("AxLink", "Light")] = "#3369D6",
    };

    public static TheoryData<string, string, string> Scales => Load(design => design.Scales);

    public static TheoryData<string, string, string> Semantic => Load(design => design.Semantic);

    [AvaloniaTheory]
    [MemberData(nameof(Scales))]
    public void Scale_step_matches_the_design_project(string key, string variant, string expected)
        => AssertColor(key, variant, expected);

    [AvaloniaTheory]
    [MemberData(nameof(Semantic))]
    public void Semantic_token_matches_the_design_project(string key, string variant, string expected)
        => AssertColor(key + "Color", variant, Decided.GetValueOrDefault((key, variant), expected));

    /// <summary>Метрики и типографика раздела 5 — теми же значениями.</summary>
    [AvaloniaTheory]
    [InlineData("AxControlHeight", 28d)]
    [InlineData("AxControlHeightCompact", 24d)]
    [InlineData("AxRowHeight", 24d)]
    [InlineData("AxButtonMinWidth", 72d)]
    [InlineData("AxCheckboxSize", 16d)]
    [InlineData("AxCheckboxGap", 5d)]
    [InlineData("AxFocusOutlineWidth", 2d)]
    [InlineData("AxFontSize", 13d)]
    [InlineData("AxFontSizeSmall", 11.5d)]
    [InlineData("AxFontSizeCaption", 10.5d)]
    public void Metric_matches_the_design_project(string key, double expected)
    {
        var window = new Window();
        window.Show();

        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value), key);
        Assert.Equal(expected, Assert.IsType<double>(value));

        window.Close();
    }

    /// <summary>Радиусы: шкала 4 / 8 / 3 и ничего кроме.</summary>
    [AvaloniaTheory]
    [InlineData("AxCornerRadius", 4d)]
    [InlineData("AxCornerRadiusLarge", 8d)]
    [InlineData("AxCornerRadiusSmall", 3d)]
    public void Corner_radius_matches_the_design_project(string key, double expected)
    {
        var window = new Window();
        window.Show();

        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value), key);
        var radius = Assert.IsType<CornerRadius>(value);

        Assert.Equal(expected, radius.TopLeft);
        Assert.Equal(expected, radius.BottomRight);

        window.Close();
    }

    private static void AssertColor(string key, string variant, string expected)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        var window = new Window();
        window.Show();

        Assert.True(window.TryFindResource(key, theme, out var value), $"{key} нет в варианте {variant}");
        var color = Assert.IsType<Color>(value);

        // Проект пишет цвет шестью знаками, а прозрачность — восемью; сравниваем
        // тем же числом знаков, каким объявлено.
        var actual = expected.Length == 7
            ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
            : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

        Assert.Equal(expected.ToUpperInvariant(), actual);

        window.Close();
    }

    private static TheoryData<string, string, string> Load(
        Func<DesignProject, Dictionary<string, Dictionary<string, string>>> part)
    {
        var data = new TheoryData<string, string, string>();

        foreach (var (key, halves) in part(DesignProject.Load()).OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            foreach (var (variant, value) in halves.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                data.Add(key, variant, value);
        }

        return data;
    }

}
