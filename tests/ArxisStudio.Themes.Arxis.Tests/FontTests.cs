using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Шрифты темы: оба ключа ведут к настоящим начертаниям.
/// </summary>
/// <remarks>
/// Пропажа шрифта — самый тихий отказ из возможных: разметка просит семейство,
/// система его не находит и молча подставляет своё. Ошибки нет, кегль и
/// метрики те же, а текст другой — на снимке это заметит только тот, кто знал,
/// как должно быть.
///
/// Поэтому проверяется не имя в ключе, а то, что менеджер шрифтов отдаёт по
/// нему начертание с ожидаемым именем семейства.
/// </remarks>
public class FontTests
{
    /// <summary>Моношрифт приезжает из своей библиотеки, а не из системы.</summary>
    [AvaloniaFact]
    public void Mono_family_resolves_to_cascadia()
    {
        Assert.Equal("Cascadia Code", Resolve("fonts:Cascadia#Cascadia Code"));
    }

    /// <summary>Основной шрифт — Inter, как записано в таблице ключей.</summary>
    [AvaloniaFact]
    public void Base_family_resolves_to_inter()
    {
        Assert.Equal("Inter", Resolve("fonts:Inter#Inter"));
    }

    /// <summary>
    /// Имя семейства, которое менеджер шрифтов действительно отдаёт по ключу.
    /// </summary>
    /// <remarks>
    /// TryGetGlyphTypeface возвращает true и для подмены: не найдя семейства,
    /// менеджер отдаёт начертание по умолчанию. Отличает их имя.
    /// </remarks>
    private static string Resolve(string family)
    {
        var typeface = new Typeface(new FontFamily(family));

        Assert.True(
            FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphs),
            $"{family}: начертания нет вовсе");

        return glyphs.FamilyName;
    }
}
