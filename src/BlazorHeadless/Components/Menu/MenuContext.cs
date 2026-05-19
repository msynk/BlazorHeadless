using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// Cascading context provided by <see cref="HMenu"/> to its descendant menu parts
/// (<see cref="HMenuButton"/>, <see cref="HMenuItems"/>, <see cref="HMenuItem"/>).
/// Carries open / active state plus the registration and keyboard delegates the
/// parts use to coordinate.
/// </summary>
public sealed class MenuContext
{
    private readonly Func<HMenuItem, int> _registerItem;
    private readonly Action<HMenuItem> _unregisterItem;
    private readonly Action<int> _setActiveIndex;
    private readonly Func<int, Task> _activateItemAsync;
    private readonly Func<KeyboardEventArgs, Task> _handleButtonKeyDownAsync;
    private readonly Func<KeyboardEventArgs, Task> _handleMenuKeyDownAsync;
    private readonly Func<Task> _toggleAsync;
    private readonly Func<Task> _closeAsync;
    private readonly Action<ElementReference> _registerButton;

    internal MenuContext(
        bool isOpen,
        bool disabled,
        int activeIndex,
        string baseId,
        Func<HMenuItem, int> registerItem,
        Action<HMenuItem> unregisterItem,
        Action<int> setActiveIndex,
        Func<int, Task> activateItemAsync,
        Func<KeyboardEventArgs, Task> handleButtonKeyDownAsync,
        Func<KeyboardEventArgs, Task> handleMenuKeyDownAsync,
        Func<Task> toggleAsync,
        Func<Task> closeAsync,
        Action<ElementReference> registerButton)
    {
        IsOpen = isOpen;
        Disabled = disabled;
        ActiveIndex = activeIndex;
        BaseId = baseId;
        _registerItem = registerItem;
        _unregisterItem = unregisterItem;
        _setActiveIndex = setActiveIndex;
        _activateItemAsync = activateItemAsync;
        _handleButtonKeyDownAsync = handleButtonKeyDownAsync;
        _handleMenuKeyDownAsync = handleMenuKeyDownAsync;
        _toggleAsync = toggleAsync;
        _closeAsync = closeAsync;
        _registerButton = registerButton;
    }

    /// <summary>Whether the menu is currently open.</summary>
    public bool IsOpen { get; }

    /// <summary>Whether the menu is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>The index of the currently "active" (highlighted) item, or -1 if none.</summary>
    public int ActiveIndex { get; }

    /// <summary>Base id used to derive button, panel, and per-item ids.</summary>
    public string BaseId { get; }

    /// <summary>Deterministic id of the menu trigger button.</summary>
    public string ButtonId => $"{BaseId}-button";

    /// <summary>Deterministic id of the menu items panel.</summary>
    public string ItemsId => $"{BaseId}-items";

    /// <summary>Returns the deterministic id for the menu item at <paramref name="index"/>.</summary>
    public string GetItemId(int index) => $"{BaseId}-item-{index}";

    internal int RegisterItem(HMenuItem item) => _registerItem(item);
    internal void UnregisterItem(HMenuItem item) => _unregisterItem(item);
    internal void SetActiveIndex(int index) => _setActiveIndex(index);
    internal Task ActivateItemAsync(int index) => _activateItemAsync(index);
    internal Task HandleButtonKeyDownAsync(KeyboardEventArgs args) => _handleButtonKeyDownAsync(args);
    internal Task HandleMenuKeyDownAsync(KeyboardEventArgs args) => _handleMenuKeyDownAsync(args);
    internal Task ToggleAsync() => _toggleAsync();
    internal Task CloseAsync() => _closeAsync();
    internal void RegisterButton(ElementReference button) => _registerButton(button);

    /// <summary>Gets the button element reference for anchor positioning.</summary>
    internal ElementReference ButtonRef { get; private set; }

    /// <summary>Sets the button element reference (called during context creation).</summary>
    internal void SetButtonRef(ElementReference buttonRef) => ButtonRef = buttonRef;
}

/// <summary>Render-fragment context exposed by <see cref="HMenuButton"/>.</summary>
public sealed record MenuButtonRenderContext
{
    /// <summary>Whether the menu panel is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether the menu is disabled.</summary>
    public required bool Disabled { get; init; }
}

/// <summary>Render-fragment context exposed by <see cref="HMenuItem"/>.</summary>
public sealed record MenuItemRenderContext
{
    /// <summary>Whether this item is currently the "active" (highlighted) item.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Whether this item is disabled.</summary>
    public required bool Disabled { get; init; }
}
