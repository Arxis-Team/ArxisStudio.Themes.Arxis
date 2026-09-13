using System.Reflection;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Разметка темы текстом.
/// </summary>
/// <remarks>
/// Почти всё здесь проверяется на живом дереве контролов: так видно не то, что
/// написано, а то, что вышло. Но два вопроса задаются именно к написанному —
/// сколько отступов осталось числом и какие ключи объявляет шкала, — а
/// скомпилированная разметка ни того, ни другого уже не показывает. Поэтому
/// исходники едут в сборке тестов ресурсами: шаблон в csproj берёт их
/// звёздочкой, чтобы новый файл темы попадал под счёт сам.
/// </remarks>
internal static class ThemeSources
{
    /// <summary>Имя ресурса и текст, по одному на файл разметки.</summary>
    public static IEnumerable<(string Name, string Text)> All()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var names = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("axaml/", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal);

        foreach (var name in names)
            yield return (Short(name), Read(assembly, name));
    }

    /// <summary>Текст одного файла разметки по его имени.</summary>
    /// <param name="file">Имя файла, например <c>Spacing.axaml</c>.</param>
    public static string Text(string file)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var name = assembly.GetManifestResourceNames()
            .Single(candidate =>
                candidate.StartsWith("axaml/", StringComparison.Ordinal) &&
                Short(candidate).Equals(file, StringComparison.Ordinal));

        return Read(assembly, name);
    }

    private static string Read(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    // Путь внутри ресурса собирает MSBuild, и разделитель там свой для каждой
    // системы сборки. Имя файла от него не зависит.
    private static string Short(string name) => name[(name.LastIndexOfAny(['/', '\\']) + 1)..];
}
