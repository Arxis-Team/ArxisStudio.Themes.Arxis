using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace ArxisStudio.Themes.Arxis;

/// <summary>
/// Тема «Arxis»: палитры Dark/Light по дизайн-токенам студии и ControlTheme-шаблоны
/// всех Ax*-контролов, а с ними — окно, окно попапа и простые контейнеры. Чужого базового слоя
/// под ней нет: приложению довольно её одной, а зарегистрировать шрифт Inter
/// (<c>WithInterFont()</c>) — по-прежнему его дело.
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
