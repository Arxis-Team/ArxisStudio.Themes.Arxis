using System.Text.RegularExpressions;
using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Тень попапа и диалога помещается в своё поле, а поле — у каждой поверхности своей тени.
/// </summary>
/// <remarks>
/// Окно попапа и окно диалога обрезают всё, что за их краем. Тень попапа с размытием 16 стояла в
/// поле 8 у меню и 6 у подсказки, тень диалога с размытием 40 — в поле 24: хвост тени кончался
/// прямой ступенькой, и в светлой теме вокруг меню была видна серая рамка. Палитра команд стояла
/// без тени вовсе: её срезал <c>ClipToBounds</c> на той же рамке, что тень несла.
/// </remarks>
public class ShadowRoomTests
{
    /// <summary>Поле вмещает тень целиком: размытие, растяжку и сдвиг по каждой оси.</summary>
    /// <remarks>
    /// Поле симметрично: меню у края экрана переворачивается, и поправка места на поле обязана
    /// быть верной в обе стороны.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("AxShadowPopup", "AxPopupShadowRoom", "Dark")]
    [InlineData("AxShadowPopup", "AxPopupShadowRoom", "Light")]
    [InlineData("AxShadowModal", "AxModalShadowRoom", "Dark")]
    [InlineData("AxShadowModal", "AxModalShadowRoom", "Light")]
    public void A_room_holds_its_whole_shadow(string shadowKey, string roomKey, string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        Assert.True(Application.Current!.TryFindResource(shadowKey, theme, out var shadows), $"в теме нет {shadowKey}");
        Assert.True(Application.Current!.TryFindResource(roomKey, theme, out var found), $"в теме нет {roomKey}");

        var room = (Thickness)found!;

        Assert.Equal(room.Left, room.Right);
        Assert.Equal(room.Top, room.Bottom);

        foreach (var shadow in (BoxShadows)shadows!)
        {
            var across = shadow.Blur + shadow.Spread + Math.Abs(shadow.OffsetX);
            var down = shadow.Blur + shadow.Spread + Math.Abs(shadow.OffsetY);

            Assert.True(room.Left >= across, $"{roomKey}: по горизонтали {room.Left}, а тень {shadowKey} уходит на {across}");
            Assert.True(room.Top >= down, $"{roomKey}: по вертикали {room.Top}, а тень {shadowKey} уходит на {down}");
        }
    }

    /// <summary>Меню, подсказка и диалог стоят каждый в поле своей тени.</summary>
    [AvaloniaFact]
    public void Popups_and_dialogs_stand_in_the_room_of_their_shadow()
    {
        var menu = new MenuFlyoutPresenter { ItemsSource = new[] { new MenuItem { Header = "Открыть" } } };
        var tip = new ToolTip { Content = "Подсказка" };
        var window = new Window { Content = new StackPanel { Children = { menu, tip } } };

        window.Show();
        window.UpdateLayout();

        Assert.Equal(Room("AxPopupShadowRoom"), Card(menu).Margin);
        Assert.Equal(Room("AxPopupShadowRoom"), Card(tip).Margin);

        var dialog = new AxDialog { Title = "Вопрос", Content = new TextBlock { Text = "Так?" } };

        dialog.Show(window);
        dialog.UpdateLayout();

        Assert.Equal(Room("AxModalShadowRoom"), Card(dialog).Margin);

        dialog.Close();
        window.Close();
    }

    /// <summary>
    /// Первый пункт вложенного меню встаёт вровень с пунктом, который его открыл.
    /// </summary>
    /// <remarks>
    /// Сдвиг вложенного меню выведен из поля под тень, отбивки меню и рамки: поменяй одно из них
    /// порознь — и вложенное меню снова поедет вниз, а тест скажет, на сколько.
    /// </remarks>
    [AvaloniaFact]
    public void The_submenu_offsets_follow_the_room()
    {
        var room = Room("AxPopupShadowRoom");
        var padding = (Thickness)Resource("AxMenuPadding");

        Assert.Equal(4 - room.Left, (double)Resource("AxSubmenuHorizontalOffset"));
        Assert.Equal(-(room.Top + padding.Top + 1), (double)Resource("AxSubmenuVerticalOffset"));
    }

    /// <summary>
    /// Рамка, несущая тень, себя не обрезает.
    /// </summary>
    /// <remarks>
    /// <c>ClipToBounds</c> обрезает и собственную тень рамки: палитра команд с ним стояла без тени.
    /// Обрезать содержимое по скруглению можно внутренней рамкой.
    /// </remarks>
    [Fact]
    public void A_shadowed_border_does_not_clip_itself()
    {
        var elements = new Regex("""<[A-Za-z:.]+\s[^<>]*>""", RegexOptions.Compiled);

        var clipping = ThemeSources.All()
            .SelectMany(source => elements.Matches(source.Text)
                .Where(tag => tag.Value.Contains("BoxShadow=", StringComparison.Ordinal) &&
                              tag.Value.Contains("ClipToBounds=\"True\"", StringComparison.Ordinal))
                .Select(_ => source.Name))
            .ToList();

        Assert.True(clipping.Count == 0, "рамка с тенью обрезает себя в " + string.Join(", ", clipping));
    }

    /// <summary>
    /// Поверхность, которую ставит приложение, не обрезает свою тень собственными границами.
    /// </summary>
    /// <remarks>
    /// Контрол с шаблоном обрезает всё по своим границам, а поле вокруг карточки у палитры в 10, у
    /// подсказки-обучения и карточки уведомления в 6 — тени диалога и попапа туда не помещаются.
    /// Живёт такая поверхность в слое окна или в разметке, где тени есть куда лечь.
    /// </remarks>
    [AvaloniaFact]
    public void A_surface_placed_by_the_application_lets_its_shadow_out()
    {
        Control[] surfaces =
        [
            new AxQuickSearch { PlaceholderText = "Команда" },
            new AxTeachingTip { Title = "Подсказка", Content = "Текст" },
            new AxNotificationCard { Title = "Готово", Content = "Текст" },
        ];

        var window = new Window { Content = new StackPanel { Children = { surfaces[0], surfaces[1], surfaces[2] } } };

        window.Show();
        window.UpdateLayout();

        foreach (var surface in surfaces)
        {
            Assert.True(Card(surface).BoxShadow.Count > 0, $"{surface.GetType().Name}: тени нет");
            Assert.False(surface.ClipToBounds, $"{surface.GetType().Name} обрезает свою тень границами");
        }

        window.Close();
    }

    private static Border Card(Control control) =>
        control.GetVisualDescendants().OfType<Border>().First(border => border.BoxShadow.Count > 0);

    private static Thickness Room(string key) => (Thickness)Resource(key);

    private static object Resource(string key)
    {
        Assert.True(Application.Current!.TryFindResource(key, out var value), $"в теме нет {key}");

        return value!;
    }
}
