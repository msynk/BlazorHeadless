using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>The orientation of an <see cref="BhTabGroup"/>. Drives keyboard navigation and the <c>aria-orientation</c> attribute.</summary>
public enum BhTabsOrientation
{
    /// <summary>Tabs flow horizontally; Left/Right arrow keys navigate.</summary>
    Horizontal,

    /// <summary>Tabs flow vertically; Up/Down arrow keys navigate.</summary>
    Vertical,
}

/// <summary>
/// Cascading context provided by <see cref="BhTabGroup"/> to its descendants
/// (<see cref="BhTabList"/>, <see cref="BhTab"/>, <see cref="BhTabPanels"/>,
/// <see cref="BhTabPanel"/>). Carries selection state, ARIA wiring helpers,
/// and the registration callbacks tabs and panels use to claim an index.
/// </summary>
public sealed class BhTabsContext
{
    private readonly Func<BhTab, int> _registerTab;
    private readonly Action<BhTab> _unregisterTab;
    private readonly Func<BhTabPanel, int> _registerPanel;
    private readonly Action<BhTabPanel> _unregisterPanel;
    private readonly Func<int, Task> _selectAsync;
    private readonly Func<int, KeyboardEventArgs, Task> _handleKeyDownAsync;

    internal BhTabsContext(
        int selectedIndex,
        bool disabled,
        BhTabsOrientation orientation,
        bool manual,
        string baseId,
        Func<BhTab, int> registerTab,
        Action<BhTab> unregisterTab,
        Func<BhTabPanel, int> registerPanel,
        Action<BhTabPanel> unregisterPanel,
        Func<int, Task> selectAsync,
        Func<int, KeyboardEventArgs, Task> handleKeyDownAsync)
    {
        SelectedIndex = selectedIndex;
        Disabled = disabled;
        Orientation = orientation;
        Manual = manual;
        BaseId = baseId;
        _registerTab = registerTab;
        _unregisterTab = unregisterTab;
        _registerPanel = registerPanel;
        _unregisterPanel = unregisterPanel;
        _selectAsync = selectAsync;
        _handleKeyDownAsync = handleKeyDownAsync;
    }

    /// <summary>The currently selected tab index.</summary>
    public int SelectedIndex { get; }

    /// <summary>Whether interaction is globally disabled for the tab group.</summary>
    public bool Disabled { get; }

    /// <summary>The orientation of the tab group (horizontal or vertical).</summary>
    public BhTabsOrientation Orientation { get; }

    /// <summary>
    /// When true, arrow keys only move keyboard focus; Space/Enter is required to
    /// activate. When false (the default), arrow keys both move focus and select.
    /// </summary>
    public bool Manual { get; }

    /// <summary>The base id used to derive deterministic tab and panel ids.</summary>
    public string BaseId { get; }

    /// <summary>Returns the deterministic HTML id for the tab at <paramref name="index"/>.</summary>
    public string GetTabId(int index) => $"{BaseId}-tab-{index}";

    /// <summary>Returns the deterministic HTML id for the panel at <paramref name="index"/>.</summary>
    public string GetPanelId(int index) => $"{BaseId}-panel-{index}";

    internal int RegisterTab(BhTab tab) => _registerTab(tab);
    internal void UnregisterTab(BhTab tab) => _unregisterTab(tab);
    internal int RegisterPanel(BhTabPanel panel) => _registerPanel(panel);
    internal void UnregisterPanel(BhTabPanel panel) => _unregisterPanel(panel);
    internal Task SelectAsync(int index) => _selectAsync(index);
    internal Task HandleKeyDownAsync(int index, KeyboardEventArgs args) => _handleKeyDownAsync(index, args);
}

/// <summary>
/// Render-fragment context exposed by <see cref="BhTab"/> so consumers can render
/// state-driven content (active styling, icons, badges).
/// </summary>
public sealed record BhTabRenderContext
{
    /// <summary>Whether this tab is currently selected.</summary>
    public required bool IsSelected { get; init; }

    /// <summary>Whether this tab (or the entire tab group) is disabled.</summary>
    public required bool Disabled { get; init; }
}

/// <summary>
/// Render-fragment context exposed by <see cref="BhTabPanel"/> for state-driven content.
/// </summary>
public sealed record BhTabPanelRenderContext
{
    /// <summary>Whether this panel is the one currently shown.</summary>
    public required bool IsSelected { get; init; }
}
