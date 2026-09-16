using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Шапка таблицы едет вбок вместе с колонками и не уезжает вверх со строками.
/// </summary>
/// <remarks>
/// Колонки шапки и колонки строк — одна сетка, и держится она на том, что стоят они друг под
/// другом. Стоило таблице поехать вбок — а колонок в ней бывает больше, чем места, — и шапка
/// оставалась на месте: подписи переставали называть то, что под ними. Внутрь области прокрутки
/// её не убрать: там она ушла бы вверх на первом же обороте колеса.
/// </remarks>
public class DataGridHeaderTests
{
    /// <summary>Таблица едет вбок, и шапка едет ровно настолько же.</summary>
    [AvaloniaFact]
    public void The_header_follows_the_columns_sideways()
    {
        var (grid, window) = Shown();

        var scroll = grid.GetVisualDescendants().OfType<ScrollViewer>().Single(part => part.Name == "PART_ScrollViewer");
        var header = grid.GetVisualDescendants().OfType<ContentPresenter>()
            .First(part => part.FindAncestorOfType<Border>()?.Name == "PART_Header");

        Assert.True(scroll.Extent.Width > scroll.Viewport.Width, "таблице некуда ехать вбок");
        Assert.Equal(0d, header.RenderTransform?.Value.M31 ?? 0d);

        scroll.Offset = new Vector(40, 0);
        window.UpdateLayout();

        Assert.Equal(-40d, header.RenderTransform!.Value.M31);

        window.Close();
    }

    /// <summary>Шапка стоит над прокруткой, а не внутри неё: вверх она не уезжает.</summary>
    [AvaloniaFact]
    public void The_header_stays_put_when_the_rows_scroll_away()
    {
        var (grid, window) = Shown();

        var scroll = grid.GetVisualDescendants().OfType<ScrollViewer>().Single(part => part.Name == "PART_ScrollViewer");
        var header = grid.GetVisualDescendants().OfType<Border>().Single(part => part.Name == "PART_Header");
        var top = header.TranslatePoint(default, grid)!.Value.Y;

        scroll.Offset = new Vector(0, scroll.Extent.Height - scroll.Viewport.Height);
        window.UpdateLayout();

        Assert.Equal(top, header.TranslatePoint(default, grid)!.Value.Y);

        window.Close();
    }

    /// <summary>Таблица со строкой шире места умеет ехать вбок.</summary>
    [AvaloniaFact]
    public void A_table_wider_than_its_place_scrolls_sideways()
    {
        var (grid, window) = Shown();

        var scroll = grid.GetVisualDescendants().OfType<ScrollViewer>().Single(part => part.Name == "PART_ScrollViewer");

        Assert.Equal(ScrollBarVisibility.Auto, scroll.HorizontalScrollBarVisibility);
        Assert.True(scroll.Extent.Width > scroll.Viewport.Width, "таблица сжала колонки вместо прокрутки");

        window.Close();
    }

    /// <summary>Таблица с колонками шире окна и с шапкой над ними.</summary>
    private static (AxDataGrid Grid, Window Window) Shown()
    {
        var grid = new AxDataGrid
        {
            Width = 200,
            Height = 120,
            Header = Row("Файл", "Размер", "Изменён"),
            ItemsSource = new[]
            {
                Row("Program.cs", "2 КБ", "вчера"),
                Row("App.axaml", "4 КБ", "сегодня"),
                Row("MainWindow.axaml", "12 КБ", "сегодня"),
                Row("StudioDock.cs", "30 КБ", "неделю назад"),
            },
        };

        var window = new Window
        {
            Width = 400,
            Height = 300,
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = grid,
        };

        window.Show();
        window.UpdateLayout();

        return (grid, window);
    }

    /// <summary>Строка таблицы: три колонки по сто точек — вместе они шире места.</summary>
    private static Control Row(params string[] cells)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };

        foreach (var cell in cells)
            row.Children.Add(new TextBlock { Text = cell, Width = 100 });

        return row;
    }
}
