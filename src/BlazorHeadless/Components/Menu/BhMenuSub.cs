using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A nested submenu. Contains a <see cref="BhMenuSubTrigger"/> (an item of the
/// parent menu) and a <see cref="BhMenuSubContent"/> (the nested panel). Works in
/// dropdown menus, context menus and menubars, to any depth.
///
/// <para><b>Interaction:</b> hovering the trigger opens the submenu after
/// <see cref="OpenDelay"/>; leaving it closes after <see cref="CloseDelay"/> unless
/// the pointer reaches the submenu (so diagonal movement doesn't close it).
/// ArrowRight / Enter / Space open it and move focus in; ArrowLeft / Escape close
/// it and return focus to the trigger (directions flip in RTL).</para>
///
/// <code>
/// &lt;BhMenuSub&gt;
///     &lt;BhMenuSubTrigger&gt;Share ▸&lt;/BhMenuSubTrigger&gt;
///     &lt;BhMenuSubContent Anchor="@("right start")"&gt;
///         &lt;BhMenuItem OnClick="Email"&gt;Email&lt;/BhMenuItem&gt;
///     &lt;/BhMenuSubContent&gt;
/// &lt;/BhMenuSub&gt;
/// </code>
/// </summary>
public class BhMenuSub : ComponentBase, IDisposable
{
    private bool _open;
    private BhMenuFocus _focusIntent;
    private CancellationTokenSource? _openTimer;
    private CancellationTokenSource? _closeTimer;
    private readonly string _baseId = BhId.New("bh-sub");

    [CascadingParameter]
    private BhMenuLevel? ParentLevel { get; set; }

    /// <summary>Hover delay (ms) before the submenu opens. Default: 100.</summary>
    [Parameter]
    public int OpenDelay { get; set; } = 100;

    /// <summary>Grace period (ms) before the submenu closes after the pointer leaves. Default: 300.</summary>
    [Parameter]
    public int CloseDelay { get; set; } = 300;

    /// <summary>Fires whenever the submenu opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>The trigger and content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Whether the submenu is open (and its parent menu too).</summary>
    public bool IsOpen => _open && (ParentLevel?.IsOpen ?? false);

    internal BhMenuLevel? Parent => ParentLevel;
    internal BhMenuSubTrigger? TriggerItem { get; set; }
    internal string TriggerId => TriggerItem?.ItemId ?? $"{_baseId}-trigger";
    internal string ContentId => $"{_baseId}-content";
    internal BhDirection Dir => ParentLevel?.Root.Dir ?? BhDirection.Ltr;

    protected override void OnParametersSet()
    {
        // A closed parent never leaves its submenus open for the next time.
        if (_open && ParentLevel is { IsOpen: false })
        {
            _open = false;
            if (ParentLevel.OpenSub == this) ParentLevel.OpenSub = null;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<BhMenuSub>>(0);
        builder.AddComponentParameter(1, "Value", this);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    internal BhMenuFocus ConsumeFocusIntent()
    {
        var intent = _focusIntent;
        _focusIntent = BhMenuFocus.None;
        return intent;
    }

    internal BhMenuFocus PeekFocusIntent() => _focusIntent;

    internal async Task OpenAsync(BhMenuFocus focus)
    {
        CancelTimers();
        if (ParentLevel is null || !ParentLevel.IsOpen) return;

        // Only one submenu per level.
        if (ParentLevel.OpenSub is { } other && other != this)
            await other.CloseAsync(focusParent: false);

        _focusIntent = focus;
        ParentLevel.OpenSub = this;
        if (_open)
        {
            // Already open (e.g. via hover): keyboard entry moves focus inside.
            if (focus is BhMenuFocus.First or BhMenuFocus.Last && Content is not null)
                await Content.EnterAsync(focus);
            return;
        }
        _open = true;
        await OnOpenChange.InvokeAsync(true);
        Refresh();
    }

    internal async Task CloseAsync(bool focusParent)
    {
        CancelTimers();
        if (!_open) return;
        _open = false;
        if (ParentLevel?.OpenSub == this) ParentLevel.OpenSub = null;
        await OnOpenChange.InvokeAsync(false);
        Refresh();
        if (focusParent && ParentLevel is not null)
        {
            await ParentLevel.FocusAsync();
            ParentLevel.SetActive(TriggerItem);
        }
    }

    internal void ScheduleOpen()
    {
        _closeTimer?.Cancel();
        if (_open) return;
        _openTimer?.Cancel();
        var cts = _openTimer = new CancellationTokenSource();
        _ = RunLaterAsync(OpenDelay, cts.Token, () => OpenAsync(BhMenuFocus.None));
    }

    internal void ScheduleClose()
    {
        _openTimer?.Cancel();
        if (!_open) return;
        _closeTimer?.Cancel();
        var cts = _closeTimer = new CancellationTokenSource();
        _ = RunLaterAsync(CloseDelay, cts.Token, () => CloseAsync(focusParent: false));
    }

    internal void CancelClose() => _closeTimer?.Cancel();

    internal void CancelOpen() => _openTimer?.Cancel();

    internal BhMenuSubContent? Content { get; set; }

    private async Task RunLaterAsync(int delay, CancellationToken token, Func<Task> action)
    {
        try
        {
            // Wait off the dispatcher: a cancelled timer must not queue work on the renderer.
            if (delay > 0) await Task.Delay(delay, token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            if (token.IsCancellationRequested) return;
            await InvokeAsync(() => token.IsCancellationRequested ? Task.CompletedTask : action());
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Refresh()
    {
        TriggerItem?.Refresh();
        Content?.Refresh();
    }

    private void CancelTimers()
    {
        _openTimer?.Cancel();
        _closeTimer?.Cancel();
    }

    public void Dispose()
    {
        CancelTimers();
        if (ParentLevel?.OpenSub == this) ParentLevel.OpenSub = null;
    }
}
