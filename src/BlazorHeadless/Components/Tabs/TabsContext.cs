using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>The orientation of an <see cref="HTabGroup"/>. Drives keyboard navigation and the <c>aria-orientation</c> attribute.</summary>
public enum TabsOrientation
{
    /// <summary>Tabs flow horizontally; Left/Right arrow keys navigate.</summary>
    Horizontal,

    /// <summary>Tabs flow vertically; Up/Down arrow keys navigate.</summary>
    Vertical,
}

/// <summary>
/// Cascading context provided by <see cref="HTabGroup"/> to its descendants
/// (<see cref="HTabList"/>, <see cref="HTab"/>, <see cref="HTabPanels"/>,
/// <see cref="HTabPanel"/>). Carries selection state, ARIA wiring helpers,
/// and the registration callbacks tabs and panels use to claim an index.
/// </summary>
public sealed class TabsContext
{
    private readonly Func<HTab, int> _registerTab;
    private readonly Action<HTab> _unregisterTab;
    private readonly Func<HTabPanel, int> _registerPanel;
    private readonly Action<HTabPanel> _unregisterPanel;
    private readonly Func<int, Task> _selectAsync;
    private readonly Func<int, KeyboardEventArgs, Task> _handleKeyDownAsync;

    internal TabsContext(
        int selectedIndex,
        bool disabled,
        TabsOrientation orientation,
        bool manual,
        string baseId,
        Func<HTab, int> registerTab,
        Action<HTab> unregisterTab,
        Func<HTabPanel, int> registerPanel,
        Action<HTabPanel> unregisterPanel,
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
    public TabsOrientation Orientation { get; }

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

    internal int RegisterTab(HTab tab) => _registerTab(tab);
    internal void UnregisterTab(HTab tab) => _unregisterTab(tab);
    internal int RegisterPanel(HTabPanel panel) => _registerPanel(panel);
    internal void UnregisterPanel(HTabPanel panel) => _unregisterPanel(panel);
    internal Task SelectAsync(int index) => _selectAsync(index);
    internal Task HandleKeyDownAsync(int index, KeyboardEventArgs args) => _handleKeyDownAsync(index, args);
}

/// <summary>
/// Render-fragment context exposed by <see cref="HTab"/> so consumers can render
/// state-driven content (active styling, icons, badges).
/// </summary>
public sealed record TabRenderContext
{
    /// <summary>Whether this tab is currently selected.</summary>
    public required bool IsSelected { get; init; }

    /// <summary>Whether this tab (or the entire tab group) is disabled.</summary>
    public required bool Disabled { get; init; }
}

/// <summary>
/// Render-fragment context exposed by <see cref="HTabPanel"/> for state-driven content.
/// </summary>
public sealed record TabPanelRenderContext
{
    /// <summary>Whether this panel is the one currently shown.</summary>
    public required bool IsSelected { get; init; }
}
