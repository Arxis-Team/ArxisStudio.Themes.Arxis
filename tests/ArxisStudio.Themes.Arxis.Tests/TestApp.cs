using ArxisStudio.Themes.Arxis;
using ArxisStudio.Themes.Arxis.Tests;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(TestApp))]

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Headless-приложение тестов: FluentTheme плюс ArxisTheme — та же пара, что у
/// студии и галереи.
/// </summary>
/// <remarks>
/// Базовый слой здесь не для удобства, а для правды: под ним тема и работает у
/// потребителя, и правку Fluent, перебившую наш стиль, набор обязан поймать.
///
/// Своё покрытие темы он при этом прикрывает: недостающий шаблон Fluent молча
/// подменит собой. Поэтому самодостаточность проверяется не приложением, а
/// самой темой — см. ThemeAppliesTemplatesTests.
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
        Styles.Add(new FluentTheme());
        Styles.Add(new ArxisTheme());
    }
}
