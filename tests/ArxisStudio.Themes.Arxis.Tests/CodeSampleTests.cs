using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Шорткат и блок кода: карточка «Шорткаты и код».
/// </summary>
/// <remarks>
/// Клавиша — плашка 20 высотой, набранная тем же шрифтом, что подпись рядом:
/// моношрифт проект отдаёт коду, путям и значениям, а «Shift F10» — ни то, ни
/// другое.
///
/// Блок кода стоит на утопленной подложке, а не на фоне поля: в светлой теме
/// поле белое, а блок там серый — тот же шаг вниз от панели, что у строки
/// дерева контролов. Кегль в карточке 12.5, в шкале его нет — взят базовый.
/// </remarks>
public class CodeSampleTests
{
    /// <summary>Клавиша: 20 высотой, отбивка 6, радиус контрола, Medium.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Key_cap_is_a_plate_of_twenty(string variant)
    {
        var (key, window) = Key(variant);

        Assert.Equal(20d, key.Height);
        Assert.Equal(new Thickness(6, 0), key.Padding);
        Assert.Equal(new CornerRadius(4), key.CornerRadius);
        Assert.Equal(FontWeight.Medium, key.FontWeight);
        Assert.Equal(11.5, key.FontSize);
        Assert.Equal(Resource(window, "AxBg3Color", variant), Colour(key.Background));

        window.Close();
    }

    /// <summary>
    /// Клавиша набрана тем же шрифтом, что подпись рядом.
    /// </summary>
    /// <remarks>
    /// Моношрифт остаётся коду, путям и значениям — так записано в таблице
    /// ключей, и в карточке клавиши стоят обычным шрифтом.
    /// </remarks>
    [AvaloniaFact]
    public void Key_cap_does_not_take_the_mono_face()
    {
        var (key, window) = Key("Dark");

        Assert.DoesNotContain("Cascadia", key.FontFamily.ToString());

        window.Close();
    }

    /// <summary>Блок кода: утопленная подложка, рамка, крупный радиус.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Code_block_sits_in_a_sunken_frame(string variant)
    {
        var (code, window) = Code(variant);

        Assert.Equal(Resource(window, "AxBgSunkenColor", variant), Colour(code.Background));
        Assert.Equal(Resource(window, "AxBrdColor", variant), Colour(code.BorderBrush));
        Assert.Equal(new Thickness(1), code.BorderThickness);
        Assert.Equal(new CornerRadius(8), code.CornerRadius);
        Assert.Equal(new Thickness(14, 12), code.Padding);

        window.Close();
    }

    /// <summary>Код набран моношрифтом базового кегля со строкой в 20.</summary>
    [AvaloniaFact]
    public void Code_is_set_in_the_mono_face_at_the_base_size()
    {
        var (code, window) = Code("Dark");

        Assert.Contains("Cascadia Code", code.FontFamily.ToString());
        Assert.Equal(13d, code.FontSize);

        var text = (SelectableTextBlock)Part(code, "PART_Text");

        Assert.Equal(20d, text.LineHeight);

        window.Close();
    }

    /// <summary>Подсветка идёт токенами кода, а не своими цветами.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Highlighting_comes_from_the_code_tokens(string variant)
    {
        var (code, window) = Code(variant);

        Assert.Equal(Resource(window, "AxCodeFgColor", variant), Colour(code.Foreground));
        Assert.Equal(Resource(window, "AxCodeTagColor", variant), Colour(code.TagBrush));
        Assert.Equal(Resource(window, "AxCodeAttrColor", variant), Colour(code.AttributeBrush));
        Assert.Equal(Resource(window, "AxCodeStringColor", variant), Colour(code.StringBrush));
        Assert.Equal(Resource(window, "AxCodeCommentColor", variant), Colour(code.CommentBrush));

        window.Close();
    }

    private static (AxChip Key, Window Window) Key(string variant)
    {
        var key = new AxChip { Classes = { "kbd" }, Content = "Shift F10" };
        var window = Shown(key, variant);

        return (key, window);
    }

    private static (AxCodeBlock Code, Window Window) Code(string variant)
    {
        var code = new AxCodeBlock { Text = "<Button Content=\"Отправить\"/>" };
        var window = Shown(code, variant);

        return (code, window);
    }

    private static Window Shown(Control content, string variant)
    {
        var window = new Window
        {
            Width = 440,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = content,
        };

        window.Show();
        window.UpdateLayout();

        return window;
    }

    private static Control Part(Control control, string name)
    {
        var part = control.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Name == name);

        Assert.True(part is not null, $"в шаблоне нет части {name}");
        return part!;
    }

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key, string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        Assert.True(window.TryFindResource(key, theme, out var value), key);

        return (Color)value!;
    }
}
