namespace BlazorHeadless;

/// <summary>The orientation of an <see cref="BhTabGroup"/>.</summary>
public enum BhTabsOrientation
{
    /// <summary>Tabs flow horizontally; Left / Right arrow keys navigate.</summary>
    Horizontal,

    /// <summary>Tabs flow vertically; Up / Down arrow keys navigate.</summary>
    Vertical,
}

/// <summary>
/// Cascading context provided by <see cref="BhTabGroup"/> to its tab list, tabs and panels.
/// </summary>
public sealed class BhTabsContext
{
    internal BhTabsContext(
        BhTabGroup group,
        int selectedIndex,
        bool disabled,
        BhOrientation orientation,
        bool manual,
        string baseId,
        BhCollection<BhTab> tabs,
        BhCollection<BhTabPanel> panels)
    {
        Group = group;
        SelectedIndex = selectedIndex;
        Disabled = disabled;
        LayoutOrientation = orientation;
        Manual = manual;
        BaseId = baseId;
        Tabs = tabs;
        Panels = panels;
    }

    internal BhTabGroup Group { get; }
    internal BhCollection<BhTab> Tabs { get; }
    internal BhCollection<BhTabPanel> Panels { get; }

    /// <summary>The selected index (index mode).</summary>
    public int SelectedIndex { get; }

    /// <summary>Whether the whole group is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>The orientation.</summary>
    public BhOrientation LayoutOrientation { get; }

    /// <summary>The orientation (legacy enum).</summary>
    public BhTabsOrientation Orientation => LayoutOrientation == BhOrientation.Vertical ? BhTabsOrientation.Vertical : BhTabsOrientation.Horizontal;

    /// <summary>Whether activation is manual.</summary>
    public bool Manual { get; }

    /// <summary>The group's base id.</summary>
    public string BaseId { get; }
}

/// <summary>Render-fragment context exposed by <see cref="BhTab"/>.</summary>
public sealed record BhTabRenderContext
{
    /// <summary>Whether this tab is selected.</summary>
    public required bool IsSelected { get; init; }

    /// <summary>Whether this tab (or the group) is disabled.</summary>
    public required bool Disabled { get; init; }
}

/// <summary>Render-fragment context exposed by <see cref="BhTabPanel"/>.</summary>
public sealed record BhTabPanelRenderContext
{
    /// <summary>Whether this panel is the one shown.</summary>
    public required bool IsSelected { get; init; }
}
