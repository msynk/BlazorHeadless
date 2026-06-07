using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless, accessible Alert Dialog implementing the WAI-ARIA
/// <c>alertdialog</c> pattern. An alert dialog is a modal dialog that interrupts
/// the user with an important message and expects a deliberate response — for
/// example, confirming a destructive action.
///
/// <para><b>Key differences from <see cref="BhDialog"/>:</b></para>
/// <list type="bullet">
///   <item><b>Built-in trigger</b> — opens via an <see cref="BhAlertDialogTrigger"/> and supports controlled/uncontrolled state.</item>
///   <item><b><c>role="alertdialog"</c></b> instead of <c>role="dialog"</c>.</item>
///   <item><b>No dismiss on overlay click</b> — the user must explicitly choose <see cref="BhAlertDialogCancel"/> or <see cref="BhAlertDialogAction"/>.</item>
///   <item><b>Default focus on Cancel</b> — focus lands on the <see cref="BhAlertDialogCancel"/> button when present, the safest choice.</item>
/// </list>
///
/// <para><b>Key features (shared with <see cref="BhDialog"/>):</b></para>
/// <list type="bullet">
///   <item><b>Focus trap</b> — Tab and Shift+Tab cycle within the content.</item>
///   <item><b>Focus return</b> — restores focus to the trigger on close.</item>
///   <item><b>Scroll lock</b> — body scrolling is locked while open. Stack-aware.</item>
///   <item><b>Inert background</b> — non-dialog body children are marked <c>inert</c>.</item>
///   <item><b>Escape closes</b> — fires <see cref="OnOpenChange"/> with <c>false</c>.</item>
///   <item><b>Auto-wired ARIA</b> — <c>role="alertdialog"</c>, <c>aria-modal="true"</c>, <c>aria-labelledby</c>, and <c>aria-describedby</c>.</item>
/// </list>
///
/// <para><b>Requires <c>builder.Services.AddBlazorHeadless()</c> at startup.</b></para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhAlertDialog&gt;
///     &lt;BhAlertDialogTrigger&gt;Delete account&lt;/BhAlertDialogTrigger&gt;
///     &lt;BhAlertDialogOverlay class="overlay" /&gt;
///     &lt;BhAlertDialogContent class="content"&gt;
///         &lt;BhAlertDialogTitle&gt;Are you absolutely sure?&lt;/BhAlertDialogTitle&gt;
///         &lt;BhAlertDialogDescription&gt;
///             This action cannot be undone. This will permanently delete your account.
///         &lt;/BhAlertDialogDescription&gt;
///         &lt;BhAlertDialogCancel&gt;Cancel&lt;/BhAlertDialogCancel&gt;
///         &lt;BhAlertDialogAction OnClick="DeleteAccount"&gt;Yes, delete account&lt;/BhAlertDialogAction&gt;
///     &lt;/BhAlertDialogContent&gt;
/// &lt;/BhAlertDialog&gt;
/// </code>
/// </summary>
public class BhAlertDialog : BhComponentBase, IAsyncDisposable
{
    [Inject] private BhInterop Interop { get; set; } = default!;

    // ── Parameters ────────────────────────────────────────────────────────────

    /// <summary>Initial open state (uncontrolled). Ignored when <see cref="Open"/> is supplied.</summary>
    [Parameter]
    public bool DefaultOpen { get; set; }

    /// <summary>Controlled open state. When non-null, <see cref="OnOpenChange"/> must update this value.</summary>
    [Parameter]
    public bool? Open { get; set; }

    /// <summary>Fires whenever the open state changes (trigger, Escape, Cancel, or Action).</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>
    /// Child content. Should contain an <see cref="BhAlertDialogTrigger"/> and an
    /// <see cref="BhAlertDialogContent"/>, optionally an
    /// <see cref="BhAlertDialogOverlay"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    // ── State ─────────────────────────────────────────────────────────────────

    private bool _isOpen;
    private bool _wasOpen;
    private int _lockHandle = -1;
    private ElementReference _contentRef;
    private ElementReference _triggerRef;
    private ElementReference _cancelRef;
    private bool _hasContentRef;
    private bool _hasTriggerRef;
    private bool _hasCancelRef;

    private bool IsOpen => Open ?? _isOpen;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        if (Open is null)
            _isOpen = DefaultOpen;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Lock when transitioning from closed → open, unlock on the reverse.
        if (IsOpen && !_wasOpen && _hasContentRef)
        {
            ElementReference? initialFocus = _hasCancelRef ? _cancelRef : null;
            ElementReference? returnFocus = _hasTriggerRef ? _triggerRef : null;
            _lockHandle = await Interop.DialogLockAsync(_contentRef, initialFocus, returnFocus);
            _wasOpen = true;
        }
        else if (!IsOpen && _wasOpen)
        {
            await Interop.DialogUnlockAsync(_lockHandle);
            _lockHandle = -1;
            _wasOpen = false;
            _hasContentRef = false;
            _hasCancelRef = false;
        }
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = CreateContext();

        builder.OpenComponent<CascadingValue<IBhCloseContext>>(0);
        builder.AddComponentParameter(1, "Value", (IBhCloseContext)context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(closeOuter =>
        {
            closeOuter.OpenComponent<CascadingValue<BhAlertDialogContext>>(0);
            closeOuter.AddComponentParameter(1, "Value", context);
            closeOuter.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenElement(0, Tag);
                inner.AddAttribute(10, "id", ComponentId);
                inner.AddMultipleAttributes(20, GetFinalAttributes());

                if (Ref is not null)
                    inner.AddElementReferenceCapture(30, Ref);

                inner.AddContent(40, ChildContent);

                inner.CloseElement();
            }));
            closeOuter.CloseComponent();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);
        return attrs;
    }

    // ── Context plumbing ─────────────────────────────────────────────────────

    private BhAlertDialogContext CreateContext() => new(
        isOpen: IsOpen,
        baseId: ComponentId,
        openAsync: OpenAsync,
        closeAsync: CloseAsync,
        registerContent: RegisterContentRef,
        registerTrigger: RegisterTriggerRef,
        registerCancel: RegisterCancelRef);

    private void RegisterContentRef(ElementReference content)
    {
        _contentRef = content;
        _hasContentRef = true;
    }

    private void RegisterTriggerRef(ElementReference trigger)
    {
        _triggerRef = trigger;
        _hasTriggerRef = true;
    }

    private void RegisterCancelRef(ElementReference cancel)
    {
        _cancelRef = cancel;
        _hasCancelRef = true;
    }

    // ── Open / Close ───────────────────────────────────────────────────────────

    internal Task OpenAsync()
    {
        if (IsOpen) return Task.CompletedTask;
        SetOpen(true);
        return Task.CompletedTask;
    }

    internal Task CloseAsync()
    {
        if (!IsOpen) return Task.CompletedTask;
        SetOpen(false);
        return Task.CompletedTask;
    }

    private void SetOpen(bool value)
    {
        if (Open is null)
            _isOpen = value;
        _ = OnOpenChange.InvokeAsync(value);
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (_lockHandle > 0)
            await Interop.DialogUnlockAsync(_lockHandle);
    }
}
