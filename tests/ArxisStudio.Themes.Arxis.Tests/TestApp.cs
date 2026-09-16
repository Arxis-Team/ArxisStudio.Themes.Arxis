using ArxisStudio.Themes.Arxis;
using ArxisStudio.Themes.Arxis.Tests;
using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(TestApp))]

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Headless-приложение тестов: одна ArxisTheme — так же, как у студии.
/// </summary>
/// <remarks>
/// Чужого базового слоя здесь больше нет, и это не упрощение, а проверка: недостающий шаблон он
/// молча подменял собой, и дыру в покрытии темы было видно только отдельным тестом. Теперь её
/// видно каждым: контрол без шаблона в безголовом прогоне остаётся пустым местом.
/// </remarks>
public class TestApp : Application
{
    /// <summary>Собирает headless-приложение с темой «Arxis».</summary>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<TestApp>()
        .WithInterFont()
        .WithCascadiaFont()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());

    /// <inheritdoc/>
    public override void Initialize()
    {
        Styles.Add(new ArxisTheme());
    }
}
