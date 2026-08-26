using System.Reflection;
using System.Text.Json;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Значения, вынутые из дизайн-проекта скриптом <c>extract-design-tokens.py</c>.
/// </summary>
/// <remarks>
/// Файл рядом — не справочник, переписанный руками, а выгрузка: шкалы и токены
/// «10 Основ», метрики раздела 5 спецификации, значения CSS-переменных обоих
/// вариантов и таблица состояний из компонентов. Тесты сверяются с ним, поэтому
/// правка проекта роняет сборку ровно там, где тема от него отстала.
/// </remarks>
public sealed class DesignProject
{
    /// <summary>Ступени шкал: AxGray1…14, AxBlue1…13 и остальные.</summary>
    public Dictionary<string, Dictionary<string, string>> Scales { get; set; } = [];

    /// <summary>Семантические токены раздела 3 в обоих вариантах.</summary>
    public Dictionary<string, Dictionary<string, string>> Semantic { get; set; } = [];

    /// <summary>Метрики и типографика раздела 5 спецификации.</summary>
    public Dictionary<string, string> Metrics { get; set; } = [];

    /// <summary>CSS-переменные макетов: короткое имя — цвет в каждом варианте.</summary>
    public Dictionary<string, Dictionary<string, string>> Variables { get; set; } = [];

    /// <summary>
    /// Состояния контролов: «компонент/вид/состояние» — какой переменной
    /// красится фон, текст и рамка.
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> States { get; set; } = [];

    /// <summary>Читает выгрузку из встроенного ресурса сборки тестов.</summary>
    public static DesignProject Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var name = assembly.GetManifestResourceNames()
            .Single(candidate => candidate.EndsWith("design-tokens.json", StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(name)!;

        return JsonSerializer.Deserialize<DesignProject>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}
