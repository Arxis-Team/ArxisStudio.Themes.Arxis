using ArxisStudio.Controls;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace ArxisStudio.Themes.Arxis.Tests;

/// <summary>
/// Экранный диктор читает контрол его ролью, а не ролью того, на чём контрол построен.
/// </summary>
/// <remarks>
/// Роль и шаблоны спрашиваются у пира — у того, кого спрашивает мост платформы, — а не у вида:
/// выглядит вкладка вкладкой и без этого.
/// </remarks>
public class AutomationRoleTests
{
    /// <summary>
    /// Полоса вкладок — набор вкладок, вкладка — вкладка с именем, и выбирают её, как строку списка.
    /// </summary>
    /// <remarks>
    /// Обе устроены списком, и прежде диктор читал вкладку «элементом списка, 2 из 3».
    /// </remarks>
    [AvaloniaFact]
    public void A_tab_strip_reads_as_tabs()
    {
        var strip = new AxTabStrip { ItemsSource = new[] { "MainWindow.axaml", "App.axaml" } };
        var window = new Window { Width = 400, Height = 100, Content = strip };

        window.Show();
        strip.SelectedIndex = 1;
        window.UpdateLayout();

        var tabs = ControlAutomationPeer.CreatePeerForElement(strip);
        var tab = ControlAutomationPeer.CreatePeerForElement(strip.GetVisualDescendants().OfType<AxTabItem>().Last());

        Assert.Equal(AutomationControlType.Tab, tabs.GetAutomationControlType());
        Assert.IsAssignableFrom<ISelectionProvider>(tabs.GetProvider<ISelectionProvider>());
        Assert.Equal(AutomationControlType.TabItem, tab.GetAutomationControlType());
        Assert.Equal("App.axaml", tab.GetName());
        Assert.True(Assert.IsAssignableFrom<ISelectionItemProvider>(tab.GetProvider<ISelectionItemProvider>()).IsSelected);

        window.Close();
    }

    /// <summary>
    /// Узел дерева говорит диктору, свёрнут он, развёрнут или лист, раскрывается его командой и
    /// сообщает о раскрытии, как бы оно ни случилось.
    /// </summary>
    /// <remarks>
    /// Пир строки дерева в Avalonia 12 раскрытия не знает: без своего диктор не слышал у узла
    /// состояния, а раскрыть его своей командой не мог.
    /// </remarks>
    [AvaloniaFact]
    public void A_tree_node_tells_and_takes_its_expansion()
    {
        var folder = new AxTreeViewItem { Header = "Views", ItemsSource = new[] { new AxTreeViewItem { Header = "MainWindow.axaml" } } };
        var leaf = new AxTreeViewItem { Header = "Program.cs" };
        var window = new Window { Width = 300, Height = 200, Content = new AxTreeView { ItemsSource = new[] { folder, leaf } } };

        window.Show();
        window.UpdateLayout();

        var node = ControlAutomationPeer.CreatePeerForElement(folder);
        var expander = Assert.IsAssignableFrom<IExpandCollapseProvider>(node.GetProvider<IExpandCollapseProvider>());
        var heard = new List<ExpandCollapseState>();

        node.PropertyChanged += (_, e) =>
        {
            if (e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty)
                heard.Add((ExpandCollapseState)e.NewValue!);
        };

        Assert.Equal(AutomationControlType.TreeItem, node.GetAutomationControlType());
        Assert.Equal(ExpandCollapseState.Collapsed, expander.ExpandCollapseState);

        expander.Expand();

        Assert.True(folder.IsExpanded, "команда диктора не раскрыла узел");
        Assert.Equal(ExpandCollapseState.Expanded, expander.ExpandCollapseState);

        folder.IsExpanded = false;

        Assert.Equal([ExpandCollapseState.Expanded, ExpandCollapseState.Collapsed], heard);

        var still = Assert.IsAssignableFrom<IExpandCollapseProvider>(
            ControlAutomationPeer.CreatePeerForElement(leaf).GetProvider<IExpandCollapseProvider>());

        still.Expand();

        Assert.Equal(ExpandCollapseState.LeafNode, still.ExpandCollapseState);
        Assert.False(leaf.IsExpanded, "лист раскрылся");

        window.Close();
    }
}
