using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Палитра и образцы выбора цвета — разделы окна Color у Unity: встроенная палитра, библиотеки образцов хозяина,
/// сохранение текущего цвета с именем, замена, удаление, порядок, вид и выбор библиотеки.
/// </summary>
public class ColorPresetTests
{
    /// <summary>Палитра — двенадцать оттенков в четырёх тонах и двенадцать серых; её цвет встаёт с прежней прозрачностью.</summary>
    [AvaloniaFact]
    public void The_palette_gives_its_color_and_keeps_the_alpha()
    {
        var (picker, window) = Shown(null, 0x803C6ECF);
        var committed = Committed(picker);

        Assert.Equal(60, AxColorPicker.Defaults.Count);
        Assert.Equal(Colors.White, AxColorPicker.Defaults[48]);
        Assert.Equal(Colors.Black, AxColorPicker.Defaults[^1]);

        picker.IsPaletteExpanded = true;
        window.UpdateLayout();

        var cells = Cells(picker, "PART_Palette");

        Assert.Equal(60, cells.Count);

        // Ряд палитры — тон по двенадцати оттенкам: двенадцатая ячейка в первом ряду, тринадцатая начинает второй.
        Assert.Equal(cells[0].Bounds.Y, cells[11].Bounds.Y);
        Assert.True(cells[12].Bounds.Y > cells[0].Bounds.Y, "тринадцатая ячейка палитры осталась в первом ряду");
        Assert.Equal(cells[0].Bounds.X, cells[12].Bounds.X);

        Click(cells[0]);

        var expected = AxColorPicker.Defaults[0];

        Assert.Equal(Color.FromArgb(0x80, expected.R, expected.G, expected.B), picker.Color);
        Assert.Single(committed);

        window.Close();
    }

    /// <summary>
    /// Раскрытая палитра и образцы списком не раздвигают выбор: окно меряет его по содержимому, и сетка без предела
    /// встала бы одной строкой — живая проверка нашла окно шириной в тысячу точек (запись 376).
    /// </summary>
    [AvaloniaFact]
    public void The_palette_and_the_swatches_wrap_within_the_picker()
    {
        var store = new FakeStore(("A very long swatch name that would not fit in the picker at all", 0xFFE0A040));
        var picker = new AxColorPicker { PresetStore = store, IsPaletteExpanded = true };
        var window = new Window { SizeToContent = SizeToContent.WidthAndHeight, Content = picker };
        var width = Application.Current!.TryFindResource("AxColorPickerWidth", out var found) && found is double value ? value : 0;

        window.Show();
        window.UpdateLayout();

        Assert.True(width > 0);
        Assert.True(picker.Bounds.Width <= width + 0.5, $"палитра раздвинула выбор до {picker.Bounds.Width}");
        Assert.True(Part<Panel>(picker, "PART_Palette").Bounds.Height > 0, "палитра не раскрылась");

        store.View = AxColorPresetView.List;
        window.UpdateLayout();

        var list = Part<Panel>(picker, "PART_PresetList");

        Assert.True(picker.Bounds.Width <= width + 0.5, $"длинное имя образца раздвинуло выбор до {picker.Bounds.Width}");
        Assert.True(list.Bounds.Width <= width + 0.5, $"список образцов вылез за выбор: {list.Bounds.Width}");

        window.Close();
    }

    /// <summary>
    /// Палитра держит двенадцать в ряд в любой плотности: зазор её сетки — часть её устройства, а не воздух ступени.
    /// </summary>
    /// <remarks>
    /// Сетка брала зазор у шкалы расстояний, а ширину — постоянной, под зазор в два: просторная ступень делала зазор
    /// четыре, в ряд вставало десять, и ряды палитры — тон по двенадцати оттенкам — перемешивались.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Compact")]
    [InlineData("Normal")]
    [InlineData("Comfortable")]
    public void The_palette_keeps_twelve_in_a_row_in_every_tier(string tier)
    {
        var picker = new AxColorPicker { IsPaletteExpanded = true };
        var window = new Window { Width = 420, Height = 900, Content = picker };
        var uri = new Uri($"avares://ArxisStudio.Themes.Arxis/Density/{tier}.axaml");

        window.Resources.MergedDictionaries.Add(new Avalonia.Markup.Xaml.Styling.ResourceInclude(uri) { Source = uri });
        window.Show();
        window.UpdateLayout();

        var cells = Part<Panel>(picker, "PART_Palette").Children.OfType<Control>().ToList();

        Assert.Equal(cells[0].Bounds.Y, cells[11].Bounds.Y);
        Assert.True(cells[12].Bounds.Y > cells[11].Bounds.Y, "тринадцатая ячейка встала в первый ряд");

        window.Close();
    }

    /// <summary>Без хранилища раздела образцов нет — палитра остаётся.</summary>
    [AvaloniaFact]
    public void Without_a_store_there_are_no_swatches()
    {
        var (picker, window) = Shown(null);

        Assert.False(Part<Control>(picker, "PART_PresetsSection").IsVisible, "раздел образцов виден без хранилища");
        Assert.True(Part<Control>(picker, "PART_PaletteHeader").IsVisible);

        window.Close();
    }

    /// <summary>
    /// Последняя ячейка — текущий цвет: щелчок спрашивает имя, Enter сохраняет образец одной записью. Esc в поле имени
    /// закрывает его и до окна не доходит — там он отменил бы весь сеанс.
    /// </summary>
    [AvaloniaFact]
    public void The_last_cell_saves_the_current_color_under_a_name()
    {
        var store = new FakeStore(("Brand", 0xFFE0A040));
        var (picker, window) = Shown(store, 0x803C6ECF);
        var escapes = 0;

        window.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Escape)
                escapes++;
        });

        var cells = Cells(picker, "PART_PresetGrid");

        Assert.Equal(2, cells.Count);
        Assert.Equal("Save the current color", ToolTip.GetTip(cells[^1]));

        Click(cells[^1]);

        var name = NameBox(picker);

        Assert.Equal("#3C6ECF", name.Text);

        name.Text = "Ocean";
        Press(name, Key.Enter);

        Assert.False(NamePopup(picker).IsOpen, "Enter не закрыл поле имени");
        Assert.True(Cells(picker, "PART_PresetGrid")[^1].IsFocused, "каретка после сохранения не вернулась к ячейке «сохранить»");
        Assert.Equal(new AxColorPreset("Ocean", Color.FromUInt32(0x803C6ECF)), store.Current.Presets[^1]);
        Assert.Equal(1, store.Saves);
        Assert.Equal(3, Cells(picker, "PART_PresetGrid").Count);

        Click(Cells(picker, "PART_PresetGrid")[^1]);
        Press(NameBox(picker), Key.Escape);

        Assert.False(NamePopup(picker).IsOpen, "Esc не закрыл поле имени");
        Assert.Equal(0, escapes);
        Assert.Equal(1, store.Saves);

        window.Close();
    }

    /// <summary>Щелчок по образцу ставит его цвет — с его прозрачностью — одной фиксацией.</summary>
    [AvaloniaFact]
    public void A_swatch_gives_its_color_with_its_alpha()
    {
        var store = new FakeStore(("Glass", 0x40E0A040));
        var (picker, window) = Shown(store);
        var committed = Committed(picker);

        Click(Cells(picker, "PART_PresetGrid")[0]);

        Assert.Equal(Color.FromUInt32(0x40E0A040), picker.Color);
        Assert.Single(committed);

        window.Close();
    }

    /// <summary>Меню образца: «Заменить» ставит ему текущий цвет, «Удалить» снимает его; каждое — запись.</summary>
    [AvaloniaFact]
    public void The_swatch_menu_replaces_and_deletes()
    {
        var store = new FakeStore(("Brand", 0xFFE0A040), ("Accent", 0xFF20A060));
        var (picker, window) = Shown(store, 0xFF3C6ECF);

        Item(ContextMenu(Cells(picker, "PART_PresetGrid")[0]), "Replace").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

        Assert.Equal(new AxColorPreset("Brand", Color.FromUInt32(0xFF3C6ECF)), store.Current.Presets[0]);

        var delete = Item(ContextMenu(Cells(picker, "PART_PresetGrid")[1]), "Delete");

        Assert.True(delete.IsDestructive, "«Удалить» не помечено необратимым");

        delete.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

        Assert.Equal(["Brand"], store.Current.Presets.Select(preset => preset.Name));
        Assert.Equal(2, store.Saves);
        Assert.Equal(2, Cells(picker, "PART_PresetGrid").Count);

        window.Close();
    }

    /// <summary>Тяга образца на место другого меняет порядок одной записью, и щелчком она не считается.</summary>
    [AvaloniaFact]
    public void A_drag_reorders_the_swatches_and_saves_once()
    {
        var store = new FakeStore(("A", 0xFFFF0000), ("B", 0xFF00FF00), ("C", 0xFF0000FF));
        var (picker, window) = Shown(store, 0xFF3C6ECF);
        var committed = Committed(picker);
        var cells = Cells(picker, "PART_PresetGrid");
        var (first, second, third) = (Center(cells[0], window), Center(cells[1], window), Center(cells[2], window));

        // Между движениями — раскладка и такт: ячейка, которую тянут, встаёт под указатель, и отпускание на ней — тот
        // самый щелчок кнопки, который после тяги считаться не должен.
        window.MouseDown(first, MouseButton.Left);
        window.MouseMove(second);
        Settle(window);
        window.MouseMove(third);
        Settle(window);

        Assert.True(cells[0].IsPointerOver, "ячейку, которую тянут, не поставили под указатель");

        window.MouseUp(third, MouseButton.Left);
        Pump();

        Assert.Equal(["B", "C", "A"], store.Current.Presets.Select(preset => preset.Name));
        Assert.Equal(1, store.Saves);
        Assert.Empty(committed);
        Assert.Equal(Color.FromUInt32(0xFF3C6ECF), picker.Color);

        window.Close();
    }

    /// <summary>Esc посреди тяги — порядок прежний, записи нет.</summary>
    [AvaloniaFact]
    public void Escape_cancels_a_drag_of_a_swatch()
    {
        var store = new FakeStore(("A", 0xFFFF0000), ("B", 0xFF00FF00), ("C", 0xFF0000FF));
        var (picker, window) = Shown(store);
        var cells = Cells(picker, "PART_PresetGrid");
        var over = Center(cells[2], window);

        window.MouseDown(Center(cells[0], window), MouseButton.Left);
        window.MouseMove(over);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, string.Empty);
        window.MouseUp(over, MouseButton.Left);
        Pump();

        Assert.Equal(["A", "B", "C"], store.Current.Presets.Select(preset => preset.Name));
        Assert.Equal(0, store.Saves);
        Assert.Equal(["A", "B", "C"], Cells(picker, "PART_PresetGrid").SkipLast(1).Select(cell => ((AxColorPreset)cell.Tag!).Name));

        window.Close();
    }

    /// <summary>
    /// Равные образцы — разные образцы: меню, Alt+стрелка и тяга правят тот, у которого их позвали.
    /// </summary>
    /// <remarks>
    /// Образец — запись, и место его искалось по равенству: «Заменить» у второго из двух одинаковых перекрашивало первый,
    /// Alt+стрелка двигала первый, а тяга ставила их не в том порядке, в каком стояли ячейки.
    /// </remarks>
    [AvaloniaFact]
    public void Equal_swatches_are_still_different_swatches()
    {
        var store = new FakeStore(("Same", 0xFFFF0000), ("Same", 0xFFFF0000), ("Other", 0xFF00FF00));
        var (picker, window) = Shown(store, 0xFF3C6ECF);
        var first = store.Current.Presets[0];

        Item(ContextMenu(Cells(picker, "PART_PresetGrid")[1]), "Replace").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

        Assert.Same(first, store.Current.Presets[0]);
        Assert.Equal(Color.FromUInt32(0xFF3C6ECF), store.Current.Presets[1].Color);

        window.Close();

        store = new FakeStore(("Same", 0xFFFF0000), ("Same", 0xFFFF0000), ("Other", 0xFF00FF00));
        (picker, window) = Shown(store);

        var second = store.Current.Presets[1];

        Assert.True(Cells(picker, "PART_PresetGrid")[1].Focus(NavigationMethod.Tab));

        window.KeyPress(Key.Right, RawInputModifiers.Alt, PhysicalKey.None, string.Empty);

        Assert.Equal(["Same", "Other", "Same"], store.Current.Presets.Select(preset => preset.Name));
        Assert.Same(second, store.Current.Presets[2]);

        window.Close();

        store = new FakeStore(("Same", 0xFFFF0000), ("Same", 0xFFFF0000), ("Other", 0xFF00FF00));
        (picker, window) = Shown(store);
        second = store.Current.Presets[1];

        var cells = Cells(picker, "PART_PresetGrid");
        var (from, to) = (Center(cells[1], window), Center(cells[2], window));

        window.MouseDown(from, MouseButton.Left);
        window.MouseMove(to);
        Settle(window);
        window.MouseUp(to, MouseButton.Left);
        Pump();

        Assert.Equal(["Same", "Other", "Same"], store.Current.Presets.Select(preset => preset.Name));
        Assert.Same(second, store.Current.Presets[2]);

        window.Close();
    }

    /// <summary>Alt+стрелка двигает образец с клавиатуры, и каретка идёт за ним.</summary>
    [AvaloniaFact]
    public void Alt_arrows_move_a_swatch()
    {
        var store = new FakeStore(("A", 0xFFFF0000), ("B", 0xFF00FF00), ("C", 0xFF0000FF));
        var (picker, window) = Shown(store);

        Assert.True(Cells(picker, "PART_PresetGrid")[0].Focus(NavigationMethod.Tab));

        window.KeyPress(Key.Right, RawInputModifiers.Alt, PhysicalKey.None, string.Empty);
        window.KeyPress(Key.Right, RawInputModifiers.Alt, PhysicalKey.None, string.Empty);

        Assert.Equal(["B", "C", "A"], store.Current.Presets.Select(preset => preset.Name));
        Assert.Equal(2, store.Saves);
        Assert.Equal("A", ((AxColorPreset)Cells(picker, "PART_PresetGrid")[2].Tag!).Name);
        Assert.True(Cells(picker, "PART_PresetGrid")[2].IsFocused, "каретка не пошла за образцом");

        window.Close();
    }

    /// <summary>
    /// Меню ⋮: вид сеткой или списком — в списке у образца имя, — библиотеки с отметкой текущей и новая библиотека в
    /// профиле; в решении — только когда хранилище его знает.
    /// </summary>
    [AvaloniaFact]
    public void The_menu_switches_the_view_and_the_library_and_names_a_new_one()
    {
        var store = new FakeStore(("Brand", 0xFFE0A040));
        var team = new AxColorPresetLibrary("Team", AxColorPresetLocation.Project, [new AxColorPreset("Logo", Colors.Navy)]);

        store.Add(team);

        var (picker, window) = Shown(store);
        var menu = OpenMenu(picker);

        Assert.True(Item(menu, "Grid").IsChecked);
        Assert.True(Item(menu, "My colors").IsChecked);
        Assert.False(Item(menu, "Team").IsChecked);
        Assert.False(Item(menu, "New library in solution…").IsEnabled, "в решении можно завести библиотеку без решения");

        Item(menu, "List").Command!.Execute(null);
        window.UpdateLayout();

        Assert.Equal(AxColorPresetView.List, store.View);
        Assert.False(Part<Control>(picker, "PART_PresetGrid").IsVisible);
        Assert.Contains("Brand", Cells(picker, "PART_PresetList")[0].GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text));

        Item(OpenMenu(picker), "Team").Command!.Execute(null);

        Assert.Same(team, store.Current);
        Assert.Equal("Logo", ((AxColorPreset)Cells(picker, "PART_PresetList")[0].Tag!).Name);

        Item(OpenMenu(picker), "New library in profile…").Command!.Execute(null);
        Pump();

        var name = NameBox(picker);

        Assert.Equal(string.Empty, name.Text);

        name.Text = "Moodboard";
        Press(name, Key.Enter);

        Assert.Equal(("Moodboard", AxColorPresetLocation.User), (store.Current.Name, store.Current.Location));
        Assert.Single(Cells(picker, "PART_PresetList"));

        window.Close();
    }

    private static (AxColorPicker Picker, Window Window) Shown(FakeStore? store, uint color = 0xFF3C6ECF)
    {
        var picker = new AxColorPicker { Color = Color.FromUInt32(color), PresetStore = store };
        var window = new Window
        {
            Width = 420,
            Height = 900,
            Content = new Border { Padding = new Thickness(20), Child = picker },
        };

        window.Show();
        window.UpdateLayout();
        Pump();

        return (picker, window);
    }

    private static List<Color> Committed(AxColorPicker picker)
    {
        var committed = new List<Color>();

        picker.ColorCommitted += (_, e) => committed.Add(e.Color);

        return committed;
    }

    private static List<AxButton> Cells(AxColorPicker picker, string panel) =>
        Part<Panel>(picker, panel).Children.OfType<AxButton>().ToList();

    private static T Part<T>(Control owner, string name)
        where T : Control =>
        owner.GetVisualDescendants().OfType<T>().First(part => part.Name == name);

    private static Popup NamePopup(AxColorPicker picker) => Part<Popup>(picker, "PART_NamePopup");

    /// <summary>Поле имени во всплывающем — оно живёт в своём корне, и ищут его от попапа.</summary>
    private static TextBox NameBox(AxColorPicker picker)
    {
        var popup = NamePopup(picker);

        Assert.True(popup.IsOpen, "поле имени не открылось");

        return Assert.IsAssignableFrom<Control>(popup.Child).GetVisualDescendants().OfType<TextBox>().Single();
    }

    /// <summary>Меню образца правой кнопкой — открытым: подписи пунктов приходят из темы, когда меню встало.</summary>
    private static AxMenuFlyout ContextMenu(Control cell)
    {
        var menu = Assert.IsType<AxMenuFlyout>(cell.ContextFlyout);

        menu.ShowAt(cell);
        Pump();

        return menu;
    }

    private static AxMenuFlyout OpenMenu(AxColorPicker picker)
    {
        Click(Part<Button>(picker, "PART_PresetsMenu"));

        return Assert.IsType<AxMenuFlyout>(picker.PresetsFlyout);
    }

    private static AxMenuItem Item(AxMenuFlyout menu, string header) =>
        menu.Items.OfType<AxMenuItem>().Single(item => Equals(item.Header, header));

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static void Press(Control target, Key key)
    {
        var root = Assert.IsAssignableFrom<TopLevel>(TopLevel.GetTopLevel(target));

        target.Focus();
        root.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, string.Empty);
        Pump();
    }

    private static Point Center(Control cell, Window window) =>
        cell.TranslatePoint(new Point(cell.Bounds.Width / 2, cell.Bounds.Height / 2), window)!.Value;

    /// <summary>Раскладка и такт отрисовки: попадание указателя безголовая платформа считает по отрисованной сцене.</summary>
    private static void Settle(Window window)
    {
        window.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    }

    private static void Pump()
    {
        for (var at = 0; at < 5; at++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }

    /// <summary>Хранилище в памяти: библиотека «My colors» с образцами и счёт записей.</summary>
    private sealed class FakeStore : IAxColorPresetStore
    {
        private readonly List<AxColorPresetLibrary> _libraries = [];
        private AxColorPresetView _view;

        public FakeStore(params (string Name, uint Color)[] presets)
        {
            Current = new AxColorPresetLibrary(
                "My colors", AxColorPresetLocation.User, presets.Select(preset => new AxColorPreset(preset.Name, Color.FromUInt32(preset.Color))));
            _libraries.Add(Current);
        }

        public event EventHandler? Changed;

        public IReadOnlyList<AxColorPresetLibrary> Libraries => _libraries;

        public AxColorPresetLibrary Current { get; private set; }

        public AxColorPresetView View
        {
            get => _view;
            set
            {
                _view = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public int Saves { get; private set; }

        public void Add(AxColorPresetLibrary library) => _libraries.Add(library);

        public bool CanCreate(AxColorPresetLocation location) => location == AxColorPresetLocation.User;

        public void Select(AxColorPresetLibrary library)
        {
            Current = library;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public AxColorPresetLibrary Create(string name, AxColorPresetLocation location)
        {
            var library = new AxColorPresetLibrary(name, location);

            _libraries.Add(library);
            Select(library);

            return library;
        }

        public void Save(AxColorPresetLibrary library) => Saves++;
    }
}
