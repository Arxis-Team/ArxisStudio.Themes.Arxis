using ArxisStudio.Themes.Arxis;
using ArxisStudio.Themes.Arxis.Tests;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(TestApp))]

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Headless-приложение тестов: FluentTheme (базовый слой M0) + ArxisTheme —
/// та же комбинация, что у студии и галереи.
/// </summary>
public class TestApp : Application
{
    /// <summary>Собирает headless-приложение с темой «Arxis».</summary>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<TestApp>()
        .WithInterFont()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());

    /// <inheritdoc/>
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new ArxisTheme());
    }
}
