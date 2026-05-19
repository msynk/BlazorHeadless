using Microsoft.AspNetCore.Components;

namespace BlazorHeadless;

/// <summary>
/// Cascading context provided by <see cref="BhDialog"/> to its descendant parts
/// (<see cref="BhDialogPanel"/>, <see cref="BhDialogTitle"/>,
/// <see cref="BhDialogDescription"/>, <see cref="BhDialogBackdrop"/>). Carries
/// open state, ARIA wiring ids, and a close callback.
/// </summary>
public sealed class BhDialogContext : IBhCloseContext
{
    private readonly Func<Task> _closeAsync;
    private readonly Action<ElementReference> _registerPanel;
    private readonly Action<string> _registerTitle;
    private readonly Action<string> _registerDescription;

    internal BhDialogContext(
        bool isOpen,
        string baseId,
        Func<Task> closeAsync,
        Action<ElementReference> registerPanel,
        Action<string> registerTitle,
        Action<string> registerDescription)
    {
        IsOpen = isOpen;
        BaseId = baseId;
        _closeAsync = closeAsync;
        _registerPanel = registerPanel;
        _registerTitle = registerTitle;
        _registerDescription = registerDescription;
    }

    /// <summary>Whether the dialog is currently open.</summary>
    public bool IsOpen { get; }

    /// <summary>The base id used to derive the panel, title, and description ids.</summary>
    public string BaseId { get; }

    /// <summary>The deterministic id used as <c>aria-labelledby</c> on the dialog panel.</summary>
    public string TitleId => $"{BaseId}-title";

    /// <summary>The deterministic id used as <c>aria-describedby</c> on the dialog panel.</summary>
    public string DescriptionId => $"{BaseId}-description";

    /// <summary>The deterministic id of the dialog panel itself.</summary>
    public string PanelId => $"{BaseId}-panel";

    /// <summary>Closes the dialog by calling the parent's <see cref="BhDialog.OnClose"/>.</summary>
    public Task CloseAsync() => _closeAsync();

    internal void RegisterPanel(ElementReference panel) => _registerPanel(panel);
    internal void RegisterTitle(string id) => _registerTitle(id);
    internal void RegisterDescription(string id) => _registerDescription(id);
}

/// <summary>
/// Render-fragment context exposed by <see cref="BhDialog"/>. Allows consumers
/// to render content driven by open state and to call <c>Close()</c> from
/// anywhere inside the dialog.
/// </summary>
public sealed record BhDialogRenderContext
{
    /// <summary>Whether the dialog is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the dialog.</summary>
    public required Action Close { get; init; }
}
