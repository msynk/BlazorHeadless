using Microsoft.AspNetCore.Components;

namespace HeadlessUI.Blazor;

/// <summary>
/// Cascading context provided by <see cref="HPopover"/> to its descendant parts
/// (<see cref="HPopoverButton"/>, <see cref="HPopoverPanel"/>,
/// <see cref="HPopoverBackdrop"/>). Carries open state, ARIA wiring ids, and
/// the open/close callbacks.
/// </summary>
public sealed class PopoverContext
{
    private readonly Func<Task> _openAsync;
    private readonly Func<Task> _closeAsync;
    private readonly Func<Task> _toggleAsync;
    private readonly Action<ElementReference> _registerPanel;
    private readonly Action<ElementReference> _registerButton;

    internal PopoverContext(
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
}

/// <summary>
/// Render-fragment context exposed by <see cref="HPopoverButton"/> and
/// <see cref="HPopoverPanel"/> for state-driven rendering.
/// </summary>
public sealed record PopoverRenderContext
{
    /// <summary>Whether the popover panel is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the popover. Useful from inside the panel (e.g. a "Close" link).</summary>
    public required Action Close { get; init; }
}

/// <summary>
/// Cascading context provided by <see cref="HPopoverGroup"/> to coordinate
/// mutual exclusion between sibling popovers.
/// </summary>
public sealed class PopoverGroupContext
{
    private readonly Action<HPopover> _register;
    private readonly Action<HPopover> _unregister;
    private readonly Func<HPopover, Task> _closeOthersAsync;

    internal PopoverGroupContext(
        Action<HPopover> register,
        Action<HPopover> unregister,
        Func<HPopover, Task> closeOthersAsync)
    {
        _register = register;
        _unregister = unregister;
        _closeOthersAsync = closeOthersAsync;
    }

    internal void Register(HPopover popover) => _register(popover);
    internal void Unregister(HPopover popover) => _unregister(popover);
    internal Task CloseOthersAsync(HPopover except) => _closeOthersAsync(except);
}
