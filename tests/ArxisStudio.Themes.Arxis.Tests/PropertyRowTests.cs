using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Строка свойства: колонка подписи по правилу Unity, перенос поля под подпись, отметка «задано здесь».
/// </summary>
/// <remarks>
/// Числа берутся из темы, а не переписываются: проверяется правило — 0,45 ширины без отступа, не уже
/// наименьшего, одна колонка на список, — а не то, сколько оно даёт сегодня.
/// </remarks>
public class PropertyRowTests
{
    /// <summary>Колонка подписи — 0,45 ширины строки без отступа, но не уже наименьшей.</summary>
    /// <remarks>Двести шестьдесят — ширина, у которой доля уже наименьшей: колонку держит нижняя граница.</remarks>
    [AvaloniaTheory]
    [InlineData(400)]
    [InlineData(600)]
    [InlineData(260)]
    public void The_label_column_follows_the_unity_rule(double width)
    {
        var (row, window) = Shown(width);
        var expected = Math.Max(Math.Round(width * AxPropertyRowPanel.LabelShare) - Token("AxPropertyLabelInset"), Token("AxPropertyLabelMinWidth"));

        Assert.False(Panel(row).IsStacked, "строка поставила поле под подпись, хотя ему хватает места");
        Assert.Equal(Token("AxPropertyMarkerWidth") + expected + Token("AxSpaceTight"), ValueLeft(row), 1);

        window.Close();
    }

    /// <summary>Колонка одна на список: строки с разной вложенностью ставят поля одной вертикалью.</summary>
    [AvaloniaFact]
    public void Rows_share_the_column_set_on_their_list_and_indent_narrows_only_the_label()
    {
        var top = new AxPropertyRow { Label = "Width", Content = new AxTextBox() };
        var nested = new AxPropertyRow { Label = "Left", Content = new AxTextBox(), Indent = 2 };
        var list = new StackPanel { Width = 420, Children = { top, nested } };

        AxPropertyRow.SetLabelWidth(list, 150);

        var window = Window(list);

        Assert.Equal(Token("AxPropertyMarkerWidth") + 150 + Token("AxSpaceTight"), ValueLeft(top), 1);
        Assert.Equal(ValueLeft(top), ValueLeft(nested), 1);
        Assert.Equal(2 * Token("AxTreeIndent"), LabelLeft(nested) - LabelLeft(top), 1);

        window.Close();
    }

    /// <summary>Полю тесно — оно уходит под подпись во всю ширину, как в узком окне настроек.</summary>
    [AvaloniaFact]
    public void A_cramped_field_goes_under_its_label()
    {
        var (row, window) = Shown(Token("AxPropertyLabelMinWidth") + Token("AxPropertyValueMinWidth") / 2);

        Assert.True(Panel(row).IsStacked, "тесное поле осталось рядом с подписью");
        Assert.True(Part<ContentPresenter>(row, "PART_ContentPresenter").Bounds.Top >= Part<AxScrubLabel>(row, "PART_Label").Bounds.Bottom - 0.5,
            "поле не ушло под подпись");

        row.Width = 600;
        window.UpdateLayout();

        Assert.False(Panel(row).IsStacked, "широкое поле осталось под подписью");

        window.Close();
    }

    /// <summary>
    /// Поле, которому мало своего наименьшего, уходит под подпись даже там, где обычному хватает: вектору
    /// уже двух ячеек в ряд — не место рядом с подписью.
    /// </summary>
    [AvaloniaFact]
    public void A_vector_too_narrow_for_two_cells_goes_under_its_label()
    {
        var floor = Token("AxVectorFieldMinWidth");
        var width = Token("AxPropertyMarkerWidth") + Token("AxPropertyLabelMinWidth") + Token("AxSpaceTight") + floor - 2;

        Assert.True(width - Token("AxPropertyLabelMinWidth") >= Token("AxPropertyValueMinWidth"), "условие теста: обычному полю места хватает");

        var (plain, first) = Shown(width);

        Assert.False(Panel(plain).IsStacked, "обычное поле ушло под подпись, хотя ему хватает места");
        first.Close();

        var (vector, second) = Shown(width, new AxVectorField { Labels = ["X", "Y"], Values = [0, 0] });

        Assert.True(Panel(vector).IsStacked, "вектор остался рядом с подписью, где ему не встать двумя ячейками");
        second.Close();
    }

    /// <summary>
    /// Обёртка не прячет наименьшую ширину поля: вектор в рамке — как в барьере, за которым студия ставит поле
    /// плагина, — уходит под подпись так же, как без неё.
    /// </summary>
    [AvaloniaFact]
    public void A_wrapped_vector_too_narrow_for_two_cells_still_goes_under_its_label()
    {
        var width = Token("AxPropertyMarkerWidth") + Token("AxPropertyLabelMinWidth") + Token("AxSpaceTight") + Token("AxVectorFieldMinWidth") - 2;
        var wrapped = new Border { Child = new Border { Child = new AxVectorField { Labels = ["X", "Y"], Values = [0, 0] } } };
        var (row, window) = Shown(width, wrapped);

        Assert.True(Panel(row).IsStacked, "вектор в обёртке остался рядом с подписью, где ему не встать двумя ячейками");
        window.Close();
    }

    /// <summary>Задано здесь — полоса акцента у края и полужирная подпись; иначе полосы нет.</summary>
    [AvaloniaFact]
    public void Overridden_shows_the_accent_bar_and_a_bold_label()
    {
        var (row, window) = Shown(400);
        var marker = Part<Border>(row, "PART_Marker");
        var label = Part<AxScrubLabel>(row, "PART_Label");

        Assert.Equal(Colors.Transparent, Color(marker.Background));
        Assert.Equal(FontWeight.Normal, label.FontWeight);

        row.IsOverridden = true;
        window.UpdateLayout();

        Assert.Equal(Color(Brush("AxAccentBrush")), Color(marker.Background));
        Assert.Equal(FontWeight.SemiBold, label.FontWeight);

        window.Close();
    }

    /// <summary>Полный вид — без подписи: поле от края полосы во всю ширину.</summary>
    [AvaloniaFact]
    public void Full_layout_leaves_no_label()
    {
        var (row, window) = Shown(400);

        row.Layout = AxPropertyRowLayout.Full;
        window.UpdateLayout();

        Assert.False(Part<AxScrubLabel>(row, "PART_Label").IsVisible, "у полного вида осталась подпись");
        Assert.Equal(Token("AxPropertyMarkerWidth"), ValueLeft(row), 1);

        window.Close();
    }

    /// <summary>Подпись строки тянет её число и фиксирует одно значение.</summary>
    [AvaloniaFact]
    public void The_label_scrubs_the_number_of_its_row()
    {
        var number = new AxNumberField { Value = 10 };
        var (row, window) = Shown(400, number);
        var committed = 0;

        row.ScrubTarget = number;
        number.ValueCommitted += (_, _) => committed++;
        window.UpdateLayout();

        var label = Part<AxScrubLabel>(row, "PART_Label");
        var at = label.TranslatePoint(new Point(10, label.Bounds.Height / 2), window)!.Value;

        window.MouseDown(at, MouseButton.Left);
        window.MouseMove(at + new Vector(5, 0));
        window.MouseMove(at + new Vector(25, 0));
        window.MouseUp(at + new Vector(25, 0), MouseButton.Left);

        Assert.True(number.Value > 10, $"протяжка за подпись не сдвинула число: {number.Value}");
        Assert.Equal(1, committed);

        window.Close();
    }

    /// <summary>Заметка строки с ошибкой — цветом ошибки; почему значение не годится, читают там же, где его набрали.</summary>
    [AvaloniaFact]
    public void An_error_note_takes_the_error_color()
    {
        var (row, window) = Shown(400);

        row.Note = "Не число";
        window.UpdateLayout();

        var note = Part<ContentPresenter>(row, "PART_Note");

        Assert.True(note.IsVisible, "заметки не видно");
        Assert.Equal(Color(Brush("AxTextSecondaryBrush")), Color(note.Foreground));

        AxValidation.SetState(row, AxValidationState.Error);
        window.UpdateLayout();

        Assert.Equal(Color(Brush("AxErrorTextBrush")), Color(note.Foreground));
        Assert.True(note.Bounds.Top >= Part<ContentPresenter>(row, "PART_ContentPresenter").Bounds.Bottom - 0.5, "заметка не под полем");

        window.Close();
    }

    private static (AxPropertyRow Row, Window Window) Shown(double width, Control? content = null)
    {
        var row = new AxPropertyRow { Label = "Width", Content = content ?? new AxTextBox(), Width = width };

        return (row, Window(row));
    }

    private static Window Window(Control content)
    {
        var window = new Window
        {
            Width = 800,
            Height = 300,
            Content = new StackPanel { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, Children = { content } },
        };

        window.Show();
        window.UpdateLayout();

        return window;
    }

    private static AxPropertyRowPanel Panel(AxPropertyRow row) => Part<AxPropertyRowPanel>(row, "PART_Panel");

    private static double ValueLeft(AxPropertyRow row) =>
        Part<ContentPresenter>(row, "PART_ContentPresenter").TranslatePoint(default, row)!.Value.X;

    private static double LabelLeft(AxPropertyRow row) =>
        Part<AxScrubLabel>(row, "PART_Label").TranslatePoint(default, row)!.Value.X;

    private static T Part<T>(AxPropertyRow row, string name)
        where T : Control =>
        row.GetVisualDescendants().OfType<T>().Single(part => part.Name == name && ReferenceEquals(part.TemplatedParent, row));

    private static double Token(string key)
    {
        Assert.True(Application.Current!.TryFindResource(key, null, out var value), $"в теме нет {key}");

        return Assert.IsType<double>(value);
    }

    private static IBrush Brush(string key)
    {
        Assert.True(Application.Current!.TryFindResource(key, Application.Current!.ActualThemeVariant, out var value), $"в теме нет {key}");

        return Assert.IsAssignableFrom<IBrush>(value);
    }

    private static Color Color(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
}
