using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Таблица держится на линейках.
/// </summary>
/// <remarks>
/// Ни рамки вокруг, ни заливки под шапкой нет: таблицу собирают
/// отбивки снизу — под шапкой и под каждой строкой. Отбивки нет у последней
/// строки, где линия висела бы над пустотой, и у выбранной, где она разрезала
/// бы полосу выделения.
///
/// Строка идёт на AxRowHeight, поэтому вертикальной отбивки у ячейки в теме
/// нет — её съедает высота строки.
/// Горизонтальные 10 стоят на ячейке, а не на строке: у строки отступ достался
/// бы только первой колонке.
/// </remarks>
public class TableTests
{
    /// <summary>Вокруг таблицы ни рамки, ни заливки под шапкой.</summary>
    [AvaloniaFact]
    public void Table_is_held_by_rules_alone()
    {
        var (table, window) = Shown();

        Assert.Equal(new Thickness(0), table.BorderThickness);
        Assert.Null(Colour(Header(table).Background));

        window.Close();
    }

    /// <summary>Шапка: отбивка снизу, AxTextSecondary, мелкий кегль, строка AxRowHeight.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Header_is_a_caption_over_a_rule(string variant)
    {
        var (table, window) = Shown(variant);

        var header = Header(table);

        // Линию под шапкой ведёт разделитель: пиксель устройства он меряет сам, а нижняя рамка
        // шапки при 150 % давала бы два.
        var rule = table.GetVisualDescendants().OfType<AxDivider>().First();

        Assert.Equal(Resource(window, "AxStrokeSubtleColor", variant), Colour(rule.Fill));
        Assert.Equal(1d, rule.Bounds.Height);
        Assert.Equal(24d, header.Bounds.Height);

        var caption = header.GetVisualDescendants().OfType<TextBlock>().First();

        Assert.Equal(Resource(window, "AxTextSecondaryColor", variant), Colour(caption.Foreground));
        Assert.Equal(12d, caption.FontSize);

        window.Close();
    }

    /// <summary>Каждая строка отбита снизу — кроме последней и выбранной.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Every_row_but_the_last_and_the_selected_one_carries_a_rule(string variant)
    {
        var (table, window) = Shown(variant);

        var rows = Rows(table).ToList();

        // Выделение показано в полную силу только у таблицы под фокусом.
        rows[1].Focus(NavigationMethod.Tab);
        window.UpdateLayout();

        Assert.Equal(new Thickness(0, 0, 0, 1), Fill(rows[0]).BorderThickness);
        Assert.Equal(Resource(window, "AxStrokeSubtleColor", variant), Colour(Fill(rows[0]).BorderBrush));

        Assert.Equal(new Thickness(0), Fill(rows[1]).BorderThickness);
        Assert.Equal(Resource(window, "AxSelectionActiveColor", variant), Colour(Fill(rows[1]).Background));

        Assert.Equal(new Thickness(0, 0, 0, 1), Fill(rows[2]).BorderThickness);
        Assert.Equal(new Thickness(0), Fill(rows[^1]).BorderThickness);

        window.Close();
    }

    /// <summary>Строка таблицы не скруглена: полоса идёт прямой.</summary>
    [AvaloniaFact]
    public void Row_keeps_square_corners()
    {
        var (table, window) = Shown();

        foreach (var row in Rows(table))
            Assert.Equal(new CornerRadius(0), Fill(row).CornerRadius);

        window.Close();
    }

    /// <summary>Ячейка отбита на 10 по горизонтали, и отбита сама, а не строка.</summary>
    [AvaloniaFact]
    public void Cell_carries_the_padding_not_the_row()
    {
        var (table, window) = Shown();

        foreach (var row in Rows(table))
            Assert.Equal(new Thickness(0), row.Padding);

        foreach (var cell in table.GetVisualDescendants().OfType<TextBlock>())
            Assert.Equal(new Thickness(10, 0), cell.Padding);

        window.Close();
    }

    private static (AxDataGrid Table, Window Window) Shown(string variant = "Dark")
    {
        var table = new AxDataGrid { Header = Row("Форма", "Контролов") };

        foreach (var name in new[] { "MainWindow.axaml", "ChatView.axaml", "LoginView.axaml", "SettingsView.axaml" })
            table.Items.Add(new AxListBoxItem { Content = Row(name, "24") });

        // После наполнения: пустому списку выбирать нечего.
        table.SelectedIndex = 1;

        var window = new Window
        {
            Width = 520,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark,
            Content = table,
        };

        window.Show();
        window.UpdateLayout();

        return (table, window);
    }

    private static Grid Row(params string[] cells)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,*") };

        for (var i = 0; i < cells.Length; i++)
        {
            var text = new TextBlock { Text = cells[i] };
            Grid.SetColumn(text, i);
            grid.Children.Add(text);
        }

        return grid;
    }

    /// <summary>Шапка таблицы: часть с именем, а не «та рамка, у которой есть низ».</summary>
    private static Border Header(AxDataGrid table) =>
        table.GetVisualDescendants().OfType<Border>().Single(part => part.Name == "PART_Header");

    private static IEnumerable<AxListBoxItem> Rows(AxDataGrid table) =>
        table.GetVisualDescendants().OfType<AxListBoxItem>();

    /// <summary>Заливка строки: она же несёт отбивку и радиус.</summary>
    private static ContentPresenter Fill(AxListBoxItem row) =>
        row.GetVisualDescendants().OfType<ContentPresenter>().First(c => c.Name == "PART_ContentPresenter");

    private static Color? Colour(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static Color Resource(Window window, string key, string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        Assert.True(window.TryFindResource(key, theme, out var value), key);

        return (Color)value!;
    }
}
