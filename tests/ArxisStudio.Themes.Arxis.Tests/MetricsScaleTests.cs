using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Метрики и дробный масштаб экрана.
/// </summary>
/// <remarks>
/// Высоты хрома кратны четырём: при 125 % целый пиксель даёт именно эту
/// кратность (4 × 1,25 = 5), и тогда вкладка, шапка панели и полоса заголовка
/// не встают на полпикселя ни при каком масштабе с шагом 25 %. Остальные длины
/// раскладки чётные — этого хватает для точных пропорций при 150 %, а резкость
/// краёв держит округление раскладки Avalonia.
/// <para>
/// Нечётных длин в теме не осталось: тумблер 30 × 16 с бегунком 12 и зазор
/// флажка 6 пришли на смену 17, 13 и 5 вместе с сеткой хрома.
/// </para>
/// </remarks>
public class MetricsScaleTests
{
    /// <summary>
    /// Высоты хрома и ступень лестницы дерева: всё, что идёт за плотностью.
    /// </summary>
    public static TheoryData<string> Chrome =>
    [
        "AxRowHeight",
        "AxControlHeight",
        "AxControlHeightCompact",
        "AxControlHeightSmall",
        "AxTabHeight",
        "AxTitleBarHeight",
        "AxStatusBarHeight",
        "AxMenuRowHeight",
        "AxToolbarButtonSize",
        "AxWindowButtonWidth",
        "AxTreeIndent",
    ];

    /// <summary>Остальные длины раскладки: ширины, размеры и толщины.</summary>
    public static TheoryData<string> Lengths =>
    [
        "AxButtonMinWidth",
        "AxButtonMinWidthCompact",
        "AxDialogButtonMinWidth",
        "AxCheckboxSize",
        "AxCheckboxGap",
        "AxIconSize",
        "AxIconSizeSmall",
        "AxScrollBarLane",
        "AxScrollThumbSize",
        "AxScrollThumbSizeHover",
        "AxToggleWidth",
        "AxToggleHeight",
        "AxToggleKnobSize",
        "AxFocusOutlineWidth",
        // Ступени шкалы отступов: расстояние — такая же длина раскладки.
        "AxSpaceHair",
        "AxSpaceTight",
        "AxSpaceSnug",
        "AxSpace",
        "AxSpaceWide",
        "AxSpaceLoose",
        "AxSpaceSection",
        "AxSpaceScreen",
        // Плитки окна проекта: ширина плитки, ползунка их размера, малая ступень силуэта и шаг лестницы.
        "AxTileWidth",
        "AxTileWidthLarge",
        "AxTileSliderWidth",
        "AxTileGlyphSizeSmall",
        "AxTileGlyphSizeStep",
    ];

    [AvaloniaTheory]
    [MemberData(nameof(Lengths))]
    public void Layout_lengths_are_even(string key)
    {
        var window = new Window();
        window.Show();

        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value));
        var length = Assert.IsType<double>(value);

        Assert.Equal(length, Math.Round(length));
        Assert.True(length % 2 == 0, $"{key} = {length}: длина раскладки должна быть чётной");

        // При 150 % чётная длина обязана дать целое число пикселей.
        Assert.Equal(length * 1.5, Math.Round(length * 1.5));

        window.Close();
    }

    /// <summary>
    /// Силуэт плитки кладёт каждую единицу сетки значка на целые пиксели при любом масштабе.
    /// </summary>
    /// <remarks>
    /// Плитка — второе названное исключение из «размер значка один»: силуэт папки и листа в клетке 16,
    /// растянутый до размера ступени. Обычная и крупная ступени стоят на пикселях сами: у 64 единица
    /// сетки — 4, 5, 6, 7 и 8 пикселей при 100…200 %, у 128 — вдвое больше. Промежуточные ступени так
    /// не ложатся — у 48 при 125 % единица 3,75 пикселя, — и их силуэт сажает на пиксели AxIcon; здесь
    /// держится то, что обычная и крупная в этом не нуждаются.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("AxTileGlyphSize")]
    [InlineData("AxTileGlyphSizeLarge")]
    public void A_tile_glyph_puts_every_grid_unit_on_whole_pixels(string key)
    {
        var window = new Window();
        window.Show();

        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value));
        var size = Assert.IsType<double>(value);

        foreach (var scale in new[] { 1, 1.25, 1.5, 1.75, 2 })
        {
            var unit = size / 16 * scale;

            Assert.True(
                Math.Abs(unit - Math.Round(unit)) < 1e-9,
                $"{key} = {size}: при {scale * 100} % единица сетки — {unit} пикселя, край силуэта размыт");
        }

        window.Close();
    }

    /// <summary>
    /// Лестница плитки проходит через свои ключи: обычная и крупная ступени — целые шаги от малой, а
    /// шаг — клетка набора.
    /// </summary>
    /// <remarks>
    /// Окно проекта строит лестницу от малой ступени шагом до крупной и начинает с обычной. Ступень,
    /// не лежащая на лестнице, была бы недостижима ползунком и колесом, а шаг не в клетку набора давал
    /// бы силуэту дробную единицу сетки и на обычном экране.
    /// </remarks>
    [AvaloniaFact]
    public void The_tile_ladder_steps_through_its_named_sizes()
    {
        var window = new Window();
        window.Show();

        double Size(string key)
        {
            Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value), $"в теме нет ключа {key}");

            return Assert.IsType<double>(value);
        }

        var small = Size("AxTileGlyphSizeSmall");
        var normal = Size("AxTileGlyphSize");
        var large = Size("AxTileGlyphSizeLarge");
        var step = Size("AxTileGlyphSizeStep");

        Assert.True(small < normal && normal < large, $"ступени не по порядку: {small}, {normal}, {large}");
        Assert.Equal(Size("AxIconSize"), step);
        Assert.True((normal - small) % step == 0, $"обычная ступень {normal} не на лестнице от {small} шагом {step}");
        Assert.True((large - small) % step == 0, $"крупная ступень {large} не на лестнице от {small} шагом {step}");

        window.Close();
    }

    /// <summary>Высота хрома кратна четырём — целый пиксель и при 125 %.</summary>
    [AvaloniaTheory]
    [MemberData(nameof(Chrome))]
    public void Chrome_heights_are_multiples_of_four(string key)
    {
        var window = new Window();
        window.Show();

        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value));
        var length = Assert.IsType<double>(value);

        Assert.True(length % 4 == 0, $"{key} = {length}: высота хрома должна быть кратна четырём");

        // Целый пиксель при каждом масштабе с шагом 25 %.
        foreach (var scale in new[] { 1.25, 1.5, 1.75, 2 })
            Assert.Equal(length * scale, Math.Round(length * scale));

        window.Close();
    }

    /// <summary>
    /// Округление раскладки включено.
    /// </summary>
    /// <remarks>
    /// Именно оно, а не кратность двум, держит края резкими на дробном
    /// масштабе: Avalonia сажает границы на целый пиксель устройства. Свойство
    /// включено по умолчанию, и выключить его где-то в теме или разметке —
    /// значит получить размытые рамки при 125 %, ничего больше не сломав.
    /// Тест ловит такое выключение.
    /// </remarks>
    [AvaloniaFact]
    public void Layout_rounding_stays_on()
    {
        var button = new ArxisStudio.Controls.AxButton { Content = "Проверка" };
        var window = new Window { Content = button };
        window.Show();

        Assert.True(window.UseLayoutRounding);
        Assert.True(button.UseLayoutRounding);

        window.Close();
    }

    /// <summary>
    /// Отступы контролов: только по горизонтали.
    /// </summary>
    /// <remarks>
    /// Вертикального отступа у кнопки, поля и комбобокса нет: текст стоит по
    /// центру наименьшей высоты 28. С отступом 6 сверху и снизу тексту
    /// оставалось 16, а строке 13-го кегля нужно больше — нижний край букв
    /// срезался, что и было видно на кнопках. Ноль отдаёт всю высоту.
    ///
    /// Горизонтальные чётные: девятка давала полпикселя при 150 %, и отступ слева
    /// и справа мог разойтись на пиксель после округления раскладки.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("AxButtonPadding", 12, 0, 12, 0)]
    [InlineData("AxTextFieldPadding", 10, 0, 10, 0)]
    [InlineData("AxComboBoxPadding", 10, 0, 6, 0)]
    public void Paddings_keep_their_values(
        string key, double left, double top, double right, double bottom)
    {
        var window = new Window();
        window.Show();

        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value));
        Assert.Equal(new Thickness(left, top, right, bottom), value);

        window.Close();
    }
}
