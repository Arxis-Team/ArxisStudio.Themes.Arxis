using ArxisStudio.Controls;
using ArxisStudio.Icons;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Вкладки: две разновидности, разведённые по виду.
/// </summary>
/// <remarks>
/// Вкладка документа и вкладка панели различаются не только высотой. У панели
/// нет ни своего фона, ни рамки — выбранной её делают цвет, полоса снизу
/// толщиной 3 и усиленное начертание. У документа для этого есть фон и полоса
/// 2, и веса ей не дают: жирное имя файла в ряду имён читается как
/// другой уровень, а не как выбор.
///
/// Наведение у обеих — AxBg3, токен наведения; AxBg2 сходился с ним в
/// светлой теме случайно, а в тёмной вкладка под курсором была темнее, чем
/// нужно.
/// </remarks>
public class TabStripTests
{
    /// <summary>
    /// Наведение обеих разновидностей берёт токен наведения — но красит разное.
    /// </summary>
    /// <remarks>
    /// У вкладки документа это её собственный фон: она и есть плитка в ряду
    /// плиток. У вкладки панели — отдельная плашка внутри неё, потому что фон
    /// во всю площадь сливался бы с рамкой панели и разделителем шапки: те же
    /// AxBg3.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Hovered_tab_takes_the_hover_token(string variant)
    {
        foreach (var compact in new[] { false, true })
        {
            var (tab, window) = Shown(compact, variant);

            ((IPseudoClasses)tab.Classes).Set(":pointerover", true);
            window.UpdateLayout();

            var painted = Part(tab, compact ? "PART_Hover" : "PART_Root");

            Assert.True(painted.IsVisible, "наведения не видно");
            Assert.Equal(
                Resource(window, "AxBg3Color", variant),
                Colour(painted.GetValue(Border.BackgroundProperty)));

            window.Close();
        }
    }

    /// <summary>
    /// Плашка наведения меньше вкладки панели и не меняет её размера.
    /// </summary>
    /// <remarks>
    /// Вкладка в шапке тянется во всю её высоту и стоит вплотную к соседке, а
    /// рамка панели сверху и разделитель снизу — того же AxBg3, что и
    /// наведение. Заливка во всю площадь сливалась с ними, и от одного слова
    /// оставалась плита от края до края. Отступ в два пикселя со всех сторон
    /// открывает края плашки, оставляет между соседними плашками карточные
    /// четыре и не закрывает полосу выбора — она те же два снизу.
    ///
    /// Прежняя плашка была на восемь пикселей шире вкладки и скруглена только
    /// сверху: она залезала на соседку тем заметнее, чем короче имя.
    ///
    /// Сама вкладка при этом прежнего размера: размер на наведении не меняется,
    /// и попадать мышью человек должен по вкладке, а не по плашке.
    /// </remarks>
    [AvaloniaFact]
    public void The_panel_tab_hover_is_a_plate_inside_the_tab()
    {
        var (tab, window) = Shown(compact: true, "Dark");

        var was = tab.Bounds;

        ((IPseudoClasses)tab.Classes).Set(":pointerover", true);
        window.UpdateLayout();

        var plate = Part(tab, "PART_Hover");

        Assert.Equal(was, tab.Bounds);

        // Под плашкой — прозрачно. Вернись сюда заливка во всю вкладку, плашка
        // легла бы на неё тем же цветом, и отступы стали бы не видны.
        Assert.Equal(
            Colors.Transparent,
            Colour(Part(tab, "PART_Root").GetValue(Border.BackgroundProperty)));

        var at = plate.TranslatePoint(default, tab);

        Assert.NotNull(at);
        Assert.Equal(2d, at.Value.X);
        Assert.Equal(2d, at.Value.Y);
        Assert.Equal(tab.Bounds.Width - 4, plate.Bounds.Width);
        Assert.Equal(tab.Bounds.Height - 4, plate.Bounds.Height);

        // Скругление со всех сторон: плашка стоит внутри вкладки и ни из чего
        // не растёт.
        var corners = plate.GetValue(Border.CornerRadiusProperty);

        Assert.Equal(new CornerRadius(4), corners);

        window.Close();
    }

    /// <summary>Без наведения плашки нет вовсе — ни у панели, ни у документа.</summary>
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_hover_plate_is_absent_until_the_pointer_arrives(bool compact)
    {
        var (tab, window) = Shown(compact, "Dark");

        Assert.False(Part(tab, "PART_Hover").IsVisible, "плашка видна без наведения");

        window.Close();
    }

    /// <summary>
    /// Полоса выбора — два пикселя у обеих разновидностей.
    /// </summary>
    /// <remarks>
    /// Два, а не три. Панельная вкладка держала три, и в шапке дока нижний пиксель
    /// уходил под разделитель — полоса выходила и громче соседней, и короче
    /// себя самой.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData(false, 2d)]
    [InlineData(true, 2d)]
    public void Selected_tab_marks_itself_with_a_two_pixel_bar(bool compact, double thickness)
    {
        var (tab, window) = Shown(compact, "Dark");

        ((IPseudoClasses)tab.Classes).Set(":selected", true);
        window.UpdateLayout();

        var marker = Part(tab, "PART_ActiveMarker");

        Assert.True(marker.IsVisible, "полосы выбора не видно");
        Assert.Equal(thickness, marker.Bounds.Height);
        Assert.Equal(
            Resource(window, "AxAccColor", "Dark"),
            Colour(marker.GetValue(Border.BackgroundProperty)));

        window.Close();
    }

    /// <summary>
    /// Выбор веса не меняет — ни у той разновидности, ни у другой.
    /// </summary>
    /// <remarks>
    /// Жирное начертание меняет метрику текста: вкладка становится шире, и весь
    /// ряд сдвигается на каждое переключение. Панельная вкладка так и делала —
    /// имена соседок ездили от щелчка к щелчку. Выбор показывают цвет и полоса
    /// снизу: они соседей не двигают.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Selecting_a_tab_does_not_change_its_weight(bool compact)
    {
        var (tab, window) = Shown(compact, "Dark");

        var was = tab.FontWeight;

        ((IPseudoClasses)tab.Classes).Set(":selected", true);
        window.UpdateLayout();

        Assert.Equal(was, tab.FontWeight);
        Assert.Equal("Normal", tab.FontWeight.ToString());

        window.Close();
    }

    /// <summary>Высота: 34 у вкладки документа, 32 у вкладки панели.</summary>
    [AvaloniaTheory]
    [InlineData(false, 34d)]
    [InlineData(true, 32d)]
    public void Height_keeps_its_value(bool compact, double height)
    {
        var (tab, window) = Shown(compact, "Dark");

        Assert.Equal(height, tab.Bounds.Height);

        // Восемь — отступ вкладки.
        Assert.Equal(new Avalonia.Thickness(8, 0), tab.Padding);

        window.Close();
    }

    /// <summary>
    /// Крестик закрытия — площадка, а не линия.
    /// </summary>
    /// <remarks>
    /// Попадать мышью по контуру в полтора пикселя человек не должен, поэтому
    /// мышь ловит вся площадка шестнадцать на шестнадцать. Без прозрачного
    /// фона она прозрачна и для попадания: нажатие ушло бы во вкладку, и та бы
    /// просто выбралась.
    /// </remarks>
    [AvaloniaFact]
    public void The_close_cross_is_an_area_and_not_a_line()
    {
        var (tab, window) = Shown(compact: false, "Dark");
        var close = Part(tab, "PART_Close");

        Assert.Equal(new Size(16, 16), close.Bounds.Size);
        Assert.NotNull(close.GetValue(Border.BackgroundProperty));

        window.Close();
    }

    /// <summary>
    /// Под курсором крестик подсвечивается сам.
    /// </summary>
    /// <remarks>
    /// Пока своей подсветки у него не было, наведение на крестик выглядело
    /// точь-в-точь как наведение на вкладку: человек нажимал, не зная, закроет
    /// он её или выберет. Цвет — ступенью заметнее наведения вкладки: AxBg4
    /// против AxBg3.
    /// </remarks>
    [AvaloniaFact]
    public void The_cross_lights_up_under_the_pointer()
    {
        var (tab, window) = Shown(compact: true, "Dark");
        var close = Part(tab, "PART_Close");

        Assert.Equal(
            Colors.Transparent,
            Colour(close.GetValue(Border.BackgroundProperty)));

        ((IPseudoClasses)close.Classes).Set(":pointerover", true);
        window.UpdateLayout();

        Assert.Equal(
            Resource(window, "AxBg4Color", "Dark"),
            Colour(close.GetValue(Border.BackgroundProperty)));

        window.Close();
    }

    /// <summary>Щелчок по крестику просит закрыть вкладку.</summary>
    /// <remarks>
    /// Просит, а не закрывает: у документа могут быть несохранённые правки, и
    /// спрашивать о них — дело хозяина вкладки, а не темы.
    /// </remarks>
    [AvaloniaFact]
    public void Clicking_the_cross_asks_to_close()
    {
        var (tab, window) = Shown(compact: false, "Dark");
        var close = Part(tab, "PART_Close");

        var asked = 0;
        tab.CloseRequested += (_, _) => asked++;

        var at = close.TranslatePoint(new Point(7, 7), window);

        Assert.NotNull(at);

        window.MouseMove(at.Value);
        window.MouseDown(at.Value, MouseButton.Left);
        window.MouseUp(at.Value, MouseButton.Left);
        window.UpdateLayout();

        Assert.Equal(1, asked);

        window.Close();
    }

    /// <summary>
    /// Значок вкладки — целая клетка набора, а не мелкий шеврон.
    /// </summary>
    /// <remarks>
    /// Набор нарисован в клетке 16, и обводка ложится в пиксели только там, где клетка в них
    /// ложится. Двенадцать — размер шеврона в тесной строке: глиф панели в нём мутнел бы при любом
    /// обычном масштабе экрана, а по весу расходился бы с тем же глифом на кнопке полосы.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_icon_of_a_tab_is_a_whole_cell(bool compact)
    {
        var (tab, window) = Shown(compact, "Dark");

        tab.Icon = AxIcons.Terminal;
        window.UpdateLayout();

        var icon = Part(tab, "PART_Icon");

        Assert.True(icon.IsVisible, "значок задан, а вкладка его не показывает");
        Assert.True(window.TryFindResource("AxIconSize", out var size), "AxIconSize");
        Assert.Equal(new Size((double)size!, (double)size!), icon.Bounds.Size);

        window.Close();
    }

    /// <summary>Нет значка — нет и места под него: подпись стоит от самого края.</summary>
    [AvaloniaFact]
    public void A_tab_without_an_icon_keeps_no_room_for_it()
    {
        var (tab, window) = Shown(compact: true, "Dark");

        Assert.False(Part(tab, "PART_Icon").IsVisible, "значка не давали, а он виден");
        Assert.Equal(tab.Padding.Left, Name(tab).TranslatePoint(default, tab)!.Value.X);

        window.Close();
    }

    /// <summary>
    /// От значка до подписи — зазор значка и текста, от подписи до крестика — прежний.
    /// </summary>
    /// <remarks>
    /// Значок и подпись — одна мысль, крестик — другое действие. Раздели их один зазор, значок
    /// отошёл бы от своего имени так же далеко, как крестик, и читался бы третьим элементом
    /// вкладки. Зазор значка и текста — тот же, что у строки дерева и у поля поиска.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_icon_stands_close_to_the_name_and_the_cross_keeps_its_distance(bool compact)
    {
        var (tab, window) = Shown(compact, "Dark");

        tab.Icon = AxIcons.Terminal;
        window.UpdateLayout();

        var icon = Part(tab, "PART_Icon");
        var name = Name(tab);
        var close = Part(tab, "PART_Close");

        Assert.True(window.TryFindResource("AxGapIconText", out var gap), "AxGapIconText");
        Assert.True(window.TryFindResource("AxSpace", out var space), "AxSpace");

        var iconEnd = icon.TranslatePoint(new Point(icon.Bounds.Width, 0), tab)!.Value.X;
        var nameStart = name.TranslatePoint(default, tab)!.Value.X;
        var nameEnd = name.TranslatePoint(new Point(name.Bounds.Width, 0), tab)!.Value.X;
        var closeStart = close.TranslatePoint(default, tab)!.Value.X;

        Assert.Equal((double)gap!, nameStart - iconEnd, 3);
        Assert.Equal((double)space!, closeStart - nameEnd, 3);

        window.Close();
    }

    /// <summary>
    /// Без своего цвета значок красится подписью — и меняется вместе с ней.
    /// </summary>
    /// <remarks>
    /// Цвет значка у вкладки заведён для документа: тип файла красит свой значок сам. Глиф панели
    /// своего цвета не несёт — он из набора студии и красится темой, как кнопка полосы. Раньше путь
    /// без данного цвета оставался без кисти и не рисовался вовсе; теперь он идёт за подписью:
    /// вторичный у невыбранной вкладки, основной у выбранной, выключенный у выключенной.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Without_a_brush_the_icon_takes_the_colour_of_the_name(string variant)
    {
        foreach (var compact in new[] { false, true })
        {
            var (tab, window) = Shown(compact, variant);

            tab.Icon = AxIcons.Terminal;
            window.UpdateLayout();

            Assert.Equal(Resource(window, "AxFg2Color", variant), Colour(Stroke(tab)));

            ((IPseudoClasses)tab.Classes).Set(":selected", true);
            window.UpdateLayout();

            Assert.Equal(Resource(window, "AxFgColor", variant), Colour(Stroke(tab)));

            tab.IsEnabled = false;
            window.UpdateLayout();

            Assert.Equal(Resource(window, "AxFgDisabledColor", variant), Colour(Stroke(tab)));

            window.Close();
        }
    }

    /// <summary>Данный вкладке цвет значка красит его сам — и выбор его не перебивает.</summary>
    [AvaloniaFact]
    public void A_given_brush_paints_the_icon_whatever_the_state()
    {
        var (tab, window) = Shown(compact: false, "Dark");

        tab.Icon = AxIcons.Document;
        tab.IconBrush = Brushes.Orange;
        window.UpdateLayout();

        Assert.Equal(Colors.Orange, Colour(Stroke(tab)));

        ((IPseudoClasses)tab.Classes).Set(":selected", true);
        window.UpdateLayout();

        Assert.Equal(Colors.Orange, Colour(Stroke(tab)));

        window.Close();
    }

    /// <summary>
    /// Правая кнопка вкладку не закрывает.
    /// </summary>
    /// <remarks>
    /// За правой кнопкой человек идёт за меню, а не за закрытием. Закройся
    /// вкладка от неё — он потерял бы документ там, где ждал список действий.
    /// </remarks>
    [AvaloniaFact]
    public void The_right_button_does_not_close()
    {
        var (tab, window) = Shown(compact: false, "Dark");
        var close = Part(tab, "PART_Close");

        var asked = 0;
        tab.CloseRequested += (_, _) => asked++;

        var at = close.TranslatePoint(new Point(7, 7), window);

        Assert.NotNull(at);

        window.MouseMove(at.Value);
        window.MouseDown(at.Value, MouseButton.Right);
        window.MouseUp(at.Value, MouseButton.Right);
        window.UpdateLayout();

        Assert.Equal(0, asked);

        window.Close();
    }

    private static (AxTabItem Tab, Window Window) Shown(bool compact, string variant)
    {
        var tab = new AxTabItem { Content = "MainWindow.axaml", IsClosable = true };

        if (compact)
            tab.Classes.Add("compact");

        var window = new Window
        {
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = tab,
        };

        window.Show();
        window.UpdateLayout();

        return (tab, window);
    }

    private static Control Part(Control control, string name)
    {
        var part = control.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Name == name);

        Assert.True(part is not null, $"в шаблоне нет части {name}");
        return part!;
    }

    /// <summary>Подпись вкладки: тот презентер, что показывает её содержимое.</summary>
    private static ContentPresenter Name(AxTabItem tab) =>
        tab.GetVisualDescendants().OfType<ContentPresenter>().Single(presenter => Equals(presenter.Content, tab.Content));

    /// <summary>Кисть, которой путь значка действительно рисуется.</summary>
    private static IBrush? Stroke(AxTabItem tab) =>
        Part(tab, "PART_Icon").GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().Single().Stroke;

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key, string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        Assert.True(window.TryFindResource(key, theme, out var value), key);

        return (Color)value!;
    }
}
