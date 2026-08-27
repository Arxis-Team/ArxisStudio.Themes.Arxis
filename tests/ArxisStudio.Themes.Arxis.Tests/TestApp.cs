using ArxisStudio.Themes.Arxis;
using ArxisStudio.Themes.Arxis.Tests;
using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(TestApp))]

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Headless-приложение тестов: одна тема «Arxis», без базового слоя.
/// </summary>
/// <remarks>
/// Под ней тема и работает у потребителя: Ax*-контролы она покрывает целиком,
/// а из базовых берёт на себя те шесть, которых им не хватает — ContextMenu,
/// MenuFlyoutPresenter, MenuItem, ScrollBar, Separator и ToolTip. Тема Fluent
/// стояла здесь слоем ниже и могла прикрыть недостающий шаблон собой; без неё
/// такая дыра падает тестом, а не всплывает у потребителя.
/// </remarks>
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
        Styles.Add(new ArxisTheme());
    }
}
