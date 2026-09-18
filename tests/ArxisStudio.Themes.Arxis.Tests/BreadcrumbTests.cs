using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Крошки навигации: путь, по которому переходят, и переполнение, при котором он не теряется.
/// </summary>
/// <remarks>
/// Крошки вернулись в SDK 7.1 с первым потребителем — правой колонкой окна проекта студии. Здесь
/// проверяется договор контрола: выбор сегмента называет его данные и номер, текущий сегмент
/// никуда не ведёт, а узкая колонка прячет ведущие сегменты в меню, оставляя последний.
/// </remarks>
public class BreadcrumbTests
{
    private static readonly string[] Deep = ["TestApp", "TestApp", "ViewModels", "Generated", "Templates"];

    /// <summary>Выбранный сегмент называет свои данные и номер от корня.</summary>
    [AvaloniaFact]
    public void Choosing_a_segment_names_its_item_and_index()
    {
        var path = Path(480, "TestApp", "MyLib", "Views");
        var window = Shown(path);
        var heard = Heard(path);

        Click(Segments(path)[1]);

        var navigated = Assert.Single(heard);

        Assert.Equal("MyLib", navigated.Item);
        Assert.Equal(1, navigated.Index);

        window.Close();
    }

    /// <summary>Текущий сегмент никуда не ведёт: туда человек уже пришёл.</summary>
    [AvaloniaFact]
    public void The_current_segment_navigates_nowhere()
    {
        var path = Path(480, "TestApp", "MyLib", "Views");
        var window = Shown(path);
        var heard = Heard(path);

        Click(Segments(path)[^1]);

        Assert.Empty(heard);

        window.Close();
    }

    /// <summary>
    /// Первый и текущий сегменты помечены, и пометки идут за путём.
    /// </summary>
    /// <remarks>
    /// Путь растёт и укорачивается на ходу: сегмент, бывший текущим, после шага вглубь им быть
    /// перестаёт, и пометка при создании контейнера этого не знала бы.
    /// </remarks>
    [AvaloniaFact]
    public void The_first_and_the_current_segment_follow_the_path()
    {
        var items = new Avalonia.Collections.AvaloniaList<string> { "TestApp", "MyLib" };
        var path = new AxBreadcrumb { ItemsSource = items, Width = 480 };
        var window = Shown(path);

        var before = Segments(path);

        Assert.True(before[0].Classes.Contains(":first"), "первый сегмент без пометки");
        Assert.True(before[1].IsCurrent, "последний сегмент не текущий");

        items.Add("Views");
        window.UpdateLayout();

        var after = Segments(path);

        Assert.False(after[1].IsCurrent, "прежний последний остался текущим после шага вглубь");
        Assert.True(after[2].IsCurrent, "новый последний сегмент не стал текущим");

        window.Close();
    }

    /// <summary>Разделитель стоит между сегментами, а перед первым его нет.</summary>
    [AvaloniaFact]
    public void The_separator_stands_between_segments_only()
    {
        var path = Path(480, "TestApp", "MyLib");
        var window = Shown(path);
        var segments = Segments(path);

        Assert.False(Separator(segments[0]).IsVisible, "разделитель перед первым сегментом");
        Assert.True(Separator(segments[1]).IsVisible, "нет разделителя между сегментами");
        Assert.False(Separator(segments[1]).IsHitTestVisible, "разделитель ловит щелчок");

        window.Close();
    }

    /// <summary>Пока путь помещается, кнопки переполнения нет.</summary>
    [AvaloniaFact]
    public void A_path_with_room_shows_no_overflow_button()
    {
        var path = Path(480, "TestApp", "MyLib");
        var window = Shown(path);

        Assert.False(Overflow(path).IsVisible, "кнопка переполнения при свободном месте");
        Assert.All(Segments(path), segment => Assert.True(segment.Bounds.Left >= 0, "сегмент спрятан без нужды"));

        window.Close();
    }

    /// <summary>
    /// Узкая колонка прячет ведущие сегменты, а текущий оставляет целиком в своих границах.
    /// </summary>
    [AvaloniaFact]
    public void A_narrow_path_hides_the_leading_segments_and_keeps_the_current_one()
    {
        var path = Path(160, Deep);
        var window = Shown(path);
        var segments = Segments(path);
        var current = segments[^1];

        Assert.True(Overflow(path).IsVisible, "нет кнопки переполнения при нехватке места");
        Assert.True(current.Bounds.Left >= 0 && current.Bounds.Width > 0, "текущий сегмент спрятан");
        Assert.True(current.Bounds.Right <= path.Bounds.Width + 0.5, "текущий сегмент вышел за край");
        // Спрятанный сегмент стоит за левым краем ряда, и ряд его обрезает.
        Assert.True(segments[0].Bounds.Right <= 0.5, $"спрятанный сегмент стоит в ряду: {segments[0].Bounds}");

        // Кнопка переполнения стоит у левого края и сегменты не закрывает.
        var shown = segments.Where(segment => segment.Bounds.Left >= 0).ToList();

        Assert.True(
            shown[0].Bounds.Left >= Overflow(path).Bounds.Right - 0.5,
            $"кнопка переполнения {Overflow(path).Bounds} легла на сегмент {shown[0].Bounds}");

        window.Close();
    }

    /// <summary>Спрятанный сегмент выбирается из меню переполнения и называет свой номер.</summary>
    [AvaloniaFact]
    public void A_hidden_segment_is_chosen_from_the_overflow_menu()
    {
        var path = Path(160, Deep);
        var window = Shown(path);
        var heard = Heard(path);

        var menu = Assert.IsType<MenuFlyout>(Overflow(path).Flyout);
        var items = menu.Items.OfType<AxMenuItem>().ToList();

        Assert.Equal("TestApp", items[0].Header);

        items[0].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

        Assert.Equal(0, Assert.Single(heard).Index);

        window.Close();
    }

    /// <summary>
    /// Спрятанный сегмент фокуса не берёт.
    /// </summary>
    /// <remarks>
    /// Рамка у него нулевая, и кольцо фокуса на нём было бы невидимо: Tab уводил бы клавиатуру в
    /// место, которого нет на экране.
    /// </remarks>
    [AvaloniaFact]
    public void A_hidden_segment_takes_no_keyboard_focus()
    {
        var path = Path(160, Deep);
        var window = Shown(path);
        var segments = Segments(path);

        Assert.False(segments[0].Focusable, "спрятанный сегмент берёт фокус");
        Assert.True(segments[^1].Focusable, "текущий сегмент не берёт фокус");

        window.Close();
    }

    /// <summary>
    /// Стрелки ходят по видимым сегментам, а с первого шагают на кнопку переполнения.
    /// </summary>
    [AvaloniaFact]
    public void Arrows_walk_the_shown_segments_and_step_onto_the_overflow_button()
    {
        var path = Path(280, Deep);
        var window = Shown(path);
        var shown = Segments(path).Where(segment => segment.Bounds.Left >= 0).ToList();

        Assert.True(shown.Count >= 2, "в узкой колонке виден один сегмент — ходить некуда");

        shown[^1].Focus();
        window.KeyPress(Key.Home, RawInputModifiers.None, PhysicalKey.Home, string.Empty);

        Assert.True(shown[0].IsFocused, "Home не привёл к первому видимому сегменту");

        window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, string.Empty);

        Assert.True(Overflow(path).IsFocused, "шаг влево с первого видимого не дошёл до кнопки переполнения");

        window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, string.Empty);

        Assert.True(shown[0].IsFocused, "шаг вправо с кнопки переполнения не вернулся к сегменту");

        window.KeyPress(Key.End, RawInputModifiers.None, PhysicalKey.End, string.Empty);

        Assert.True(shown[^1].IsFocused, "End не привёл к текущему сегменту");

        window.Close();
    }

    /// <summary>Кнопка переполнения названа: иконочную кнопку без имени диктор читает словом «кнопка».</summary>
    [AvaloniaFact]
    public void The_overflow_button_is_named()
    {
        var path = Path(160, Deep);
        var window = Shown(path);

        Assert.False(string.IsNullOrWhiteSpace(Avalonia.Automation.AutomationProperties.GetName(Overflow(path))));

        window.Close();
    }

    private static AxBreadcrumb Path(double width, params string[] items) =>
        new() { ItemsSource = items, Width = width, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };

    private static List<AxBreadcrumbItem> Segments(AxBreadcrumb path) =>
        [.. path.GetVisualDescendants().OfType<AxBreadcrumbItem>().OrderBy(segment => path.IndexFromContainer(segment))];

    private static Button Overflow(AxBreadcrumb path) =>
        path.GetVisualDescendants().OfType<Button>().Single(part => part.Name == "PART_Overflow");

    private static Control Separator(AxBreadcrumbItem segment) =>
        segment.GetVisualDescendants().OfType<Control>().Single(part => part.Name == "PART_Separator");

    private static List<AxBreadcrumbNavigatedEventArgs> Heard(AxBreadcrumb path)
    {
        var heard = new List<AxBreadcrumbNavigatedEventArgs>();

        path.Navigated += (_, e) => heard.Add(e);

        return heard;
    }

    private static void Click(AxBreadcrumbItem segment) =>
        segment.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static Window Shown(Control content)
    {
        var window = new Window
        {
            Width = 480,
            Height = 120,
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = content,
        };

        window.Show();
        window.UpdateLayout();

        return window;
    }
}
