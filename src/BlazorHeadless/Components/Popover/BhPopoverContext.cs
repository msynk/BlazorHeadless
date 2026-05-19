using Microsoft.AspNetCore.Components;

namespace BlazorHeadless;

/// <summary>
/// Cascading context provided by <see cref="BhPopover"/> to its descendant parts
/// (<see cref="BhPopoverButton"/>, <see cref="BhPopoverPanel"/>,
/// <see cref="BhPopoverBackdrop"/>). Carries open state, ARIA wiring ids, and
/// the open/close callbacks.
/// </summary>
public sealed class BhPopoverContext : IBhCloseContext
{
    private readonly Func<Task> _openAsync;
    private readonly Func<Task> _closeAsync;
    private readonly Func<Task> _toggleAsync;
    private readonly Action<ElementReference> _registerPanel;
    private readonly Action<ElementReference> _registerButton;

    internal BhPopoverContext(
        bool isOpen,
        string baseId,
        Func<Task> openAsync,
        Func<Task> closeAsync,
        Func<Task> toggleAsync,
        Action<ElementReference> registerPanel,
        Action<ElementReference> registerButton)
    {
        IsOpen = isOpen;
        BaseId = baseId;
        _openAsync = openAsync;
        _closeAsync = closeAsync;
        _toggleAsync = toggleAsync;
        _registerPanel = registerPanel;
        _registerButton = registerButton;
    }

    /// <summary>Whether the popover panel is currently open.</summary>
    public bool IsOpen { get; }

    /// <summary>Base id used to derive button and panel ids.</summary>
    public string BaseId { get; }

    /// <summary>Deterministic id of the popover button.</summary>
    public string ButtonId => $"{BaseId}-button";

    /// <summary>Deterministic id of the popover panel.</summary>
    public string PanelId => $"{BaseId}-panel";

    /// <summary>Opens the popover.</summary>
    public Task OpenAsync() => _openAsync();

    /// <summary>Closes the popover.</summary>
    public Task CloseAsync() => _closeAsync();

    /// <summary>Toggles the popover open or closed.</summary>
    public Task ToggleAsync() => _toggleAsync();

    internal void RegisterPanel(ElementReference panel) => _registerPanel(panel);
    internal void RegisterButton(ElementReference button) => _registerButton(button);

    /// <summary>Gets the button element reference for anchor positioning.</summary>
    internal ElementReference ButtonRef { get; private set; }

    /// <summary>Sets the button element reference.</summary>
    internal void SetButtonRef(ElementReference buttonRef) => ButtonRef = buttonRef;
}

/// <summary>
/// Render-fragment context exposed by <see cref="BhPopoverButton"/> and
/// <see cref="BhPopoverPanel"/> for state-driven rendering.
/// </summary>
public sealed record BhPopoverRenderContext
{
    /// <summary>Whether the popover panel is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the popover. Useful from inside the panel (e.g. a "Close" link).</summary>
    public required Action Close { get; init; }
}

/// <summary>
/// Cascading context provided by <see cref="BhPopoverGroup"/> to coordinate
/// mutual exclusion between sibling popovers.
/// </summary>
public sealed class BhPopoverGroupContext
{
    private readonly Action<BhPopover> _register;
    private readonly Action<BhPopover> _unregister;
    private readonly Func<BhPopover, Task> _closeOthersAsync;

    internal BhPopoverGroupContext(
        Action<BhPopover> register,
        Action<BhPopover> unregister,
        Func<BhPopover, Task> closeOthersAsync)
    {
        _register = register;
        _unregister = unregister;
        _closeOthersAsync = closeOthersAsync;
    }

    internal void Register(BhPopover popover) => _register(popover);
    internal void Unregister(BhPopover popover) => _unregister(popover);
    internal Task CloseOthersAsync(BhPopover except) => _closeOthersAsync(except);
}
