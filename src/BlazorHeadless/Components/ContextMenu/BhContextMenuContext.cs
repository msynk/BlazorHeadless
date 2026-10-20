namespace BlazorHeadless;

/// <summary>
/// Cascading context provided by <see cref="BhContextMenu"/> to its trigger and
/// content. Carries open state, the pointer position and ARIA wiring ids.
/// </summary>
public sealed class BhContextMenuContext : IBhCloseContext
{
    internal BhContextMenuContext(BhContextMenu menu, bool isOpen, bool disabled, double x, double y, string baseId)
    {
        Menu = menu;
        IsOpen = isOpen;
        Disabled = disabled;
        X = x;
        Y = y;
        BaseId = baseId;
    }

    internal BhContextMenu Menu { get; }

    /// <summary>Whether the menu is open.</summary>
    public bool IsOpen { get; }

    /// <summary>Whether the context menu is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>Viewport X coordinate the menu opened at.</summary>
    public double X { get; }

    /// <summary>Viewport Y coordinate the menu opened at.</summary>
    public double Y { get; }

    /// <summary>Base id used to derive the trigger and content ids.</summary>
    public string BaseId { get; }

    /// <summary>Deterministic id of the trigger region.</summary>
    public string TriggerId => $"{BaseId}-trigger";

    /// <summary>Deterministic id of the menu panel.</summary>
    public string ContentId => $"{BaseId}-content";

    /// <summary>Opens the menu at a viewport point.</summary>
    public Task OpenAtAsync(double x, double y) => Menu.OpenAtAsync(x, y);

    /// <summary>Closes the menu.</summary>
    public Task CloseAsync() => Menu.CloseAsync();
}

/// <summary>Render-fragment context exposed by <see cref="BhContextMenuTrigger"/>.</summary>
public sealed record BhContextMenuTriggerRenderContext
{
    /// <summary>Whether the menu is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether the trigger is disabled.</summary>
    public required bool Disabled { get; init; }
}

/// <summary>Render-fragment context exposed by <see cref="BhContextMenuItem"/>.</summary>
public sealed record BhContextMenuItemRenderContext
{
    /// <summary>Whether this item is highlighted.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Whether this item is disabled.</summary>
    public required bool Disabled { get; init; }
}
