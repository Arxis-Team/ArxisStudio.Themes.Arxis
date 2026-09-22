using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Переполнение полосы вкладок: до непоместившегося документа можно дойти.
/// </summary>
/// <remarks>
/// Прежде вкладки лежали в окне прокрутки с видимой полосой: при десятке документов часть имён
/// уходила за край, и достать их можно было только прокруткой — в среде, где переключение между
/// документами идёт десятками раз в час, это худший из способов. Полоса прокрутки под вкладками
/// снята, а непоместившиеся вкладки собраны в меню у правого края, как в Rider и Visual Studio.
/// </remarks>
public class TabOverflowTests
{
    /// <summary>Пока всё помещается, кнопки переполнения нет.</summary>
    [AvaloniaFact]
    public void A_strip_with_room_for_every_tab_shows_no_overflow_button()
    {
        var strip = Strip(400, "Program.cs", "App.axaml");
        var window = Shown(strip);

        Assert.False(Overflow(strip).IsVisible, "кнопка переполнения при свободном месте");

        window.Close();
    }

    /// <summary>Не поместившаяся вкладка выбирается из меню и приезжает на видное место.</summary>
    [AvaloniaFact]
    public void A_hidden_tab_is_chosen_from_the_overflow_menu()
    {
        var strip = Strip(160, "Program.cs", "App.axaml", "MainWindow.axaml", "StudioDock.cs");
        var window = Shown(strip);

        var button = Overflow(strip);

        Assert.True(button.IsVisible, "кнопка переполнения при нехватке места");

        var menu = Assert.IsAssignableFrom<MenuFlyout>(button.Flyout);
        var items = menu.Items.OfType<AxMenuItem>().ToList();

        Assert.NotEmpty(items);

        // Имя спрашивается до выбора: выбранная вкладка уходит из списка скрытых, а с ним и её
        // пункт — меню набирается заново, стоит перемениться тому, что не видно.
        var last = items[^1];
        var name = last.Header;

        last.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        window.UpdateLayout();

        Assert.Equal(name, strip.SelectedItem);

        var chosen = strip.GetRealizedContainers().OfType<AxTabItem>().Single(tab => tab.IsSelected);
        var scroll = strip.GetVisualDescendants().OfType<ScrollViewer>().Single(part => part.Name == "PART_Scroll");
        var left = chosen.Bounds.Left - scroll.Offset.X;

        Assert.InRange(left, -0.5d, scroll.Viewport.Width - chosen.Bounds.Width + 0.5d);

        window.Close();
    }

    /// <summary>Меню набирается заранее и идёт за тем, что сейчас не видно.</summary>
    /// <remarks>
    /// Набрать его перед самым показом нельзя: всплывающее окно строит себе содержимое один раз,
    /// когда его открывают впервые, и вкладки, положенные в меню по событию <c>Opening</c>, в
    /// него уже не попадали — у кнопки открывалась пустая рамка. Поэтому меню набирает раскладка,
    /// и проверяется оно там же: до всякого показа.
    /// </remarks>
    [AvaloniaFact]
    public void The_overflow_menu_follows_what_is_hidden()
    {
        var strip = Strip(160, "Program.cs", "App.axaml", "MainWindow.axaml", "StudioDock.cs");
        var window = Shown(strip);

        var menu = Assert.IsAssignableFrom<MenuFlyout>(Overflow(strip).Flyout);

        Assert.Contains("StudioDock.cs", Headers(menu));

        var scroll = strip.GetVisualDescendants().OfType<ScrollViewer>().Single(part => part.Name == "PART_Scroll");

        scroll.Offset = new Vector(scroll.Extent.Width - scroll.Viewport.Width, 0);
        window.UpdateLayout();

        var after = Headers(menu);

        Assert.DoesNotContain("StudioDock.cs", after);
        Assert.Contains("Program.cs", after);

        window.Close();
    }

    /// <summary>Переименованный документ назван в меню по-новому.</summary>
    /// <remarks>
    /// Подпись пункта привязана к подписи вкладки, а не списана с неё: набирается меню в
    /// раскладке, и списанная подпись держалась бы до следующей перемены в списке скрытых — то
    /// есть до тех пор, пока человек не тронет границу панели.
    /// </remarks>
    [AvaloniaFact]
    public void A_renamed_tab_is_named_anew_in_the_menu()
    {
        var strip = Strip(160, "Program.cs", "App.axaml", "MainWindow.axaml", "StudioDock.cs");
        var window = Shown(strip);

        var menu = Assert.IsAssignableFrom<MenuFlyout>(Overflow(strip).Flyout);
        var hidden = strip.GetRealizedContainers().OfType<AxTabItem>().Last();

        Assert.Contains("StudioDock.cs", Headers(menu));

        hidden.Content = "StudioDock.g.cs";
        window.UpdateLayout();

        Assert.Contains("StudioDock.g.cs", Headers(menu));

        window.Close();
    }

    /// <summary>Карточка меню встаёт под кнопкой, у её правого края, а не на поле под тень дальше.</summary>
    /// <remarks>
    /// Карточка стоит в попапе не вплотную: вокруг неё тема оставляет поле, на котором рисуется тень.
    /// Место попап отмеряет от своего края, и меню, поставленное «под кнопкой по правому краю», без
    /// поправки встаёт на ширину поля правее и ниже — оторванным от кнопки, которая его открыла.
    /// Поправку знает <see cref="AxMenuFlyout"/>, и меню полосы — такое же меню студии, как всякое
    /// другое.
    /// </remarks>
    [AvaloniaFact]
    public void The_overflow_menu_stands_under_the_button()
    {
        var strip = Strip(160, "Program.cs", "App.axaml", "MainWindow.axaml", "StudioDock.cs");

        // Окно повыше: меню, которому не хватило места под кнопкой, отпрыгнуло бы над ней.
        var window = Shown(strip, height: 400);
        var button = Overflow(strip);

        Assert.True(button.IsVisible, "кнопка переполнения при нехватке места");

        var corner = button.PointToScreen(new Point(button.Bounds.Width, button.Bounds.Height));
        var at = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;

        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);

        // Раскрытое меню встаёт на место кадром отрисовки: до него карточка стоит там, где её ещё
        // не двигали.
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        var card = window.GetVisualDescendants().OfType<MenuFlyoutPresenter>().Single().GetVisualChildren().First();
        var top = card.PointToScreen(new Point(card.Bounds.Width, 0));

        Assert.True(
            Math.Abs(top.X - corner.X) <= 1 && Math.Abs(top.Y - corner.Y) <= 1,
            $"карточка встала в {top}, а кнопка кончается в {corner}");

        window.Close();
    }

    private static List<string> Headers(MenuFlyout menu) =>
        [.. menu.Items.OfType<AxMenuItem>().Select(item => item.Header?.ToString() ?? string.Empty)];

    private static Button Overflow(AxTabStrip strip) =>
        strip.GetVisualDescendants().OfType<Button>().Single(part => part.Name == "PART_Overflow");

    private static AxTabStrip Strip(double width, params string[] items) =>
        new() { ItemsSource = items, Width = width, SelectedIndex = 0 };

    private static Window Shown(Control content, double height = 200)
    {
        var window = new Window
        {
            Width = 480,
            Height = height,
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = content,
        };

        window.Show();
        window.UpdateLayout();

        return window;
    }
}
