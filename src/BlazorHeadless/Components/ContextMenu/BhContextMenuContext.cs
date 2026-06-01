using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// Cascading context provided by <see cref="BhContextMenu"/> to its descendant
/// parts (<see cref="BhContextMenuTrigger"/>, <see cref="BhContextMenuContent"/>,
/// <see cref="BhContextMenuItem"/>, <see cref="BhContextMenuLabel"/>,
/// <see cref="BhContextMenuGroup"/>, <see cref="BhContextMenuSeparator"/>).
///
/// <para>
/// Carries open / active state plus the pointer coordinates at which the menu
/// was summoned, along with the registration and keyboard delegates the parts
/// use to coordinate. Implements <see cref="IBhCloseContext"/> so a
/// <see cref="BhCloseButton"/> placed inside the content closes the menu.
/// </para>
/// </summary>
public sealed class BhContextMenuContext : IBhCloseContext
{
    private readonly Func<BhContextMenuItem, int> _registerItem;
    private readonly Action<BhContextMenuItem> _unregisterItem;
    private readonly Action<int> _setActiveIndex;
    private readonly Func<int, Task> _activateItemAsync;
    private readonly Func<KeyboardEventArgs, Task> _handleContentKeyDownAsync;
    private readonly Func<double, double, Task> _openAtAsync;
    private readonly Func<Task> _closeAsync;

    internal BhContextMenuContext(
        bool isOpen,
        bool disabled,
        int activeIndex,
        double x,
        double y,
        string baseId,
        Func<BhContextMenuItem, int> registerItem,
        Action<BhContextMenuItem> unregisterItem,
        Action<int> setActiveIndex,
        Func<int, Task> activateItemAsync,
        Func<KeyboardEventArgs, Task> handleContentKeyDownAsync,
        Func<double, double, Task> openAtAsync,
        Func<Task> closeAsync)
    {
        IsOpen = isOpen;
        Disabled = disabled;
        ActiveIndex = activeIndex;
        X = x;
        Y = y;
        BaseId = baseId;
        _registerItem = registerItem;
        _unregisterItem = unregisterItem;
        _setActiveIndex = setActiveIndex;
        _activateItemAsync = activateItemAsync;
        _handleContentKeyDownAsync = handleContentKeyDownAsync;
        _openAtAsync = openAtAsync;
        _closeAsync = closeAsync;
    }

    /// <summary>Whether the context menu is currently open.</summary>
    public bool IsOpen { get; }

    /// <summary>Whether the context menu is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>The index of the currently "active" (highlighted) item, or -1 if none.</summary>
    public int ActiveIndex { get; }

    /// <summary>The viewport X coordinate (px) where the menu was summoned.</summary>
    public double X { get; }

    /// <summary>The viewport Y coordinate (px) where the menu was summoned.</summary>
    public double Y { get; }

    /// <summary>Base id used to derive the trigger, content, and per-item ids.</summary>
    public string BaseId { get; }

    /// <summary>Deterministic id of the trigger element.</summary>
    public string TriggerId => $"{BaseId}-trigger";

    /// <summary>Deterministic id of the content panel.</summary>
    public string ContentId => $"{BaseId}-content";

    /// <summary>Returns the deterministic id for the menu item at <paramref name="index"/>.</summary>
    public string GetItemId(int index) => $"{BaseId}-item-{index}";

    internal int RegisterItem(BhContextMenuItem item) => _registerItem(item);
    internal void UnregisterItem(BhContextMenuItem item) => _unregisterItem(item);
    internal void SetActiveIndex(int index) => _setActiveIndex(index);
    internal Task ActivateItemAsync(int index) => _activateItemAsync(index);
    internal Task HandleContentKeyDownAsync(KeyboardEventArgs args) => _handleContentKeyDownAsync(args);
    internal Task OpenAtAsync(double x, double y) => _openAtAsync(x, y);
    internal Task CloseAsync() => _closeAsync();

    /// <inheritdoc />
    Task IBhCloseContext.CloseAsync() => _closeAsync();
}

/// <summary>Render-fragment context exposed by <see cref="BhContextMenuTrigger"/>.</summary>
public sealed record BhContextMenuTriggerRenderContext
{
    /// <summary>Whether the context menu is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether the context menu is disabled.</summary>
    public required bool Disabled { get; init; }
}

/// <summary>Render-fragment context exposed by <see cref="BhContextMenuItem"/>.</summary>
public sealed record BhContextMenuItemRenderContext
{
    /// <summary>Whether this item is currently the "active" (highlighted) item.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Whether this item is disabled.</summary>
    public required bool Disabled { get; init; }
}
