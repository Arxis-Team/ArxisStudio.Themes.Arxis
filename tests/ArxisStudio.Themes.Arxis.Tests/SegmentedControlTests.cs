using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Сегментный переключатель в прокрутке: выбор, сделанный кодом, прокрутку вокруг не двигает, а сегмент под
/// кареткой она показывает.
/// </summary>
/// <remarks>
/// Своей прокрутки у ряда сегментов нет, и просьба показать выбранный сегмент уходила к прокрутке вокруг:
/// инспектор студии, где переключатель стоял у нижнего края, уезжал вверх от каждого показанного значения.
/// </remarks>
public class SegmentedControlTests
{
    /// <summary>Высота прокрутки; переключатель стоит так, что нижний край режет его посередине.</summary>
    private const double ViewportHeight = 100;

    /// <summary>Выбор сегмента кодом не прокручивает список, у края которого стоит переключатель.</summary>
    [AvaloniaFact]
    public void Choosing_a_segment_in_code_leaves_the_scroll_around_it_alone()
    {
        var (segments, viewer, window) = Shown();

        segments.SelectedIndex = 2;
        Settle(window);

        Assert.Equal(2, segments.SelectedIndex);
        Assert.True(viewer.Offset.Y == 0, $"выбор сегмента прокрутил список на {viewer.Offset.Y}");

        window.Close();
    }

    /// <summary>Сегмент, на который перешла каретка, прокрутка показывает: его видно целиком.</summary>
    [AvaloniaFact]
    public void A_focused_segment_is_brought_into_view()
    {
        var (segments, viewer, window) = Shown();
        var item = Assert.IsType<AxSegmentItem>(segments.ContainerFromIndex(1));

        item.Focus(NavigationMethod.Directional);
        Settle(window);

        var bottom = item.TranslatePoint(new Point(0, item.Bounds.Height), viewer)!.Value.Y;

        Assert.True(viewer.Offset.Y > 0, "сегмент под кареткой остался срезанным краем");
        Assert.True(bottom <= ViewportHeight + 0.5, $"низ сегмента под кареткой — на {bottom:0.##}, а прокрутка кончается на {ViewportHeight}");

        window.Close();
    }

    private static (AxSegmentedControl Segments, ScrollViewer Viewer, Window Window) Shown()
    {
        var segments = new AxSegmentedControl
        {
            Items =
            {
                new AxSegmentItem { Content = "NoWrap" },
                new AxSegmentItem { Content = "Wrap" },
                new AxSegmentItem { Content = "WrapWithOverflow" },
            },
            SelectedIndex = 0,
        };
        var above = new Border();
        var viewer = new ScrollViewer
        {
            Height = ViewportHeight,
            Content = new StackPanel { Children = { above, segments, new Border { Height = 400 } } },
        };
        var window = new Window { Width = 400, Height = 300, Content = viewer };

        window.Show();
        window.UpdateLayout();

        // Нижний край режет переключатель посередине.
        above.Height = ViewportHeight - segments.Bounds.Height / 2;
        Settle(window);

        var bottom = segments.TranslatePoint(new Point(0, segments.Bounds.Height), viewer)!.Value.Y;

        Assert.True(bottom > ViewportHeight + 1, "условие теста: переключатель не срезан нижним краем");
        Assert.Equal(0, viewer.Offset.Y);

        return (segments, viewer, window);
    }

    private static void Settle(Window window)
    {
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}
