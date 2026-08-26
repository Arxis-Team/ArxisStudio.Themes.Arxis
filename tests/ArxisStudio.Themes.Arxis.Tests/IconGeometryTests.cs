using System.Reflection;
using System.Text.Json;
using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Форма каждой иконки против листа иконок дизайн-проекта.
/// </summary>
/// <remarks>
/// Иконка — это данные, и расходятся они молча: путь на полпикселя левее
/// глазом не виден, а в наборе из полутора сотен глифов не виден и подавно.
/// Поэтому сверяются не подписи и не количество, а сама форма.
///
/// Строку пути в готовой <see cref="Geometry"/> уже не прочитать — Avalonia
/// разбирает её в поток команд и текст не хранит. Зато форму можно снять
/// отпечатком: габариты, габариты с обводкой 1.2 и решётка проб по всей
/// клетке 16 × 16. Два пути, дающие один отпечаток, рисуют одно и то же —
/// а записаны при этом могут быть по-разному, и это правильно: сверяется
/// рисунок, а не то, каким текстом его записали.
/// </remarks>
public class IconGeometryTests
{
    /// <summary>Обводка набора: 1.2 из метрик, ею же меряются габариты.</summary>
    private static readonly Pen Stroke = new(Brushes.Black, 1.2);

    public static TheoryData<string, string> Icons
    {
        get
        {
            var data = new TheoryData<string, string>();

            foreach (var (name, path) in Load().OrderBy(entry => entry.Key, StringComparer.Ordinal))
                data.Add(name, path);

            return data;
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(Icons))]
    public void Icon_shape_matches_the_design_project(string name, string path)
    {
        var expected = Geometry.Parse(path);
        var actual = Resolve(name);

        Assert.Equal(Round(expected.Bounds), Round(actual.Bounds));
        Assert.Equal(Round(expected.GetRenderBounds(Stroke)), Round(actual.GetRenderBounds(Stroke)));
        Assert.Equal(Probe(expected), Probe(actual));
    }

    /// <summary>Набор не растерял иконок и не завёл лишних.</summary>
    [AvaloniaFact]
    public void Set_holds_exactly_the_icons_of_the_design_project()
    {
        var declared = Sources()
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            .Where(property => property.PropertyType == typeof(Geometry))
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(Load().Keys.OrderBy(name => name, StringComparer.Ordinal),
            declared.OrderBy(name => name, StringComparer.Ordinal));
    }

    /// <summary>
    /// Снимает отпечаток формы: попадание в заливку по решётке с шагом 0.5.
    /// </summary>
    /// <remarks>
    /// Шаг взят вдвое мельче полупиксельной сетки, на которой нарисован набор,
    /// поэтому сдвиг любого узла пути отпечаток меняет. Проверка попадания в
    /// обводку в headless-режиме не работает, и опираться на неё нельзя.
    /// </remarks>
    private static string Probe(Geometry geometry)
    {
        var hits = new System.Text.StringBuilder(33 * 33);

        for (var y = 0d; y <= 16; y += 0.5)
        {
            for (var x = 0d; x <= 16; x += 0.5)
                hits.Append(geometry.FillContains(new Point(x, y)) ? '#' : '.');
        }

        return hits.ToString();
    }

    /// <summary>Габариты с точностью до сотой: разбор пути даёт дробные числа.</summary>
    private static Rect Round(Rect rect) => new(
        Math.Round(rect.X, 2),
        Math.Round(rect.Y, 2),
        Math.Round(rect.Width, 2),
        Math.Round(rect.Height, 2));

    private static Geometry Resolve(string name)
    {
        foreach (var type in Sources())
        {
            if (type.GetProperty(name, BindingFlags.Public | BindingFlags.Static) is { } property)
                return (Geometry)property.GetValue(null)!;
        }

        throw new ArgumentException($"в наборе нет иконки {name}");
    }

    /// <summary>Действия лежат в AxIcons, глифы палитры — во вложенном Toolbox.</summary>
    private static IEnumerable<Type> Sources()
    {
        yield return typeof(AxIcons);

        foreach (var nested in typeof(AxIcons).GetNestedTypes(BindingFlags.Public))
            yield return nested;
    }

    private static Dictionary<string, string> Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var name = assembly.GetManifestResourceNames()
            .Single(candidate => candidate.EndsWith("design-icons.json", StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(name)!;

        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!["icons"];
    }
}
