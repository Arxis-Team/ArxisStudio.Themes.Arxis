using ArxisStudio.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Поле жетонов: набор словами, снятие крестиком и с клавиатуры.
/// </summary>
/// <remarks>
/// Снятие с клавиатуры идёт в два нажатия: Backspace в пустой строке ставит каретку на последний жетон, и
/// только следующее снимает его. Лишнее нажатие не стирает то, что человек не собирался трогать.
/// </remarks>
public class TokenFieldTests
{
    [AvaloniaFact]
    public void Spaces_and_commas_turn_typed_words_into_tokens()
    {
        var (field, window) = Shown();
        var changes = Changes(field);

        Input(field).Focus();
        window.KeyTextInput("primary large,");

        Assert.Equal(["primary", "large"], field.Tokens);
        Assert.Equal(string.Empty, Input(field).Text);
        Assert.Equal(["primary", "large"], Assert.Single(changes).Added);

        window.Close();
    }

    [AvaloniaFact]
    public void Enter_adds_the_rest_and_a_repeat_is_skipped()
    {
        var (field, window) = Shown();

        Input(field).Focus();
        window.KeyTextInput("a b");
        Press(window, Key.Enter);
        window.KeyTextInput("a");
        Press(window, Key.Enter);

        Assert.Equal(["a", "b"], field.Tokens);
        Assert.Equal(string.Empty, Input(field).Text);

        window.Close();
    }

    /// <summary>Backspace в пустой строке ставит каретку на последний жетон, и только второе нажатие снимает его.</summary>
    [AvaloniaFact]
    public void Backspace_walks_to_the_last_token_before_it_removes_it()
    {
        var (field, window) = Shown(["a", "b"]);

        Input(field).Focus();
        Press(window, Key.Back);

        Assert.Equal(["a", "b"], field.Tokens);
        Assert.True(Chip(field, "b").IsFocused, "Backspace не поставил каретку на последний жетон");

        Press(window, Key.Back);

        Assert.Equal(["a"], field.Tokens);
        Assert.True(Chip(field, "a").IsFocused, "снятый жетон не отдал каретку соседу");

        window.Close();
    }

    [AvaloniaFact]
    public void Arrows_walk_the_tokens_and_come_back_to_the_input()
    {
        var (field, window) = Shown(["a", "b"]);

        Input(field).Focus();
        Press(window, Key.Left);
        Assert.True(Chip(field, "b").IsFocused, "← из строки не пришла на последний жетон");

        Press(window, Key.Left);
        Assert.True(Chip(field, "a").IsFocused, "← не пошла к предыдущему жетону");

        Press(window, Key.Right);
        Press(window, Key.Right);
        Assert.True(Input(field).IsFocused, "→ с последнего жетона не вернулась в строку");

        window.Close();
    }

    /// <summary>Крестик снимает свой жетон и каретку не берёт: человек дописывает дальше.</summary>
    [AvaloniaFact]
    public void The_cross_removes_its_token_and_leaves_the_caret_where_it_was()
    {
        var (field, window) = Shown(["a", "b"]);

        Input(field).Focus();

        var cross = Cross(Chip(field, "a"));

        Assert.True(cross.IsVisible, "у снимаемого жетона нет крестика");

        var at = cross.TranslatePoint(new Point(cross.Bounds.Width / 2, cross.Bounds.Height / 2), window)!.Value;

        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        window.UpdateLayout();

        Assert.Equal(["b"], field.Tokens);
        Assert.True(Input(field).IsFocused, "крестик увёл каретку из строки ввода");

        window.Close();
    }

    [AvaloniaFact]
    public void Leaving_the_field_keeps_what_was_typed()
    {
        var (field, window) = Shown();

        Input(field).Focus();
        window.KeyTextInput("accent");
        Beside(window).Focus();

        Assert.Equal(["accent"], field.Tokens);

        window.Close();
    }

    /// <summary>Подсказка видна, пока жетонов нет: рядом с ними она читалась бы ещё одним.</summary>
    [AvaloniaFact]
    public void The_placeholder_shows_only_while_there_are_no_tokens()
    {
        var (field, window) = Shown(setup: field => field.PlaceholderText = "Classes");

        Assert.Equal("Classes", Input(field).PlaceholderText);

        field.Tokens = ["a"];
        window.UpdateLayout();

        Assert.True(string.IsNullOrEmpty(Input(field).PlaceholderText), "подсказка осталась рядом с жетоном");

        window.Close();
    }

    [AvaloniaFact]
    public void Read_only_has_neither_input_nor_crosses()
    {
        var (field, window) = Shown(["a"], field => field.IsReadOnly = true);

        Assert.False(Input(field).IsVisible, "у поля только для чтения есть строка ввода");
        Assert.False(Cross(Chip(field, "a")).IsVisible, "у жетона поля только для чтения есть крестик");

        window.Close();
    }

    private static (AxTokenField Field, Window Window) Shown(
        IReadOnlyList<string>? tokens = null, Action<AxTokenField>? setup = null)
    {
        var field = new AxTokenField { Tokens = tokens ?? [], Width = 300 };

        setup?.Invoke(field);

        var window = new Window
        {
            Width = 400,
            Height = 200,
            Content = new StackPanel { Children = { field, new AxButton { Content = "beside" } } },
        };

        window.Show();
        window.UpdateLayout();

        return (field, window);
    }

    private static List<AxTokensChangedEventArgs> Changes(AxTokenField field)
    {
        var changes = new List<AxTokensChangedEventArgs>();

        field.TokensChanged += (_, e) => changes.Add(e);

        return changes;
    }

    private static TextBox Input(AxTokenField field) =>
        Assert.IsAssignableFrom<TextBox>(field.Input);

    private static AxChip Chip(AxTokenField field, string token) =>
        field.GetVisualDescendants().OfType<AxChip>().Single(chip => Equals(chip.Content, token));

    private static Button Cross(AxChip chip) =>
        chip.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_Remove");

    private static Control Beside(Window window) =>
        window.GetVisualDescendants().OfType<AxButton>().First(button => Equals(button.Content, "beside"));

    private static void Press(Window window, Key key)
    {
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, string.Empty);
        window.UpdateLayout();
    }
}
