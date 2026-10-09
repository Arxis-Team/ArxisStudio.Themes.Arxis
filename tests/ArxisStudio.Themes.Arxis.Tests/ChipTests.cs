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
/// Снимаемый чип: крестик и клавиша просят снять его, а снимает хозяин.
/// </summary>
public class ChipTests
{
    [AvaloniaFact]
    public void A_plain_chip_has_no_cross()
    {
        var (chip, window) = Shown(removable: false);

        Assert.False(Cross(chip).IsVisible, "у обычного чипа есть крестик");

        window.Close();
    }

    [AvaloniaFact]
    public void The_cross_of_a_removable_chip_asks_to_remove_it()
    {
        var (chip, window) = Shown(removable: true);
        var asked = 0;

        chip.RemoveRequested += (_, _) => asked++;

        var cross = Cross(chip);

        Assert.True(cross.IsVisible, "у снимаемого чипа нет крестика");
        Assert.Equal("Remove", Avalonia.Automation.AutomationProperties.GetName(cross));

        var at = cross.TranslatePoint(new Point(cross.Bounds.Width / 2, cross.Bounds.Height / 2), window)!.Value;

        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);

        Assert.Equal(1, asked);

        window.Close();
    }

    /// <summary>С клавиатуры чип снимают Delete или Backspace, стоя на нём самом.</summary>
    [AvaloniaTheory]
    [InlineData(Key.Delete)]
    [InlineData(Key.Back)]
    public void A_key_on_a_focused_removable_chip_asks_to_remove_it(Key key)
    {
        var (chip, window) = Shown(removable: true);
        var asked = 0;

        chip.RemoveRequested += (_, _) => asked++;
        chip.Focusable = true;

        Assert.True(chip.Focus(NavigationMethod.Tab), "чип не взял каретку");

        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, string.Empty);

        Assert.Equal(1, asked);
        Assert.True(chip.Classes.Contains(":focus-visible"), "у чипа с кареткой нет кольца");

        window.Close();
    }

    private static (AxChip Chip, Window Window) Shown(bool removable)
    {
        var chip = new AxChip
        {
            Content = "primary",
            IsRemovable = removable,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
        };
        var window = new Window { Width = 300, Height = 100, Content = chip };

        window.Show();
        window.UpdateLayout();

        return (chip, window);
    }

    private static Button Cross(AxChip chip) =>
        chip.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_Remove");
}
