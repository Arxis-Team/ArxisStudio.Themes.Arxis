using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace ArxisStudio.Themes.Arxis;

/// <summary>
/// Тема «Arxis»: палитры Dark/Light по дизайн-токенам студии и ControlTheme-шаблоны
/// всех Ax*-контролов. В M0 подключается поверх <c>FluentTheme</c> — базового слоя
/// для не-Ax примитивов; приложение должно зарегистрировать шрифт Inter
/// (<c>WithInterFont()</c>).
/// </summary>
public partial class ArxisTheme : Styles
{
    /// <summary>Создаёт тему и загружает её XAML.</summary>
    /// <param name="sp">Провайдер сервисов из места включения темы; может быть null.</param>
    public ArxisTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}
