using ArxisStudio.Controls;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Список, в который можно набрать своё (<c>IsEditable</c>): поле ввода на месте выбранного, текст сводится с выбором, а
/// каретка в поле зажигает рамку и кольцо списка.
/// </summary>
public class EditableComboBoxTests
{
    /// <summary>
    /// У списка с вводом на месте выбранного — поле ввода: выбранное встаёт в него текстом, набранное становится текстом
    /// списка. Tab в список ставит каретку в поле, каретка в поле зажигает рамку и кольцо списка, а уйдя — гасит.
    /// </summary>
    [AvaloniaFact]
    public void An_editable_combo_box_shows_a_box_in_place_of_the_selected_item()
    {
        var (combo, window) = Shown(editable: true);
        var box = Part<AxTextBox>(combo, "PART_EditableTextBox");
        var ring = Part<Control>(combo, "PART_FocusRing");

        Assert.True(box.IsVisible, "поля ввода у списка с вводом нет");
        Assert.False(Part<ContentControl>(combo, "PART_SelectionBox").IsVisible, "выбранное стоит рядом с полем ввода");

        combo.SelectedItem = "Consolas";
        Settle(window);

        Assert.Equal("Consolas", box.Text);

        combo.Focus(NavigationMethod.Tab);
        Settle(window);

        Assert.Same(box, window.FocusManager!.GetFocusedElement());
        Assert.True(ring.IsVisible, "каретка в поле не зажгла кольцо списка");
        Assert.True(combo.Classes.Contains(":editing"), "список не знает, что в его поле пишут");

        box.SelectAll();
        window.KeyTextInput("Arial Black");
        Settle(window);

        Assert.Equal("Arial Black", combo.Text);

        Next(window).Focus();
        Settle(window);

        Assert.False(ring.IsVisible, "кольцо списка горит, когда каретка ушла");

        window.Close();
    }

    /// <summary>Обычный список поля ввода не показывает: выбранное стоит, как стояло.</summary>
    [AvaloniaFact]
    public void A_plain_combo_box_keeps_its_selected_item()
    {
        var (combo, window) = Shown(editable: false);

        combo.SelectedItem = "Consolas";
        Settle(window);

        Assert.False(Part<AxTextBox>(combo, "PART_EditableTextBox").IsVisible, "у обычного списка поле ввода");
        Assert.True(Part<ContentControl>(combo, "PART_SelectionBox").IsVisible, "у обычного списка нет выбранного");

        window.Close();
    }

    private static (AxComboBox Combo, Window Window) Shown(bool editable)
    {
        var combo = new AxComboBox
        {
            IsEditable = editable,
            ItemsSource = new[] { "Arial", "Consolas", "Segoe UI" },
            Width = 200,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
        };
        var window = new Window
        {
            Width = 300,
            Height = 200,
            Content = new StackPanel { Children = { combo, new AxButton { Content = "OK" } } },
        };

        window.Show();
        Settle(window);

        return (combo, window);
    }

    private static AxButton Next(Window window) =>
        window.GetVisualDescendants().OfType<AxButton>().Single(button => Equals(button.Content, "OK"));

    private static T Part<T>(Control owner, string name)
        where T : Control =>
        owner.GetVisualDescendants().OfType<T>().First(part => part.Name == name);

    private static void Settle(Window window)
    {
        window.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
