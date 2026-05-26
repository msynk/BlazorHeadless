using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless, accessible Tooltip — a small popup that surfaces information
/// about a control when it is hovered or focused. Mirrors the
/// <a href="https://www.radix-ui.com/primitives/docs/components/tooltip">Radix UI
/// Tooltip</a> primitive.
///
/// <para><b>Anatomy:</b></para>
/// <code>
/// &lt;BhTooltipProvider&gt;       @* optional, configures timings globally *@
///     &lt;BhTooltip&gt;
///         &lt;BhTooltipTrigger /&gt;
///         &lt;BhTooltipContent&gt;
///             &lt;BhTooltipArrow /&gt;  @* optional *@
///         &lt;/BhTooltipContent&gt;
///     &lt;/BhTooltip&gt;
/// &lt;/BhTooltipProvider&gt;
/// </code>
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Pointer and keyboard</b> — opens on hover or focus, closes on leave/blur or Escape.</item>
///   <item><b>Delay coordination</b> — uses <see cref="BhTooltipProvider"/>'s
///     <c>DelayDuration</c> / <c>SkipDelayDuration</c> so quickly traversing
///     siblings shows them instantly.</item>
///   <item><b>Hoverable content</b> — the pointer can move from trigger into
///     content without closing (disable via <see cref="DisableHoverableContent"/>).</item>
///   <item><b>Controlled or uncontrolled</b> — seed with <see cref="DefaultOpen"/>
///     or drive with <see cref="Open"/> + <see cref="OnOpenChange"/>.</item>
///   <item><b>Data attributes</b> — emits
///     <c>data-state="closed" | "delayed-open" | "instant-open"</c>.</item>
///   <item><b>Anchor positioning</b> — set <see cref="BhAnchorOptions"/> on
///     <see cref="BhTooltipContent"/> for automatic flip / shift placement.</item>
/// </list>
///
/// <para><b>Requires <c>builder.Services.AddBlazorHeadless()</c> at startup.</b></para>
/// </summary>
public class BhTooltip : BhComponentBase, IDisposable
{
    [CascadingParameter]
    private BhTooltipProviderContext? ProviderContext { get; set; }

    // ── Parameters ────────────────────────────────────────────────────────────

    /// <summary>Initial open state (uncontrolled). Ignored when <see cref="Open"/> is supplied.</summary>
    [Parameter]
    public bool DefaultOpen { get; set; }

    /// <summary>Controlled open state. When non-null, <see cref="OnOpenChange"/> must update this value.</summary>
    [Parameter]
    public bool? Open { get; set; }

    /// <summary>Fires whenever the open state changes.</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>
    /// Override the delay (in milliseconds) before the tooltip opens. Falls back
    /// to the enclosing <see cref="BhTooltipProvider"/>, then to 700ms.
    /// Set to 0 to open instantly.
    /// </summary>
    [Parameter]
    public int? DelayDuration { get; set; }

    /// <summary>
    /// Override the provider's <c>DisableHoverableContent</c>. When <c>true</c>,
    /// pointer leaving the trigger immediately schedules a close.
    /// </summary>
    [Parameter]
    public bool? DisableHoverableContent { get; set; }

    /// <summary>Child content. Should contain a <see cref="BhTooltipTrigger"/> and a <see cref="BhTooltipContent"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    // ── State ─────────────────────────────────────────────────────────────────

    private bool _isOpen;
    private bool _delayedOpen;
    private CancellationTokenSource? _openCts;
    private CancellationTokenSource? _closeCts;
    private ElementReference _triggerRef;

    private bool IsOpen => Open ?? _isOpen;
    private int EffectiveDelay => DelayDuration ?? ProviderContext?.DelayDuration ?? 700;
    private bool EffectiveDisableHoverableContent =>
        DisableHoverableContent ?? ProviderContext?.DisableHoverableContent ?? false;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        if (Open is null)
            _isOpen = DefaultOpen;
        // A controlled-open or default-open tooltip is "instant" — no delay was
        // observed; this matches Radix' data-state semantics for forced open.
        _delayedOpen = false;
    }

    public void Dispose()
    {
        CancelOpenTimer();
        CancelCloseTimer();
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = CreateContext();

        builder.OpenComponent<CascadingValue<BhTooltipContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());

            if (Ref is not null)
                inner.AddElementReferenceCapture(30, Ref);

            inner.AddContent(40, ChildContent);
            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["data-state"] = ResolveStateValue();
        return attrs;
    }

    // ── Context ───────────────────────────────────────────────────────────────

    private BhTooltipContext CreateContext()
    {
        var ctx = new BhTooltipContext(
            isOpen: IsOpen,
            delayedOpen: _delayedOpen,
            baseId: ComponentId,
            disableHoverableContent: EffectiveDisableHoverableContent,
            setOpenAsync: SetOpenAsync,
            scheduleOpenAsync: ScheduleOpenAsync,
            scheduleCloseAsync: ScheduleCloseAsync,
            registerTrigger: RegisterTriggerRef);
        return ctx;
    }

    private void RegisterTriggerRef(ElementReference trigger) => _triggerRef = trigger;

    private string ResolveStateValue()
    {
        if (!IsOpen) return "closed";
        return _delayedOpen ? "delayed-open" : "instant-open";
    }

    // ── Open / Close scheduling ───────────────────────────────────────────────

    private async Task ScheduleOpenAsync()
    {
        CancelCloseTimer();
        if (IsOpen) return;

        // If the provider says we recently closed another tooltip, skip the delay.
        var skipDelay = ProviderContext?.IsRecentlyClosed() == true;
        var delay = skipDelay ? 0 : EffectiveDelay;

        if (delay <= 0)
        {
            await SetOpenInternalAsync(true, delayed: false);
            return;
        }

        CancelOpenTimer();
        _openCts = new CancellationTokenSource();
        var token = _openCts.Token;

        try
        {
            await Task.Delay(delay, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested) return;
        await InvokeAsync(() => SetOpenInternalAsync(true, delayed: true));
    }

    private async Task ScheduleCloseAsync()
    {
        CancelOpenTimer();
        if (!IsOpen) return;

        // A short grace period lets the pointer travel from trigger to content
        // without flicker. Skipped when hoverable content is disabled.
        var grace = EffectiveDisableHoverableContent ? 0 : 100;
        if (grace <= 0)
        {
            await SetOpenInternalAsync(false, delayed: false);
            return;
        }

        CancelCloseTimer();
        _closeCts = new CancellationTokenSource();
        var token = _closeCts.Token;

        try
        {
            await Task.Delay(grace, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested) return;
        await InvokeAsync(() => SetOpenInternalAsync(false, delayed: false));
    }

    private Task SetOpenAsync(bool open)
    {
        CancelOpenTimer();
        CancelCloseTimer();
        return SetOpenInternalAsync(open, delayed: false);
    }

    private async Task SetOpenInternalAsync(bool value, bool delayed)
    {
        if (IsOpen == value && _delayedOpen == delayed) return;

        _delayedOpen = value && delayed;

        if (Open is null)
            _isOpen = value;

        if (!value)
            ProviderContext?.NotifyClosed();

        await OnOpenChange.InvokeAsync(value);
        StateHasChanged();
    }

    private void CancelOpenTimer()
    {
        _openCts?.Cancel();
        _openCts?.Dispose();
        _openCts = null;
    }

    private void CancelCloseTimer()
    {
        _closeCts?.Cancel();
        _closeCts?.Dispose();
        _closeCts = null;
    }
}
