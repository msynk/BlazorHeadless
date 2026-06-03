using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless Hover Card — surfaces preview content for sighted users when they
/// hover or focus a link. A faithful port of
/// <a href="https://www.radix-ui.com/primitives/docs/components/hover-card">Radix UI's
/// HoverCard</a> primitive.
///
/// <para><b>Anatomy:</b></para>
/// <code>
/// &lt;BhHoverCard&gt;
///     &lt;BhHoverCardTrigger /&gt;
///     &lt;BhHoverCardContent&gt;
///         &lt;BhHoverCardArrow /&gt;  @* optional *@
///     &lt;/BhHoverCardContent&gt;
/// &lt;/BhHoverCard&gt;
/// </code>
///
/// <para><b>Key features (inspired by Radix UI):</b></para>
/// <list type="bullet">
///   <item>
///     <b>Hover and focus</b> — opens when the pointer enters (or the trigger is
///     focused) and closes on leave / blur. Touch pointers are ignored so the
///     card never blocks tapping the link.
///   </item>
///   <item>
///     <b>Open / close delays</b> — <see cref="OpenDelay"/> (default 700ms) and
///     <see cref="CloseDelay"/> (default 300ms) prevent accidental triggering and
///     let the pointer travel from trigger into content without flicker.
///   </item>
///   <item>
///     <b>Hoverable content</b> — moving the pointer from the trigger into the
///     content keeps the card open; it only closes once the pointer leaves both.
///   </item>
///   <item>
///     <b>Controlled or uncontrolled</b> — seed with <see cref="DefaultOpen"/> or
///     drive with <see cref="Open"/> + <see cref="OnOpenChange"/>.
///   </item>
///   <item>
///     <b>Anchor positioning</b> — set <see cref="BhAnchorOptions"/> on
///     <see cref="BhHoverCardContent"/> for automatic flip / shift placement.
///   </item>
///   <item>
///     <b>Not keyboard-only accessible by design</b> — like Radix, the hover card
///     is a progressive enhancement for pointer/focus users; the trigger remains
///     a fully functional link for everyone else.
///   </item>
/// </list>
///
/// <para><b>Requires <c>builder.Services.AddBlazorHeadless()</c> at startup
/// when using anchor positioning on the content.</b></para>
/// </summary>
public class BhHoverCard : BhComponentBase, IDisposable
{
    /// <summary>Initial open state (uncontrolled). Ignored when <see cref="Open"/> is supplied.</summary>
    [Parameter]
    public bool DefaultOpen { get; set; }

    /// <summary>Controlled open state. When non-null, <see cref="OnOpenChange"/> must update this value.</summary>
    [Parameter]
    public bool? Open { get; set; }

    /// <summary>Fires whenever the open state changes (controlled or uncontrolled).</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>
    /// The delay in milliseconds before the card opens after the pointer enters
    /// the trigger. Default: 700. Set to 0 to open instantly.
    /// </summary>
    [Parameter]
    public int OpenDelay { get; set; } = 700;

    /// <summary>
    /// The delay in milliseconds before the card closes after the pointer leaves
    /// the trigger and content. Default: 300.
    /// </summary>
    [Parameter]
    public int CloseDelay { get; set; } = 300;

    /// <summary>Child content. Should contain a <see cref="BhHoverCardTrigger"/> and a <see cref="BhHoverCardContent"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    // ── State ─────────────────────────────────────────────────────────────────

    private bool _isOpen;
    private CancellationTokenSource? _openCts;
    private CancellationTokenSource? _closeCts;
    private ElementReference _triggerRef;

    private bool IsOpen => Open ?? _isOpen;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        if (Open is null)
            _isOpen = DefaultOpen;
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

        builder.OpenComponent<CascadingValue<BhHoverCardContext>>(0);
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
        SetDataState(attrs, IsOpen);
        return attrs;
    }

    // ── Context ───────────────────────────────────────────────────────────────

    private BhHoverCardContext CreateContext() => new(
        isOpen: IsOpen,
        baseId: ComponentId,
        scheduleOpenAsync: ScheduleOpenAsync,
        scheduleCloseAsync: ScheduleCloseAsync,
        setOpenAsync: SetOpenAsync,
        registerTrigger: t => _triggerRef = t);

    // ── Open / close scheduling ────────────────────────────────────────────────

    private async Task ScheduleOpenAsync()
    {
        CancelCloseTimer();
        if (IsOpen) return;

        if (OpenDelay <= 0)
        {
            await SetOpenInternalAsync(true);
            return;
        }

        CancelOpenTimer();
        _openCts = new CancellationTokenSource();
        var token = _openCts.Token;

        try
        {
            await Task.Delay(OpenDelay, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested) return;
        await InvokeAsync(() => SetOpenInternalAsync(true));
    }

    private async Task ScheduleCloseAsync()
    {
        CancelOpenTimer();
        if (!IsOpen) return;

        if (CloseDelay <= 0)
        {
            await SetOpenInternalAsync(false);
            return;
        }

        CancelCloseTimer();
        _closeCts = new CancellationTokenSource();
        var token = _closeCts.Token;

        try
        {
            await Task.Delay(CloseDelay, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested) return;
        await InvokeAsync(() => SetOpenInternalAsync(false));
    }

    private Task SetOpenAsync(bool open)
    {
        CancelOpenTimer();
        CancelCloseTimer();
        return SetOpenInternalAsync(open);
    }

    private async Task SetOpenInternalAsync(bool value)
    {
        if (IsOpen == value) return;

        if (Open is null)
            _isOpen = value;

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
