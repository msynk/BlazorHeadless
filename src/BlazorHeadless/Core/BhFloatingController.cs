using Microsoft.JSInterop;

namespace BlazorHeadless;

/// <summary>Why a floating layer asked to be dismissed.</summary>
public enum BhDismissReason
{
    /// <summary>The Escape key was pressed.</summary>
    EscapeKey,

    /// <summary>A pointer was pressed outside the layer (and its trigger / nested layers).</summary>
    PointerDownOutside,

    /// <summary>Focus moved outside the layer.</summary>
    FocusOutside,

    /// <summary>The layer was closed from code (selection, close button, …).</summary>
    Programmatic,
}

/// <summary>Layer (dismissal) behaviour of a floating surface.</summary>
public sealed class BhLayerOptions
{
    /// <summary>Escape dismisses the layer when focus is outside every layer. Default: true.</summary>
    public bool Escape { get; set; } = true;

    /// <summary>Pointer-down outside dismisses the layer. Default: true.</summary>
    public bool PointerDownOutside { get; set; } = true;

    /// <summary>Focus moving outside dismisses the layer. Default: false.</summary>
    public bool FocusOutside { get; set; }

    /// <summary>A modal layer shields the layers below it from outside interaction.</summary>
    public bool Modal { get; set; }

    /// <summary>Hover-driven layer (tooltip, hover card): Escape always dismisses it first.</summary>
    public bool Hover { get; set; }

    /// <summary>Handle of the parent surface (nested menus, popovers opened from popovers).</summary>
    public int ParentHandle { get; set; }

    /// <summary>Extra element ids that belong to the layer (trigger, input, …).</summary>
    public List<string?> BranchIds { get; } = new();

    internal Dictionary<string, object?> ToJs() => new()
    {
        ["escape"] = Escape,
        ["outside"] = PointerDownOutside,
        ["focusOutside"] = FocusOutside,
        ["modal"] = Modal,
        ["hover"] = Hover,
        ["parentFloating"] = ParentHandle,
        ["ids"] = BranchIds.Where(id => !string.IsNullOrEmpty(id)).ToArray(),
    };
}

/// <summary>Everything the JS runtime needs to open a floating surface.</summary>
public sealed class BhFloatingRequest
{
    /// <summary>The id of the content element (layer, focus scope, positioning target).</summary>
    public required string ContentId { get; init; }

    /// <summary>
    /// Elements moved by the portal (in order) and kept visible by modal isolation.
    /// Defaults to <see cref="ContentId"/>. Use it to move a wrapper (dialog root)
    /// or several siblings (overlay + content) together.
    /// </summary>
    public IReadOnlyList<string?>? SurfaceIds { get; init; }

    /// <summary>Teleport the surface to the portal root (or <see cref="PortalTarget"/>) while open.</summary>
    public bool Portal { get; init; }

    /// <summary>Optional id / CSS selector of the portal container.</summary>
    public string? PortalTarget { get; init; }

    /// <summary>Id of the reference element used for anchor positioning.</summary>
    public string? ReferenceId { get; init; }

    /// <summary>Virtual reference point (viewport coordinates) used instead of <see cref="ReferenceId"/>.</summary>
    public (double X, double Y)? Point { get; init; }

    /// <summary>Anchor positioning options; null disables positioning.</summary>
    public BhAnchorOptions? Anchor { get; init; }

    /// <summary>Id of an arrow element to position along the reference.</summary>
    public string? ArrowId { get; init; }

    /// <summary>Dismissal behaviour; null disables the layer.</summary>
    public BhLayerOptions? Layer { get; init; }

    /// <summary>Hide the rest of the page from assistive tech and lock scrolling.</summary>
    public bool Modal { get; init; }

    /// <summary>Lock document scroll when <see cref="Modal"/>. Default: true.</summary>
    public bool LockScroll { get; init; } = true;

    /// <summary>Focus management; null leaves focus alone.</summary>
    public BhFocusScopeOptions? Focus { get; init; }

    /// <summary>Whether the content is only revealed by the runtime (anchored or portalled).</summary>
    public bool RevealedByScript => Anchor is not null || Portal;

    internal Dictionary<string, object?> ToJs()
    {
        object? reference = Point is { } p
            ? new Dictionary<string, object?> { ["x"] = p.X, ["y"] = p.Y }
            : ReferenceId;

        return new Dictionary<string, object?>
        {
            ["portalIds"] = SurfaceIds?.Where(id => !string.IsNullOrEmpty(id)).ToArray(),
            ["portal"] = Portal ? (object?)PortalTarget ?? true : null,
            ["reference"] = reference,
            ["anchor"] = Anchor?.ToJs(ArrowId),
            ["layer"] = Layer?.ToJs(),
            ["modal"] = Modal ? new Dictionary<string, object?> { ["hideOthers"] = true, ["scrollLock"] = LockScroll } : null,
            ["focus"] = Focus?.ToJs(),
        };
    }
}

/// <summary>
/// Drives the lifecycle of one floating surface (popover panel, menu, listbox
/// options, tooltip, dialog …) against the JS runtime, including presence:
/// the surface stays visible with <c>data-state="closed"</c> until its CSS exit
/// animation has finished, so consumers can animate both directions with plain
/// CSS keyframes or transitions.
///
/// <para>
/// Owners call <see cref="SyncAsync"/> from <c>OnAfterRenderAsync</c> and use
/// <see cref="ShouldHide"/> / <see cref="IsPresent"/> while rendering.
/// </para>
/// </summary>
public sealed class BhFloatingController : IAsyncDisposable
{
    private enum Phase { Closed, Opening, Open, Closing }

    private readonly BhInterop _interop;
    private readonly Func<BhDismissReason, Task> _onDismiss;
    private readonly Action _stateHasChanged;
    private DotNetObjectReference<BhFloatingController>? _selfRef;
    private Phase _phase;
    private bool _syncing;
    private bool _disposed;

    /// <summary>Creates a controller.</summary>
    /// <param name="interop">The JS runtime wrapper.</param>
    /// <param name="onDismiss">Invoked when the layer requests dismissal (Escape, outside click, focus outside).</param>
    /// <param name="stateHasChanged">The owner's <c>StateHasChanged</c>.</param>
    public BhFloatingController(BhInterop interop, Func<BhDismissReason, Task> onDismiss, Action stateHasChanged)
    {
        _interop = interop;
        _onDismiss = onDismiss;
        _stateHasChanged = stateHasChanged;
    }

    /// <summary>The JS handle of the open surface, or 0. Pass to child layers as their parent.</summary>
    public int Handle { get; private set; }

    /// <summary>Whether the surface is mounted in JS (open or animating out).</summary>
    public bool IsActive => _phase is Phase.Open or Phase.Closing;

    /// <summary>Whether the content should be rendered at all (open, or still animating out).</summary>
    public bool IsPresent(bool isOpen) => isOpen || _phase is Phase.Open or Phase.Closing;

    /// <summary>
    /// Whether the content element should carry the <c>hidden</c> attribute.
    /// Closed content is hidden right away — elements marked <c>data-bh-presence</c>
    /// are kept visible by the runtime while a CSS exit animation runs, so closing
    /// is instant when nothing animates. Script-revealed surfaces (anchored /
    /// portalled) stay hidden until the runtime has positioned them, which
    /// prevents a flash at the wrong spot.
    /// </summary>
    public bool ShouldHide(bool isOpen, bool revealedByScript)
        => !isOpen || (revealedByScript && _phase is Phase.Closed or Phase.Opening);

    /// <summary>
    /// Reconciles the JS surface with <paramref name="isOpen"/>. Safe to call on
    /// every render; transitions are serialized and re-evaluated after each await.
    /// </summary>
    public async Task SyncAsync(Func<bool> isOpen, Func<BhFloatingRequest> request)
    {
        if (_syncing || _disposed) return;
        _syncing = true;
        try
        {
            while (!_disposed)
            {
                var open = isOpen();
                if (open && _phase == Phase.Closed)
                {
                    _phase = Phase.Opening;
                    var req = request();
                    _selfRef ??= DotNetObjectReference.Create(this);
                    Handle = await _interop.FloatingOpenAsync(_selfRef, req.ContentId, req.ToJs());
                    _phase = Phase.Open;
                    _stateHasChanged();
                    continue;
                }

                if (!open && _phase == Phase.Open)
                {
                    _phase = Phase.Closing;
                    var handle = Handle;
                    Handle = 0;
                    await _interop.FloatingCloseAsync(handle, restoreFocus: true, waitForExit: true);
                    _phase = Phase.Closed;
                    _stateHasChanged();
                    continue;
                }

                break;
            }
        }
        finally
        {
            _syncing = false;
        }
    }

    /// <summary>Re-applies positioning (e.g. after the anchor options changed).</summary>
    public ValueTask UpdateAsync(BhAnchorOptions? anchor = null, string? arrowId = null)
    {
        if (Handle <= 0) return ValueTask.CompletedTask;
        var options = anchor is null ? null : new Dictionary<string, object?> { ["anchor"] = anchor.ToJs(arrowId) };
        return _interop.FloatingUpdateAsync(Handle, options);
    }

    /// <summary>Moves the virtual anchor point (context menus re-opened at a new position).</summary>
    public ValueTask SetPointAsync(double x, double y) => _interop.FloatingSetPointAsync(Handle, x, y);

    /// <summary>Invoked by the JS runtime when the layer should be dismissed.</summary>
    [JSInvokable]
    public Task OnLayerDismiss(string reason)
    {
        if (_disposed || _phase != Phase.Open) return Task.CompletedTask;
        var parsed = reason switch
        {
            "escape" => BhDismissReason.EscapeKey,
            "focus-outside" => BhDismissReason.FocusOutside,
            _ => BhDismissReason.PointerDownOutside,
        };
        return _onDismiss(parsed);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        if (Handle > 0)
        {
            var handle = Handle;
            Handle = 0;
            await _interop.FloatingCloseAsync(handle, restoreFocus: true, waitForExit: false);
        }
        _selfRef?.Dispose();
        _selfRef = null;
    }
}
