using Microsoft.AspNetCore.Components;

namespace BlazorHeadless;

/// <summary>
/// Cascading context provided by <see cref="BhAlertDialog"/> to its descendant
/// parts (<see cref="BhAlertDialogTrigger"/>, <see cref="BhAlertDialogOverlay"/>,
/// <see cref="BhAlertDialogContent"/>, <see cref="BhAlertDialogTitle"/>,
/// <see cref="BhAlertDialogDescription"/>, <see cref="BhAlertDialogCancel"/>,
/// <see cref="BhAlertDialogAction"/>). Carries open state, ARIA wiring ids, and
/// the open/close callbacks.
/// </summary>
public sealed class BhAlertDialogContext : IBhCloseContext
{
    private readonly Func<Task> _openAsync;
    private readonly Func<Task> _closeAsync;
    private readonly Action<ElementReference> _registerContent;
    private readonly Action<ElementReference> _registerTrigger;
    private readonly Action<ElementReference> _registerCancel;

    internal BhAlertDialogContext(
        bool isOpen,
        string baseId,
        Func<Task> openAsync,
        Func<Task> closeAsync,
        Action<ElementReference> registerContent,
        Action<ElementReference> registerTrigger,
        Action<ElementReference> registerCancel)
    {
        IsOpen = isOpen;
        BaseId = baseId;
        _openAsync = openAsync;
        _closeAsync = closeAsync;
        _registerContent = registerContent;
        _registerTrigger = registerTrigger;
        _registerCancel = registerCancel;
    }

    /// <summary>Whether the alert dialog is currently open.</summary>
    public bool IsOpen { get; }

    /// <summary>Base id used to derive the trigger, content, title, and description ids.</summary>
    public string BaseId { get; }

    /// <summary>Deterministic id of the trigger button.</summary>
    public string TriggerId => $"{BaseId}-trigger";

    /// <summary>Deterministic id of the content surface.</summary>
    public string ContentId => $"{BaseId}-content";

    /// <summary>The deterministic id used as <c>aria-labelledby</c> on the content.</summary>
    public string TitleId => $"{BaseId}-title";

    /// <summary>The deterministic id used as <c>aria-describedby</c> on the content.</summary>
    public string DescriptionId => $"{BaseId}-description";

    /// <summary>Opens the alert dialog.</summary>
    public Task OpenAsync() => _openAsync();

    /// <summary>Closes the alert dialog.</summary>
    public Task CloseAsync() => _closeAsync();

    internal void RegisterContent(ElementReference content) => _registerContent(content);
    internal void RegisterTrigger(ElementReference trigger) => _registerTrigger(trigger);
    internal void RegisterCancel(ElementReference cancel) => _registerCancel(cancel);
}

/// <summary>
/// Render-fragment context exposed by <see cref="BhAlertDialog"/> parts for
/// state-driven rendering.
/// </summary>
public sealed record BhAlertDialogRenderContext
{
    /// <summary>Whether the alert dialog is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the alert dialog.</summary>
    public required Action Close { get; init; }
}
