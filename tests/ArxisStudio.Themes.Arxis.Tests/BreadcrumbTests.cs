using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
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

        var menu = Assert.IsAssignableFrom<MenuFlyout>(Overflow(path).Flyout);
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

    /// <summary>
    /// Тяга раскрывает меню спрятанных уровней, не забирая клавиатуры; щелчок по «…» после этого
    /// раскрывает его по-прежнему — с клавиатурой в меню.
    /// </summary>
    /// <remarks>
    /// Тяга держит клавиатуру там, где начата: Esc и Ctrl, нажатые посреди неё, должны прийти туда.
    /// Режим «без клавиатуры» живёт до закрытия: остался бы — щелчок по «…» раскрывал бы меню, по
    /// которому не пройти стрелками.
    /// </remarks>
    [AvaloniaFact]
    public void A_drag_opens_the_overflow_without_taking_the_keyboard()
    {
        var path = Path(160, Deep);
        var outside = new AxButton { Content = "снаружи" };
        var window = Shown(new StackPanel { Children = { outside, path } }, height: 400);

        outside.Focus();

        Assert.True(path.OpenOverflow(), "меню спрятанных уровней не раскрылось");
        Assert.True(path.IsOverflowOpen);
        Assert.True(outside.IsFocused, "меню, раскрытое тягой, забрало клавиатуру");

        path.CloseOverflow();

        Assert.False(path.IsOverflowOpen, "меню не закрылось");

        var at = Center(Overflow(path), window);

        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);

        Assert.True(path.IsOverflowOpen, "щелчок не раскрыл меню");
        Assert.NotNull((window.FocusManager?.GetFocusedElement() as Visual)?.FindAncestorOfType<MenuFlyoutPresenter>(includeSelf: true));

        window.Close();
    }

    /// <summary>
    /// Под точкой экрана крошки называют сегмент — показанный в ряду, а при открытом меню и
    /// спрятанный, чей пункт под ней; «…» и полотно меню — место переполнения, не сегмент.
    /// </summary>
    /// <remarks>
    /// Точка экрана, потому что меню — отдельное окно: хозяин, который несёт на захвате указателя,
    /// видит курсор в координатах своего окна, а пункт меню лежит в чужом.
    /// </remarks>
    [AvaloniaFact]
    public void A_screen_point_names_the_segment_shown_or_hidden()
    {
        var path = Path(160, Deep);
        var window = Shown(path, height: 400);
        var segments = Segments(path);

        Assert.Same(segments[^1], path.SegmentAt(Screen(segments[^1])));
        Assert.True(path.IsOverflowAt(Screen(Overflow(path))), "«…» не место переполнения");
        Assert.Null(path.SegmentAt(Screen(Overflow(path))));

        path.OpenOverflow();

        var item = Menu(path)[0];

        Assert.Same(segments[0], path.SegmentAt(Screen(item)));
        Assert.True(path.IsOverflowAt(Screen(item)), "пункт меню — не место переполнения");
        Assert.False(path.IsOverflowAt(Screen(segments[^1])), "текущий сегмент — место переполнения");

        var hidden = Screen(item);

        path.CloseOverflow();

        Assert.Null(path.SegmentAt(hidden));

        window.Close();
    }

    /// <summary>Цель, поставленная спрятанному сегменту, видна на его пункте в меню, и только на нём.</summary>
    [AvaloniaFact]
    public void A_hidden_segment_marked_as_a_drop_target_marks_its_menu_item()
    {
        var path = Path(280, Deep);
        var window = Shown(path);
        var segments = Segments(path);
        var items = Menu(path);

        Assert.True(items.Count >= 2, "спрятан один уровень — отличить пункт от соседа нечем");

        segments[0].IsDropTarget = true;

        Assert.True(items[0].IsDropTarget, "пункт спрятанного сегмента не отмечен целью");
        Assert.False(items[1].IsDropTarget, "отмечен соседний пункт");

        // Меню, собранное заново, — ряд сузился и спрятал ещё уровень, — отметку не теряет.
        path.Width = 160;
        window.UpdateLayout();

        Assert.NotSame(items[0], Menu(path)[0]);
        Assert.True(Menu(path)[0].IsDropTarget, "пересобранное меню потеряло отметку цели");

        segments[0].IsDropTarget = false;

        Assert.False(Menu(path)[0].IsDropTarget, "отметка пункта осталась, когда цель сняли");

        window.Close();
    }

    /// <summary>
    /// Тяга из проводника над меню спрятанных уровней приходит к крошкам их событием, с курсором в их
    /// координатах, а ответ крошек уходит назад.
    /// </summary>
    /// <remarks>
    /// У всплывающего окна свой корень, и событие пункта меню до крошек само не доходит: хозяин,
    /// слушающий тягу на крошках, спрятанных уровней иначе не видел бы.
    /// </remarks>
    [AvaloniaFact]
    public void A_drag_over_the_overflow_menu_arrives_at_the_crumbs()
    {
        var path = Path(160, Deep);
        var window = Shown(path, height: 400);
        var answered = DragDropEffects.None;
        AxBreadcrumbItem? heard = null;

        DragDrop.SetAllowDrop(path, true);
        path.AddHandler(DragDrop.DragOverEvent, (_, e) =>
        {
            heard = path.SegmentAt(path.PointToScreen(e.GetPosition(path)));
            e.DragEffects = DragDropEffects.Copy;
            e.Handled = true;
        });
        path.OpenOverflow();

        // Раскрытое меню встаёт на место и попадает в сцену кадром отрисовки: до него курсор, принесённый
        // на пункт, пришёлся бы на пустое место.
        Frame();

        // Ответ читается на полотне меню, после пересылки: дальше всплывающего окна событие не идёт.
        var item = Menu(path)[0];

        Assert.IsAssignableFrom<Control>(item.Parent).AddHandler(
            DragDrop.DragOverEvent, (_, e) => answered = e.DragEffects, handledEventsToo: true);

        var data = new DataTransfer();

        data.Add(DataTransferItem.Create(DataFormat.Text, "notes.md"));

        var at = Center(item, window);

        window.DragDrop(at, RawDragEventType.DragEnter, data, DragDropEffects.Copy | DragDropEffects.Move, RawInputModifiers.None);
        window.DragDrop(at, RawDragEventType.DragOver, data, DragDropEffects.Copy | DragDropEffects.Move, RawInputModifiers.None);

        Assert.Same(Segments(path)[0], heard);
        Assert.Equal(DragDropEffects.Copy, answered);

        window.Close();
    }

    /// <summary>
    /// Меню, раскрытое тягой, окна не заслоняет: под точкой мимо него — то, что там показано.
    /// </summary>
    /// <remarks>
    /// Под всплывающее с лёгким закрытием Avalonia стелет поверх окна прозрачный слой, которым ловит
    /// щелчок мимо, и в нём тонет всякое попадание в окно — попадание системной тяги тоже. Раскрытое
    /// тягой меню сделало бы недосягаемым всё, над чем несут, и закрыть его было бы уже нечем.
    /// </remarks>
    [AvaloniaFact]
    public void A_drag_opened_overflow_leaves_the_window_reachable()
    {
        var path = Path(160, Deep);
        var below = new AxButton { Content = "под крошками" };

        DockPanel.SetDock(path, Dock.Top);
        DockPanel.SetDock(below, Dock.Bottom);

        var window = Shown(new DockPanel { LastChildFill = false, Children = { path, below } }, height: 400);

        Assert.True(path.OpenOverflow(), "меню спрятанных уровней не раскрылось");

        Frame();

        var beside = Center(below, window);

        Assert.Same(below, (window.InputHitTest(beside) as Visual)?.FindAncestorOfType<AxButton>(includeSelf: true));

        // Закрывшись, меню возвращает слой: раскрытое щелчком закрывается щелчком мимо, как всегда.
        path.CloseOverflow();

        var clicked = 0;
        var at = Center(Overflow(path), window);

        below.Click += (_, _) => clicked++;

        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        Frame();

        Assert.True(path.IsOverflowOpen, "щелчок не раскрыл меню");

        window.MouseDown(beside, MouseButton.Left);
        window.MouseUp(beside, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.False(path.IsOverflowOpen, "щелчок мимо не закрыл меню");
        Assert.True(clicked == 0, "щелчок, закрывший меню, дошёл и до кнопки под ним");

        window.Close();
    }

    /// <summary>
    /// Карточка меню встаёт под «…», а не на поле под тень дальше, и место переполнения — она: сквозь
    /// поле видно то, что лежит под меню, и несут туда, к нему.
    /// </summary>
    [AvaloniaFact]
    public void The_overflow_menu_stands_under_the_button()
    {
        var path = Path(160, Deep);

        path.Margin = new Thickness(40, 40, 0, 0);

        var window = Shown(path, height: 400);
        var button = Overflow(path);
        var corner = button.PointToScreen(new Point(0, button.Bounds.Height));

        path.OpenOverflow();
        Frame();

        var card = Card(window);
        var top = card.PointToScreen(default);
        var bottom = card.PointToScreen(new Point(0, card.Bounds.Height));

        Assert.True(
            Math.Abs(top.X - corner.X) <= 1 && Math.Abs(top.Y - corner.Y) <= 1,
            $"карточка встала в {top}, а «…» кончается в {corner}");
        Assert.True(path.IsOverflowAt(top + new PixelVector(4, 4)), "карточка — не место переполнения");
        Assert.False(path.IsOverflowAt(bottom + new PixelVector(-4, 4)), "поле под тень сочтено меню");

        window.Close();
    }

    /// <summary>
    /// Целью сброса бывает полотно меню вслед за крошками — и только оно: окно всплывающего шире
    /// карточки на поле под тень, и сброс, пришедший на поле, разобрать некому.
    /// </summary>
    /// <remarks>
    /// Цель, у которой нет обработчика, отвечает источнику тем, что он предложил: проводник счёл бы
    /// сброс принятым и мог бы убрать у себя отпущенное.
    /// </remarks>
    [AvaloniaFact]
    public void The_overflow_menu_is_a_target_only_where_the_crumbs_are()
    {
        var path = Path(160, Deep);
        var window = Shown(path, height: 400);

        DragDrop.SetAllowDrop(path, true);
        path.OpenOverflow();
        Frame();

        var presenter = Presenter(window);

        Assert.True(DragDrop.GetAllowDrop(presenter), "полотно меню не стало целью вслед за крошками");
        Assert.True(DragDrop.GetAllowDrop(Menu(path)[0]), "пункт спрятанного уровня не стал целью");
        Assert.False(
            DragDrop.GetAllowDrop(Assert.IsAssignableFrom<Interactive>(presenter.Parent)),
            "окно меню — цель, а разобрать сброс на нём некому");

        DragDrop.SetAllowDrop(path, false);

        Assert.False(DragDrop.GetAllowDrop(presenter), "полотно осталось целью, когда крошки перестали ею быть");

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

    /// <summary>
    /// Прогоняет кадр отрисовки: раскрытое меню встаёт на место и попадает в сцену им — до кадра
    /// точка над пунктом пришлась бы на пустое место.
    /// </summary>
    private static void Frame()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>Полотно раскрытого меню спрятанных уровней.</summary>
    private static MenuFlyoutPresenter Presenter(Window window) =>
        window.GetVisualDescendants().OfType<MenuFlyoutPresenter>().Single();

    /// <summary>Видимая карточка раскрытого меню — полотно без поля под тень.</summary>
    private static Visual Card(Window window) => Presenter(window).GetVisualChildren().First();

    /// <summary>Пункты меню спрятанных уровней — по порядку уровней.</summary>
    private static List<AxMenuItem> Menu(AxBreadcrumb path) =>
        [.. Assert.IsAssignableFrom<MenuFlyout>(Overflow(path).Flyout).Items.OfType<AxMenuItem>()];

    /// <summary>Середина элемента в точках экрана.</summary>
    private static PixelPoint Screen(Visual visual) =>
        visual.PointToScreen(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2));

    /// <summary>Середина элемента в точках окна.</summary>
    private static Point Center(Visual visual, Window window) =>
        visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), window)!.Value;

    private static Window Shown(Control content, double height = 120)
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
