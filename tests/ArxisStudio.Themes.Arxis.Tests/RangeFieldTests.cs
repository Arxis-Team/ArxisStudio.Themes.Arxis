using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Число в границах — ползунок и поле рядом, как Range у Unity, — и сеанс протяжки ползунка: на ходу видно, фиксируется
/// одно значение, Esc возвращает начало.
/// </summary>
public class RangeFieldTests
{
    /// <summary>Протяжка ползунка видна в поле на каждом шаге, а фиксируется одним значением при отпускании.</summary>
    [AvaloniaFact]
    public void A_drag_on_the_slider_shows_each_step_in_the_field_and_commits_once()
    {
        var (field, window) = Shown();
        var (changing, committed, cancelled) = Listen(field);
        var slider = field.Slider;
        var number = field.NumberField;

        window.MouseDown(OnTrack(slider, window, 0.2), MouseButton.Left);
        Settle(window);
        window.MouseMove(OnTrack(slider, window, 0.5));
        Settle(window);
        window.MouseMove(OnTrack(slider, window, 0.7));
        Settle(window);

        Assert.True(changing.Count >= 2, $"протяжка показала {changing.Count} шагов");
        Assert.Empty(committed);
        Assert.Equal(0.7, field.Value, 2);
        Assert.Equal("0.7", number.Text);

        // Ручку, которую тянут, видно взятой, где бы ни был указатель: он ушёл за конец дорожки, а ручка — нет.
        var thumb = slider.GetVisualDescendants().OfType<Thumb>().Single();

        window.MouseMove(OnTrack(slider, window, 1.15));
        Settle(window);

        Assert.Equal(1, field.Value, 6);
        Assert.False(thumb.IsPointerOver, "указатель остался над ручкой");
        Assert.True(slider.TryFindResource("AxAccentHoverBrush", slider.ActualThemeVariant, out var taken), "ключа наведения акцента нет");
        Assert.Same(taken, thumb.BorderBrush);

        window.MouseMove(OnTrack(slider, window, 0.7));
        Settle(window);
        window.MouseUp(OnTrack(slider, window, 0.7), MouseButton.Left);
        Settle(window);

        Assert.Equal(0.7, Assert.Single(committed), 2);
        Assert.Empty(cancelled);
        Assert.False(slider.IsScrubbing, "протяжка не кончилась отпусканием");

        window.Close();
    }

    /// <summary>Нажатие по дорожке без тяги — тоже значение: оно встаёт под указатель и фиксируется при отпускании.</summary>
    [AvaloniaFact]
    public void A_press_on_the_track_takes_the_value_under_the_pointer()
    {
        var (field, window) = Shown();
        var (_, committed, _) = Listen(field);
        var at = OnTrack(field.Slider, window, 0.4);

        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        Settle(window);

        Assert.Equal(0.4, Assert.Single(committed), 2);
        Assert.Equal(0.4, field.Value, 2);

        window.Close();
    }

    /// <summary>Ползунок идёт делениями шага: протянутое между ними встаёт на ближайшее.</summary>
    [AvaloniaFact]
    public void The_slider_walks_the_steps()
    {
        var (field, window) = Shown(field => field.Step = 0.25);
        var (_, committed, _) = Listen(field);
        var slider = field.Slider;

        window.MouseDown(OnTrack(slider, window, 0.1), MouseButton.Left);
        Settle(window);
        window.MouseMove(OnTrack(slider, window, 0.33));
        Settle(window);
        window.MouseUp(OnTrack(slider, window, 0.33), MouseButton.Left);
        Settle(window);

        Assert.Equal(0.25, Assert.Single(committed), 6);

        window.Close();
    }

    /// <summary>
    /// Esc посреди протяжки возвращает значение начала — и разные значения у выбранных, — а до отпускания ползунок за
    /// указателем больше не идёт и ничего не фиксирует.
    /// </summary>
    [AvaloniaFact]
    public void Escape_in_the_middle_of_a_drag_brings_the_start_back()
    {
        var (field, window) = Shown(field =>
        {
            field.Value = 0.3;
            field.IsMixed = true;
        });
        var (changing, committed, cancelled) = Listen(field);
        var slider = field.Slider;
        var number = field.NumberField;

        window.MouseDown(OnTrack(slider, window, 0.6), MouseButton.Left);
        Settle(window);
        window.MouseMove(OnTrack(slider, window, 0.8));
        Settle(window);

        Assert.False(field.IsMixed, "протянутое осталось разным");
        Assert.NotEmpty(changing);

        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, string.Empty);
        Settle(window);

        Assert.Single(cancelled);
        Assert.Equal(0.3, field.Value, 6);
        Assert.Equal(0.3, slider.Value, 6);
        Assert.True(field.IsMixed, "отмена не вернула разные значения");
        Assert.True(number.IsMixed, "поле после отмены не показывает прочерк");

        var shown = changing.Count;

        window.MouseMove(OnTrack(slider, window, 0.1));
        Settle(window);
        window.MouseUp(OnTrack(slider, window, 0.1), MouseButton.Left);
        Settle(window);

        Assert.Equal(0.3, slider.Value, 6);
        Assert.Equal(shown, changing.Count);
        Assert.Empty(committed);

        window.Close();
    }

    /// <summary>Шаг клавишей фиксируется каждый: стрелка — на шаг, PageUp — на десять, End — к верхней границе.</summary>
    [AvaloniaFact]
    public void Keys_step_the_slider_and_each_step_is_committed()
    {
        var (field, window) = Shown(field => field.Value = 0.5);
        var (changing, committed, _) = Listen(field);
        var slider = field.Slider;

        Assert.True(slider.Focus(NavigationMethod.Tab), "ползунок не взял клавиатуру");

        Press(window, Key.Right);
        Press(window, Key.PageUp);
        Press(window, Key.End);

        Assert.Equal(3, committed.Count);
        Assert.Equal(0.51, committed[0], 6);
        Assert.Equal(0.61, committed[1], 6);
        Assert.Equal(1, committed[2], 6);
        Assert.Empty(changing);
        Assert.Equal("1", field.NumberField.Text);

        window.Close();
    }

    /// <summary>Набранное в поле фиксируется одним значением, и ползунок встаёт к нему.</summary>
    [AvaloniaFact]
    public void A_typed_value_is_committed_once_and_moves_the_slider()
    {
        var (field, window) = Shown();
        var (_, committed, _) = Listen(field);
        var number = field.NumberField;

        number.Focus();
        number.SelectAll();
        window.KeyTextInput("0.25");
        Press(window, Key.Enter);

        Assert.Equal(0.25, Assert.Single(committed), 6);
        Assert.Equal(0.25, field.Value, 6);
        Assert.Equal(0.25, field.Slider.Value, 6);

        window.Close();
    }

    /// <summary>Значение, поставленное хозяином, событий не поднимает, а части его показывают.</summary>
    [AvaloniaFact]
    public void A_value_set_by_the_host_raises_nothing()
    {
        var (field, window) = Shown();
        var (changing, committed, cancelled) = Listen(field);

        field.Value = 0.6;
        Settle(window);

        Assert.Empty(changing);
        Assert.Empty(committed);
        Assert.Empty(cancelled);
        Assert.Equal(0.6, field.Slider.Value, 6);
        Assert.Equal("0.6", field.NumberField.Text);

        window.Close();
    }

    /// <summary>Поле числа — шириной своего ключа справа от ползунка, а всё поле не уже своей наименьшей ширины.</summary>
    [AvaloniaFact]
    public void The_number_stands_right_of_the_slider_at_its_width()
    {
        var (field, window) = Shown();
        var number = field.NumberField;
        var slider = field.Slider;

        Assert.Equal(Metric(field, "AxRangeFieldNumberWidth"), number.Bounds.Width);
        Assert.Equal(Metric(field, "AxRangeFieldMinWidth"), field.MinWidth);
        Assert.True(
            slider.TranslatePoint(new Point(slider.Bounds.Width, 0), field)!.Value.X <= number.TranslatePoint(default, field)!.Value.X,
            "поле числа не справа от ползунка");

        window.Close();
    }

    private static (AxRangeField Field, Window Window) Shown(Action<AxRangeField>? setup = null)
    {
        var field = new AxRangeField
        {
            Minimum = 0,
            Maximum = 1,
            Step = 0.01,
            Decimals = 2,
            Width = 300,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
        };

        setup?.Invoke(field);

        var window = new Window { Width = 400, Height = 120, Content = field };

        window.Show();
        Settle(window);

        return (field, window);
    }

    private static (List<double> Changing, List<double> Committed, List<RoutedEventArgs> Cancelled) Listen(AxRangeField field)
    {
        var changing = new List<double>();
        var committed = new List<double>();
        var cancelled = new List<RoutedEventArgs>();

        field.ValueChanging += (_, e) => changing.Add(e.Value);
        field.ValueCommitted += (_, e) => committed.Add(e.Value);
        field.ScrubCancelled += (_, e) => cancelled.Add(e);

        return (changing, committed, cancelled);
    }

    /// <summary>Точка дорожки, которая значит <paramref name="value"/> между нулём и единицей, — в координатах окна.</summary>
    private static Point OnTrack(AxSlider slider, Window window, double value)
    {
        var track = slider.GetVisualDescendants().OfType<Track>().Single();
        var thumb = Assert.IsAssignableFrom<Thumb>(track.Thumb);
        var x = thumb.Bounds.Width / 2 + value * (track.Bounds.Width - thumb.Bounds.Width);

        return track.TranslatePoint(new Point(x, track.Bounds.Height / 2), window)!.Value;
    }

    private static double Metric(Control control, string key) =>
        control.TryFindResource(key, out var value) && value is double metric ? metric : double.NaN;

    private static void Press(Window window, Key key)
    {
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, string.Empty);
        window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, string.Empty);
        Settle(window);
    }

    /// <summary>Раскладка и такт отрисовки: попадание указателя безголовая платформа считает по отрисованной сцене.</summary>
    private static void Settle(Window window)
    {
        window.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
