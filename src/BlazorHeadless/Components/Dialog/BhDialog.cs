using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless, accessible Dialog (modal) implementing the WAI-ARIA Dialog pattern.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Focus trap</b> — Tab and Shift+Tab cycle within the panel.</item>
///   <item><b>Initial focus</b> — first focusable element in the panel by default; override via <see cref="BhDialogPanel.InitialFocus"/>.</item>
///   <item><b>Focus return</b> — restores focus to the previously-focused element on close.</item>
///   <item><b>Scroll lock</b> — body scrolling is locked while the dialog is open. Stack-aware.</item>
///   <item><b>Inert background</b> — non-dialog body children are marked <c>inert</c> so they're hidden from assistive tech and unfocusable.</item>
///   <item><b>Escape closes</b>, <b>backdrop click closes</b> — both fire <see cref="OnClose"/>.</item>
///   <item><b>Auto-wired ARIA</b> — <c>role="dialog"</c>, <c>aria-modal="true"</c>, <c>aria-labelledby</c>, <c>aria-describedby</c> all wired by the title and description sub-components.</item>
/// </list>
///
/// <para><b>Requires <c>builder.Services.AddBlazorHeadless()</c> at startup.</b></para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhDialog Open="@isOpen" OnClose="() => isOpen = false"&gt;
///     &lt;BhDialogBackdrop class="dialog-backdrop" /&gt;
///     &lt;BhDialogPanel class="dialog-panel"&gt;
///         &lt;BhDialogTitle&gt;Confirm action&lt;/BhDialogTitle&gt;
///         &lt;BhDialogDescription&gt;This cannot be undone.&lt;/BhDialogDescription&gt;
///         &lt;button @onclick="() => isOpen = false"&gt;Cancel&lt;/button&gt;
///         &lt;button @onclick="Confirm"&gt;Confirm&lt;/button&gt;
///     &lt;/BhDialogPanel&gt;
/// &lt;/BhDialog&gt;
/// </code>
/// </summary>
public class BhDialog : BhComponentBase
{
    [Inject] private BhInterop Interop { get; set; } = default!;

    /// <summary>Whether the dialog is currently open.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Fires when the user dismisses the dialog (Escape, backdrop click, or programmatic close).</summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>
    /// Child content. Should contain an <see cref="BhDialogPanel"/> and optionally an
    /// <see cref="BhDialogBackdrop"/>, <see cref="BhDialogTitle"/>, and
    /// <see cref="BhDialogDescription"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool _wasOpen;
    private int _lockHandle = -1;
    private ElementReference _panelRef;
    private bool _hasPanelRef;

    protected override string DefaultTag => "div";

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Lock when transitioning from closed → open, unlock on the reverse.
        if (Open && !_wasOpen && _hasPanelRef)
        {
            _lockHandle = await Interop.DialogLockAsync(_panelRef);
            _wasOpen = true;
        }
        else if (!Open && _wasOpen)
        {
            await Interop.DialogUnlockAsync(_lockHandle);
            _lockHandle = -1;
            _wasOpen = false;
            _hasPanelRef = false;
        }
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!Open) return;

        var context = CreateContext();

        builder.OpenComponent<CascadingValue<IBhCloseContext>>(0);
        builder.AddComponentParameter(1, "Value", (IBhCloseContext)context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(closeOuter =>
        {
            closeOuter.OpenComponent<CascadingValue<BhDialogContext>>(0);
            closeOuter.AddComponentParameter(1, "Value", context);
            closeOuter.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenElement(0, Tag);
                inner.AddAttribute(10, "id", ComponentId);
                inner.AddMultipleAttributes(20, GetFinalAttributes());

                if (Ref is not null)
                    inner.AddElementReferenceCapture(30, Ref);

                if (ChildContent is not null)
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
        SetDataState(attrs, Open);
        // The dialog root is a positioning container that fills the viewport so
        // the panel and backdrop can be absolutely positioned within it.
        // Consumers can override these styles entirely via class= or style=.
        if (!attrs.ContainsKey("style"))
            attrs["style"] = "position:fixed;inset:0;z-index:50;";
        return attrs;
    }

    // ── Context plumbing ─────────────────────────────────────────────────────

    private BhDialogContext CreateContext() => new(
        isOpen: Open,
        baseId: ComponentId,
        closeAsync: CloseAsync,
        registerPanel: RegisterPanelRef,
        registerTitle: _ => { /* title id is derived deterministically; no-op */ },
        registerDescription: _ => { /* same */ });

    private void RegisterPanelRef(ElementReference panel)
    {
        _panelRef = panel;
        _hasPanelRef = true;
    }

    private async Task CloseAsync()
    {
        if (!Open) return;
        await OnClose.InvokeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_lockHandle > 0)
            await Interop.DialogUnlockAsync(_lockHandle);
    }
}
